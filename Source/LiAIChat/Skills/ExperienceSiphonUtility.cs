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
                Messages.Message("LiAIChat_ExperienceSiphonNoExperience".Translate(patient.LabelShort),
                    patient, MessageTypeDefOf.NeutralEvent);
                return false;
            }

            SkillRecord donorSkill = donorSkills.RandomElement();
            SkillRecord recipientSkill = surgeon.skills.GetSkill(donorSkill.def);
            float takenExperience = Math.Min(MaximumDonorExperiencePerOperation, donorSkill.XpTotalEarned);
            float receivedExperience = takenExperience / BaseDonorExperiencePerRecipientExperience * EfficiencyFactor(surgeon);

            RemoveExperience(donorSkill, takenExperience);
            recipientSkill.Learn(receivedExperience, true, true);

            Messages.Message(
                "LiAIChat_ExperienceSiphonGained".Translate(
                    donorSkill.def.LabelCap,
                    receivedExperience.ToString("0.##")),
                surgeon,
                MessageTypeDefOf.PositiveEvent);
            GrantMoodBoost(patient);
            GrantMoodBoost(surgeon);
            return true;
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
            float remainingExperience = Math.Max(0f, skill.XpTotalEarned - amount);
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
                __result = false;
            }
        }
    }

    public class Recipe_SiphonExperience : Recipe_Surgery
    {
        public override bool AvailableOnNow(Thing thing, BodyPartRecord part)
        {
            return base.AvailableOnNow(thing, part) &&
                ExperienceSiphonUtility.IsEligiblePatient(thing as Pawn);
        }

        protected override void OnSurgerySuccess(
            Pawn pawn,
            BodyPartRecord part,
            Pawn billDoer,
            List<Thing> ingredients,
            Bill bill)
        {
            base.OnSurgerySuccess(pawn, part, billDoer, ingredients, bill);
            ExperienceSiphonUtility.TransferRandomSkillExperience(pawn, billDoer);
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
