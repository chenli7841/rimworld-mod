using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace LiAIChat.Skills
{
    public static class ExperienceSiphonUtility
    {
        public const string RecipeDefName = "LiAIChat_SiphonExperience";

        private static readonly HashSet<Bill_Medical> CompletedOperations = new HashSet<Bill_Medical>();

        private const float BaseDonorExperiencePerRecipientExperience = 3f;
        private const float MaximumDonorExperiencePerOperation = 8000f;
        private const float MedicalSkillBonusPerLevel = 0.025f;
        private const float SocialSkillBonusPerLevel = 0.025f;
        private const float BeautyBonusPerPoint = 0.10f;
        private const float AestheticImplantBonus = 0.25f;

        public static bool IsEligiblePatient(Pawn patient)
        {
            if (patient == null || patient.Dead || !patient.IsPrisonerOfColony || patient.ageTracker == null)
            {
                return false;
            }

            float age = patient.ageTracker.AgeBiologicalYears;
            if (age < 20f || age > 50f || patient.skills == null ||
                !patient.skills.skills.Any(skill => skill != null &&
                    skill.Level >= 2 && skill.XpTotalEarned > 0f))
            {
                return false;
            }

            Room room = patient.GetRoom();
            if (room == null || room.PsychologicallyOutdoors)
            {
                return false;
            }

            List<Building_Bed> beds = room.ContainedBeds.ToList();
            return beds.Count == 1 && beds[0].def.defName == "RoyalBed";
        }

        public static bool IsEligibleSurgeon(Pawn surgeon, Pawn patient)
        {
            if (surgeon == null || patient == null || !IsEligiblePatient(patient) ||
                surgeon.Dead || surgeon.Downed || !surgeon.IsColonist ||
                surgeon.IsPrisoner || surgeon.RaceProps == null || !surgeon.RaceProps.Humanlike ||
                surgeon.ageTracker == null || surgeon.skills == null || surgeon.Map != patient.Map ||
                HasCurrentRomanticRelationship(surgeon) || AreFamily(surgeon, patient))
            {
                return false;
            }

            float age = surgeon.ageTracker.AgeBiologicalYears;
            bool ageAndGenderMatch = age >= 13f && age <= 50f &&
                surgeon.gender != Gender.None && patient.gender != Gender.None &&
                surgeon.gender != patient.gender;
            return ageAndGenderMatch && HasTransferableSkill(surgeon, patient);
        }

        public static float EfficiencyFactor(Pawn surgeon)
        {
            if (surgeon == null || surgeon.skills == null)
            {
                return 1f;
            }

            SkillRecord medicine = surgeon.skills.GetSkill(SkillDefOf.Medicine);
            SkillRecord social = surgeon.skills.GetSkill(SkillDefOf.Social);
            float medicineLevel = medicine == null ? 0f : medicine.Level;
            float socialLevel = social == null ? 0f : social.Level;

            StatDef beautyStat = DefDatabase<StatDef>.GetNamedSilentFail("PawnBeauty");
            float beauty = beautyStat == null ? 0f : Math.Max(0f, surgeon.GetStatValue(beautyStat));

            float factor = 1f +
                medicineLevel * MedicalSkillBonusPerLevel +
                socialLevel * SocialSkillBonusPerLevel +
                beauty * BeautyBonusPerPoint;

            if (HasHediff(surgeon, "AestheticShaper")) factor += AestheticImplantBonus;
            if (HasHediff(surgeon, "AestheticNose")) factor += AestheticImplantBonus;
            return factor;
        }

        public static bool TransferRandomSkillExperience(Pawn patient, Pawn surgeon)
        {
            if (!IsEligibleSurgeon(surgeon, patient))
            {
                Log.Message("[LiAIChat Experience Siphon] Transfer skipped: " + EligibilityFailureReason(surgeon, patient));
                return false;
            }

            List<SkillRecord> donorSkills = patient.skills.skills
                .Where(skill => skill != null && skill.def != null &&
                    skill.Level >= 2 && skill.XpTotalEarned > 0f &&
                    surgeon.skills.GetSkill(skill.def) != null &&
                    !surgeon.skills.GetSkill(skill.def).TotallyDisabled &&
                    surgeon.skills.GetSkill(skill.def).Level < 20)
                .ToList();

            if (donorSkills.Count == 0)
            {
                Log.Message("[LiAIChat Experience Siphon] Transfer skipped: no eligible skill with experience. Patient=" +
                    (patient?.LabelShort ?? "null") + ", surgeon=" + (surgeon?.LabelShort ?? "null"));
                Messages.Message("LiAIChat_ExperienceSiphonNoExperience".Translate(patient.LabelShort),
                    patient, MessageTypeDefOf.NeutralEvent);
                return false;
            }

            SkillRecord donorSkill = donorSkills
                .OrderByDescending(skill => skill.Level)
                .ThenByDescending(skill => skill.xpSinceLastLevel)
                .ThenBy(skill => skill.def.defName, StringComparer.Ordinal)
                .First();
            SkillRecord recipientSkill = surgeon.skills.GetSkill(donorSkill.def);
            int donorLevelBefore = donorSkill.Level;
            float donorExperienceBefore = TotalExperience(donorSkill);
            float takenExperience = Math.Min(MaximumDonorExperiencePerOperation, donorExperienceBefore);
            float receivedExperience = takenExperience / BaseDonorExperiencePerRecipientExperience * EfficiencyFactor(surgeon);

            RemoveExperience(donorSkill, takenExperience);
            int donorLevelAfter = donorSkill.Level;
            recipientSkill.Learn(receivedExperience, true, true);

            Log.Message("[LiAIChat Experience Siphon] Experience transferred. Patient=" + patient.LabelShort +
                ", surgeon=" + surgeon.LabelShort + ", skill=" + donorSkill.def.defName +
                ", donorXpRemoved=" + takenExperience.ToString("0.##") +
                ", recipientXpAdded=" + receivedExperience.ToString("0.##") +
                ", donorLevel=" + donorLevelBefore + "->" + donorLevelAfter);

            Messages.Message(
                "LiAIChat_ExperienceSiphonGained".Translate(
                    patient.LabelShort,
                    donorSkill.def.LabelCap,
                    takenExperience.ToString("0.##"),
                    donorLevelBefore,
                    donorLevelAfter,
                    surgeon.LabelShort,
                    receivedExperience.ToString("0.##")),
                surgeon,
                MessageTypeDefOf.PositiveEvent);
            GrantMoodBoost(patient);
            GrantMoodBoost(surgeon);
            return true;
        }

        public static void RecordSuccessfulOperation(Pawn patient, Pawn surgeon, List<Thing> ingredients, Bill bill)
        {
            if (bill?.recipe?.defName != RecipeDefName)
            {
                return;
            }

            Log.Message("[LiAIChat Experience Siphon] Surgery success callback. Patient=" +
                (patient?.LabelShort ?? "null") + ", surgeon=" + (surgeon?.LabelShort ?? "null") +
                ", ingredients=" + DescribeMedicine(ingredients));
        }

        public static void CompleteSuccessfulOperation(Bill_Medical bill, Pawn surgeon, List<Thing> ingredients)
        {
            if (bill == null || !CompletedOperations.Add(bill))
            {
                return;
            }

            Pawn patient = bill.GiverPawn;
            Log.Message("[LiAIChat Experience Siphon] Finalizing completed operation. Patient=" +
                (patient?.LabelShort ?? "null") + ", surgeon=" + (surgeon?.LabelShort ?? "null") +
                ", ingredients=" + DescribeMedicine(ingredients));

            bool medicineWasBillIngredient = ingredients != null && ingredients.Any(thing =>
                thing != null && thing.def?.defName == "MedicineIndustrial");
            if (medicineWasBillIngredient)
            {
                Log.Message("[LiAIChat Experience Siphon] Medicine was supplied to the vanilla bill ingredient list.");
            }
            else
            {
                ConsumeMedicineFromMap(patient, surgeon);
            }

            TransferRandomSkillExperience(patient, surgeon);
        }

        private static void ConsumeMedicineFromMap(Pawn patient, Pawn surgeon)
        {
            ThingDef medicineDef = DefDatabase<ThingDef>.GetNamedSilentFail("MedicineIndustrial");
            Map map = patient?.Map ?? surgeon?.Map;
            if (medicineDef == null || map == null)
            {
                Log.Message("[LiAIChat Experience Siphon] Medicine not consumed: MedicineIndustrial def or map unavailable.");
                return;
            }

            Thing medicine = map.listerThings.ThingsOfDef(medicineDef)
                .Where(thing => thing != null && thing.Spawned && !thing.Destroyed && thing.stackCount > 0 &&
                    (surgeon == null || (!thing.IsForbidden(surgeon) && surgeon.CanReach(thing, PathEndMode.Touch, Danger.Deadly))))
                .OrderBy(thing => patient == null ? 0 : patient.Position.DistanceToSquared(thing.Position))
                .FirstOrDefault();

            if (medicine == null)
            {
                Log.Message("[LiAIChat Experience Siphon] Medicine not consumed: no reachable MedicineIndustrial stack on the map.");
                return;
            }

            int before = medicine.stackCount;
            Thing consumed = before == 1 ? medicine : medicine.SplitOff(1);
            consumed?.Destroy(DestroyMode.Vanish);
            Log.Message("[LiAIChat Experience Siphon] Consumed one MedicineIndustrial from map stock. Stack=" +
                before + "->" + (medicine.Destroyed ? 0 : medicine.stackCount));
        }

        public static string EligibilityFailureReason(Pawn surgeon, Pawn patient)
        {
            if (surgeon == null) return "surgeon is null";
            if (patient == null) return "patient is null";
            if (patient.Dead) return "patient is dead";
            if (!patient.IsPrisonerOfColony) return "patient is no longer a colony prisoner";
            if (!IsEligiblePatient(patient)) return "patient no longer meets age, skill, room, or bed conditions";
            if (surgeon.Dead) return "surgeon is dead";
            if (surgeon.Downed) return "surgeon is downed";
            if (!surgeon.IsColonist) return "surgeon is not a colonist";
            if (surgeon.IsPrisoner) return "surgeon is a prisoner";
            if (surgeon.ageTracker == null || surgeon.skills == null) return "surgeon age or skills tracker is missing";
            if (surgeon.Map != patient.Map) return "surgeon and patient are on different maps";
            if (HasCurrentRomanticRelationship(surgeon)) return "surgeon has a current romantic relationship";
            if (AreFamily(surgeon, patient)) return "surgeon and patient are family";
            if (surgeon.ageTracker.AgeBiologicalYears < 13f || surgeon.ageTracker.AgeBiologicalYears > 50f)
                return "surgeon is outside the allowed age range";
            if (!HasTransferableSkill(surgeon, patient)) return "no transferable skill remains";
            return "eligibility check failed for an unknown reason";
        }

        private static string DescribeMedicine(List<Thing> ingredients)
        {
            if (ingredients == null || ingredients.Count == 0)
            {
                return "none";
            }

            return string.Join(", ", ingredients.Where(thing => thing != null)
                .Select(thing => thing.def?.defName + " x" + thing.stackCount + (thing.Destroyed ? " (destroyed)" : "")));
        }

        public static void TickExperienceSiphonRestAndHeart(Pawn pawn, int delta)
        {
            if (pawn == null || pawn.needs == null || pawn.needs.rest == null || delta <= 0 ||
                pawn.CurJob == null || pawn.CurJob.bill == null || pawn.CurJob.bill.recipe == null ||
                pawn.CurJob.bill.recipe.defName != RecipeDefName || pawn.Map == null)
            {
                return;
            }

            pawn.needs.rest.CurLevel = Math.Min(1f, pawn.needs.rest.CurLevel + 0.0008f * delta);

            Pawn patient = SiphonPatientForJob(pawn.CurJob);
            if (patient != null)
            {
                GrantMoodBoostIfMissing(pawn);
                GrantMoodBoostIfMissing(patient);
            }
        }

        private static Pawn SiphonPatientForJob(Job job)
        {
            if (job?.bill?.recipe?.defName != RecipeDefName)
            {
                return null;
            }

            Pawn patient = job.targetA.Thing as Pawn;
            if (patient != null && patient.IsPrisonerOfColony)
            {
                return patient;
            }

            patient = job.targetB.Thing as Pawn;
            return patient != null && patient.IsPrisonerOfColony ? patient : null;
        }

        private static bool HasHediff(Pawn pawn, string defName)
        {
            HediffDef hediffDef = DefDatabase<HediffDef>.GetNamedSilentFail(defName);
            return hediffDef != null && pawn.health != null && pawn.health.hediffSet.GetFirstHediffOfDef(hediffDef) != null;
        }

        private static bool HasTransferableSkill(Pawn surgeon, Pawn patient)
        {
            return surgeon?.skills != null && patient?.skills != null &&
                patient.skills.skills.Any(donorSkill =>
                {
                    if (donorSkill == null || donorSkill.def == null ||
                        donorSkill.Level < 2 || donorSkill.XpTotalEarned <= 0f)
                    {
                        return false;
                    }

                    SkillRecord recipientSkill = surgeon.skills.GetSkill(donorSkill.def);
                    return recipientSkill != null && !recipientSkill.TotallyDisabled && recipientSkill.Level < 20;
                });
        }

        private static bool HasCurrentRomanticRelationship(Pawn pawn)
        {
            if (pawn?.relations == null)
            {
                return false;
            }

            return pawn.relations.DirectRelations.Any(relation =>
                relation != null &&
                (relation.def == PawnRelationDefOf.Spouse ||
                 relation.def == PawnRelationDefOf.Fiance ||
                 relation.def == PawnRelationDefOf.Lover));
        }

        private static bool AreFamily(Pawn first, Pawn second)
        {
            if (first?.relations == null || second?.relations == null)
            {
                return false;
            }

            return first.relations.FamilyByBlood.Contains(second) ||
                second.relations.FamilyByBlood.Contains(first);
        }

        private static void RemoveExperience(SkillRecord skill, float amount)
        {
            // XpTotalEarned includes completed levels only; xpSinceLastLevel is separate.
            float remainingExperience = Math.Max(0f, TotalExperience(skill) - amount);
            int newLevel = 0;

            while (newLevel < 20)
            {
                float experienceForNextLevel = SkillRecord.XpRequiredToLevelUpFrom(newLevel);
                if (remainingExperience < experienceForNextLevel)
                {
                    break;
                }

                remainingExperience -= experienceForNextLevel;
                newLevel++;
            }

            float experienceLostToday = Math.Min(amount, skill.xpSinceMidnight);
            skill.Level = newLevel;
            skill.xpSinceLastLevel = remainingExperience;
            skill.xpSinceMidnight = Math.Max(0f, skill.xpSinceMidnight - experienceLostToday);
        }

        private static float TotalExperience(SkillRecord skill)
        {
            return skill == null ? 0f : skill.XpTotalEarned + Math.Max(0f, skill.xpSinceLastLevel);
        }

        private static void GrantMoodBoost(Pawn pawn)
        {
            ThoughtDef thought = DefDatabase<ThoughtDef>.GetNamedSilentFail("LiAIChat_ExperienceSiphonAfterglow");
            MemoryThoughtHandler memories = pawn?.needs?.mood?.thoughts?.memories;
            if (memories != null && thought != null)
            {
                Thought_Memory oldMemory = memories.GetFirstMemoryOfDef(thought);
                if (oldMemory != null)
                {
                    memories.RemoveMemory(oldMemory);
                }

                memories.TryGainMemory(thought, pawn);
            }
        }

        private static void GrantMoodBoostIfMissing(Pawn pawn)
        {
            ThoughtDef thought = DefDatabase<ThoughtDef>.GetNamedSilentFail("LiAIChat_ExperienceSiphonAfterglow");
            MemoryThoughtHandler memories = pawn?.needs?.mood?.thoughts?.memories;
            if (memories != null && thought != null && memories.GetFirstMemoryOfDef(thought) == null)
            {
                memories.TryGainMemory(thought, pawn);
            }
        }
    }

    [HarmonyPatch(typeof(Recipe_Surgery), "CheckSurgeryFail")]
    public static class Recipe_Surgery_ExperienceSiphonSuccessPatch
    {
        public static void Postfix(object[] __args, ref bool __result)
        {
            Bill bill = __args != null && __args.Length > 4 ? __args[4] as Bill : null;
            if (bill?.recipe?.defName == ExperienceSiphonUtility.RecipeDefName)
            {
                bool originalResult = __result;
                __result = false;
                Log.Message("[LiAIChat Experience Siphon] Surgery failure check intercepted. OriginalResult=" +
                    originalResult + ", finalResult=false (continue as success).");
            }
        }
    }

    [HarmonyPatch(typeof(Recipe_Surgery), "OnSurgerySuccess")]
    public static class Recipe_Surgery_ExperienceSiphonCompletionPatch
    {
        public static void Postfix(Pawn pawn, Pawn billDoer, List<Thing> ingredients, Bill bill)
        {
            ExperienceSiphonUtility.RecordSuccessfulOperation(pawn, billDoer, ingredients, bill);
        }
    }

    [HarmonyPatch(typeof(Bill_Medical), "Notify_IterationCompleted")]
    public static class Bill_Medical_ExperienceSiphonMedicineConsumptionPatch
    {
        public static void Postfix(Bill_Medical __instance, Pawn billDoer, List<Thing> ingredients)
        {
            if (__instance?.recipe?.defName == ExperienceSiphonUtility.RecipeDefName)
            {
                Log.Message("[LiAIChat Experience Siphon] Medical bill iteration completed. Doer=" +
                    (billDoer?.LabelShort ?? "null") + ", ingredients=" +
                    (ingredients == null ? "null" : ingredients.Count.ToString()) + ".");
                ExperienceSiphonUtility.CompleteSuccessfulOperation(__instance, billDoer, ingredients);
            }
        }
    }

    [HarmonyPatch(typeof(JobDriver), "Notify_Starting")]
    public static class JobDriver_ExperienceSiphonStartLogPatch
    {
        public static void Postfix(JobDriver __instance)
        {
            Job job = __instance?.job;
            if (job?.bill?.recipe?.defName != ExperienceSiphonUtility.RecipeDefName)
            {
                return;
            }

            Pawn patient = job.targetA.Thing as Pawn;
            Log.Message("[LiAIChat Experience Siphon] Job started. Surgeon=" +
                (__instance.pawn?.LabelShort ?? "null") + ", patient=" +
                (patient?.LabelShort ?? "null") + ", jobDef=" + job.def?.defName +
                ", recipe=" + job.bill.recipe.defName + ", targetB=" +
                (job.targetB.Thing?.def?.defName ?? "null") +
                (job.targetB.Thing == null ? "" : " x" + job.targetB.Thing.stackCount));
        }
    }

    public class Recipe_SiphonExperience : Recipe_Surgery
    {
        public override bool AvailableOnNow(Thing thing, BodyPartRecord part)
        {
            return base.AvailableOnNow(thing, part) &&
                ExperienceSiphonUtility.IsEligiblePatient(thing as Pawn);
        }
    }

    [HarmonyPatch(typeof(WorkGiver_DoBill), "JobOnThing")]
    public static class WorkGiver_DoBill_ExperienceSiphonPatch
    {
        public static void Postfix(object[] __args, ref Job __result)
        {
            if (__result == null || __result.bill == null || __result.bill.recipe == null ||
                __result.bill.recipe.defName != ExperienceSiphonUtility.RecipeDefName)
            {
                return;
            }

            Pawn surgeon = __args.Length > 0 ? __args[0] as Pawn : null;
            Pawn patient = __args.Length > 1 ? __args[1] as Pawn : null;
            if (!ExperienceSiphonUtility.IsEligibleSurgeon(surgeon, patient))
            {
                __result = null;
                return;
            }

            JobDef interruptibleBill = DefDatabase<JobDef>.GetNamedSilentFail("LiAIChat_ExperienceSiphon");
            if (interruptibleBill != null)
            {
                __result.def = interruptibleBill;
            }
        }
    }

    [HarmonyPatch(typeof(Pawn), "TickInterval")]
    public static class Pawn_ExperienceSiphonRestAndHeartPatch
    {
        public static void Postfix(Pawn __instance, int delta)
        {
            ExperienceSiphonUtility.TickExperienceSiphonRestAndHeart(__instance, delta);
        }
    }
}
