using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace LiAIChat.Furniture
{
    public class Building_MassageChair : Building
    {
        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn)
        {
            foreach (FloatMenuOption option in base.GetFloatMenuOptions(selPawn))
                yield return option;

            if (selPawn == null || !selPawn.IsColonistPlayerControlled)
                yield break;

            CompMassageChair massage = GetComp<CompMassageChair>();
            if (massage == null)
                yield break;

            string reason = massage.GetManualMassageDisabledReason(selPawn);
            if (reason != null)
            {
                yield return new FloatMenuOption("LiAIChat_MassageChairEnjoyOption".Translate() + " (" + reason + ")", null);
                yield break;
            }

            yield return new FloatMenuOption("LiAIChat_MassageChairEnjoyOption".Translate(), () =>
            {
                JobDef jobDef = DefDatabase<JobDef>.GetNamedSilentFail("LiAIChat_EnjoyMassage");
                if (jobDef != null && selPawn.Spawned && selPawn.CanReserve(this))
                    selPawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(jobDef, this), JobTag.Misc);
            });
        }
    }

    public class CompProperties_MassageChair : CompProperties
    {
        public CompProperties_MassageChair()
        {
            compClass = typeof(CompMassageChair);
        }
    }

    public class CompMassageChair : ThingComp
    {
        public const int MassageIntervalTicks = 2500;
        public const int MassageBuffTicks = 10000;
        public const int LimbBuffTicks = 5000;
        public const int ActiveMassageTicks = 1250;
        public const float ActiveJoyPerTick = 0.00012f;

        private Pawn currentUser;
        private int progressTicks;

        private CompPowerTrader Power => parent?.GetComp<CompPowerTrader>();
        public bool Powered => Power == null || Power.PowerOn;
        public Pawn CurrentUser => FindCurrentUser();
        public int ProgressTicks => progressTicks;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_References.Look(ref currentUser, "massageChairCurrentUser");
            Scribe_Values.Look(ref progressTicks, "massageChairProgressTicks", 0);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                progressTicks = Mathf.Clamp(progressTicks, 0, MassageIntervalTicks - 1);
                if (currentUser == null)
                    progressTicks = 0;
            }
        }

        public override void CompTick()
        {
            base.CompTick();
            if (parent?.Spawned != true || parent.Map == null)
            {
                ResetProgress();
                return;
            }

            Pawn user = FindCurrentUser();
            if (user == null)
            {
                ResetProgress();
                return;
            }

            if (currentUser != user)
            {
                currentUser = user;
                progressTicks = 0;
            }

            if (!Powered)
                return;

            progressTicks++;
            if (progressTicks >= MassageIntervalTicks)
            {
                progressTicks = 0;
                MassageEffectUtility.Apply(user);
            }
        }

        public override string CompInspectStringExtra()
        {
            string powerState = "LiAIChat_MassageChairPowerState".Translate(
                Powered ? "LiAIChat_MassageChairPowered".Translate() : "LiAIChat_MassageChairUnpowered".Translate());
            Pawn user = FindCurrentUser();
            string userState = user == null
                ? "LiAIChat_MassageChairUserNone".Translate()
                : "LiAIChat_MassageChairUser".Translate(user.LabelShortCap);
            string progress = user == null
                ? "LiAIChat_MassageChairProgressIdle".Translate(MassageIntervalTicks / 2500f)
                : "LiAIChat_MassageChairProgress".Translate(
                    (progressTicks / 2500f).ToString("0.0"), MassageIntervalTicks / 2500f);

            return powerState + "\n" + userState + "\n" + progress + "\n" +
                   "LiAIChat_MassageChairEffectsInfo".Translate();
        }

        public string GetManualMassageDisabledReason(Pawn pawn)
        {
            if (pawn == null || !pawn.Spawned || pawn.Map != parent?.Map)
                return "LiAIChat_MassageChairPawnUnavailable".Translate();
            if (pawn.Downed || pawn.Drafted || pawn.InMentalState)
                return "LiAIChat_MassageChairPawnUnavailable".Translate();
            if (!Powered)
                return "LiAIChat_MassageChairNeedsPower".Translate();
            Pawn user = FindCurrentUser();
            if (user != null && user != pawn)
                return "LiAIChat_MassageChairOccupied".Translate(user.LabelShortCap);
            if (!pawn.CanReach(parent, PathEndMode.OnCell, Danger.Some))
                return "LiAIChat_MassageChairCannotReach".Translate();
            if (!pawn.CanReserve(parent))
                return "LiAIChat_MassageChairOccupied".Translate(user?.LabelShortCap ?? "");
            return null;
        }

        public Pawn FindCurrentUser()
        {
            if (parent?.Spawned != true || parent.Map == null)
                return null;

            List<Thing> things = parent.Map.thingGrid.ThingsListAt(parent.Position);
            for (int i = 0; i < things.Count; i++)
            {
                Pawn pawn = things[i] as Pawn;
                if (pawn != null && pawn.Spawned && pawn.Position == parent.Position &&
                    !PawnPostureUtility.Laying(pawn.GetPosture()) && !pawn.pather.Moving)
                    return pawn;
            }
            return null;
        }

        private void ResetProgress()
        {
            currentUser = null;
            progressTicks = 0;
        }
    }

    public static class MassageEffectUtility
    {
        private const string MoodThoughtDefName = "LiAIChat_RelaxedByMassage";
        private const string ArmHediffDefName = "LiAIChat_MassageArmEfficiency";
        private const string LegHediffDefName = "LiAIChat_MassageLegEfficiency";
        private const string ManipulationCoreTag = "ManipulationLimbCore";
        private const string MovingCoreTag = "MovingLimbCore";

        public static void Apply(Pawn pawn)
        {
            if (pawn?.health?.hediffSet == null)
                return;

            List<BodyPartRecord> naturalArms = NaturalLimbCores(pawn, ManipulationCoreTag);
            List<BodyPartRecord> naturalLegs = NaturalLimbCores(pawn, MovingCoreTag);
            RefreshMoodMemory(pawn, naturalArms.Count + naturalLegs.Count);
            RefreshLimbHediffs(pawn, naturalArms, naturalLegs);
        }

        public static bool IsNaturalLimbCore(Pawn pawn, BodyPartRecord part)
        {
            if (pawn?.health?.hediffSet == null || part == null || pawn.health.hediffSet.PartIsMissing(part))
                return false;
            return pawn.health.hediffSet.GetDirectlyAddedPartFor(part) == null;
        }

        public static int NaturalLimbCount(Pawn pawn)
        {
            return NaturalLimbCores(pawn, ManipulationCoreTag).Count +
                   NaturalLimbCores(pawn, MovingCoreTag).Count;
        }

        private static List<BodyPartRecord> NaturalLimbCores(Pawn pawn, string tagName)
        {
            if (pawn?.RaceProps?.body?.AllParts == null)
                return new List<BodyPartRecord>();

            return pawn.RaceProps.body.AllParts
                .Where(part => part?.def?.tags != null &&
                    part.def.tags.Any(tag => tag != null && tag.defName == tagName) &&
                    IsNaturalLimbCore(pawn, part))
                .ToList();
        }

        private static void RefreshMoodMemory(Pawn pawn, int naturalLimbCount)
        {
            if (pawn.needs?.mood?.thoughts?.memories == null)
                return;

            ThoughtDef thoughtDef = DefDatabase<ThoughtDef>.GetNamedSilentFail(MoodThoughtDefName);
            if (thoughtDef == null)
                return;

            MemoryThoughtHandler memories = pawn.needs.mood.thoughts.memories;
            Thought_Memory oldMemory = memories.GetFirstMemoryOfDef(thoughtDef);
            if (oldMemory != null)
                memories.RemoveMemory(oldMemory);

            memories.TryGainMemory(thoughtDef, pawn);
            Thought_Memory memory = memories.GetFirstMemoryOfDef(thoughtDef);
            memory?.SetForcedStage(Mathf.Clamp(naturalLimbCount, 0, 4));
        }

        private static void RefreshLimbHediffs(Pawn pawn, List<BodyPartRecord> naturalArms,
            List<BodyPartRecord> naturalLegs)
        {
            HediffDef armDef = DefDatabase<HediffDef>.GetNamedSilentFail(ArmHediffDefName);
            HediffDef legDef = DefDatabase<HediffDef>.GetNamedSilentFail(LegHediffDefName);
            if (armDef == null || legDef == null)
                return;

            List<Hediff> old = pawn.health.hediffSet.hediffs
                .Where(hediff => hediff?.def == armDef || hediff?.def == legDef).ToList();
            for (int i = 0; i < old.Count; i++)
                pawn.health.RemoveHediff(old[i]);

            for (int i = 0; i < naturalArms.Count; i++)
                pawn.health.AddHediff(HediffMaker.MakeHediff(armDef, pawn, naturalArms[i]));
            for (int i = 0; i < naturalLegs.Count; i++)
                pawn.health.AddHediff(HediffMaker.MakeHediff(legDef, pawn, naturalLegs[i]));
        }
    }

    public class HediffCompProperties_MassageLimb : HediffCompProperties
    {
        public HediffCompProperties_MassageLimb()
        {
            compClass = typeof(HediffComp_MassageLimb);
        }
    }

    public class HediffComp_MassageLimb : HediffComp
    {
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (Pawn != null && Pawn.IsHashIntervalTick(60) &&
                !MassageEffectUtility.IsNaturalLimbCore(Pawn, parent.Part))
                Pawn.health.RemoveHediff(parent);
        }
    }

    [HarmonyPatch(typeof(Thought_Memory), "MoodOffset")]
    public static class MassageMoodThoughtPatch
    {
        public static void Postfix(Thought_Memory __instance, ref float __result)
        {
            if (__instance?.def?.defName == "LiAIChat_RelaxedByMassage")
                __result = 3f + 2f * MassageEffectUtility.NaturalLimbCount(__instance.pawn);
        }
    }

    public class JobDriver_EnjoyMassage : JobDriver
    {
        private Building_MassageChair Chair => job.targetA.Thing as Building_MassageChair;
        private CompMassageChair Massage => Chair?.GetComp<CompMassageChair>();

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return Chair != null && pawn.Reserve(Chair, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOn(() => Chair == null || Massage == null || !Massage.Powered || pawn.Drafted || pawn.Downed);

            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.OnCell);

            Toil massage = ToilMaker.MakeToil("EnjoyMassage");
            massage.defaultCompleteMode = ToilCompleteMode.Delay;
            massage.defaultDuration = CompMassageChair.ActiveMassageTicks;
            massage.tickAction = () =>
            {
                if (pawn.IsHashIntervalTick(10) && pawn.needs?.joy != null)
                    pawn.needs.joy.GainJoy(CompMassageChair.ActiveJoyPerTick * 10f, JoyKindDefOf.Meditative);
            };
            massage.WithProgressBarToilDelay(TargetIndex.A);
            yield return massage;

            Toil finish = ToilMaker.MakeToil("FinishMassage");
            finish.initAction = () => MassageEffectUtility.Apply(pawn);
            finish.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return finish;
        }
    }
}
