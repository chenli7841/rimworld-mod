using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace LiAIChat.Traits
{
    internal static class OpportunisticSleeperUtility
    {
        private const string TraitDefName = "LiAIChat_OpportunisticSleeper";
        private const string JobDefName = "LiAIChat_OpportunisticSleep";
        private const int BedSearchRadius = 15;
        private const int FireDangerRadius = 12;

        public static bool HasTrait(Pawn pawn)
        {
            TraitDef trait = DefDatabase<TraitDef>.GetNamedSilentFail(TraitDefName);
            return trait != null && pawn?.story?.traits?.HasTrait(trait) == true;
        }

        public static bool IsOpportunisticSleepJob(Pawn pawn)
        {
            return pawn?.CurJob?.def?.defName == JobDefName;
        }

        public static bool CanSleepSafely(Pawn pawn, IntVec3 cell)
        {
            Map map = pawn?.Map;
            if (map == null || !cell.IsValid || !cell.InBounds(map))
                return false;

            if (!GenTemperature.SafeTemperatureAtCell(pawn, cell, map))
                return false;

            if (GenHostility.AnyHostileActiveThreatToPlayer(map, false, false))
                return false;

            int fireRadiusSquared = FireDangerRadius * FireDangerRadius;
            if (map.listerThings.ThingsInGroup(ThingRequestGroup.Fire)
                .OfType<Fire>()
                .Any(fire => fire.Spawned && fire.Position.DistanceToSquared(cell) <= fireRadiusSquared))
                return false;

            return true;
        }

        public static bool CanStartJob(Pawn pawn)
        {
            if (pawn == null || pawn.Dead || pawn.Downed || pawn.Drafted || pawn.InMentalState ||
                !pawn.IsColonistPlayerControlled || !HasTrait(pawn) || pawn.Map == null ||
                pawn.needs?.rest == null || pawn.health?.capacities == null ||
                pawn.health.capacities.GetLevel(PawnCapacityDefOf.Consciousness) < 0.5f)
                return false;

            return CanSleepSafely(pawn, pawn.Position);
        }

        public static Building_Bed FindNearbyBed(Pawn pawn)
        {
            if (pawn?.Map == null)
                return null;

            int radiusSquared = BedSearchRadius * BedSearchRadius;
            return pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.Bed)
                .OfType<Building_Bed>()
                .Where(bed => bed.Spawned && !bed.Destroyed && bed.SleepingSlotsCount > 0 &&
                    bed.AnyUnoccupiedSleepingSlot && !bed.IsForbidden(pawn) &&
                    bed.Position.DistanceToSquared(pawn.Position) <= radiusSquared &&
                    RestUtility.CanUseBedNow(bed, pawn, true, false, null) &&
                    pawn.CanReserveAndReach(bed, PathEndMode.OnCell, Danger.None))
                .Where(bed =>
                {
                    IntVec3 slot = RestUtility.GetBedSleepingSlotPosFor(pawn, bed);
                    return slot.IsValid && CanSleepSafely(pawn, slot);
                })
                .OrderBy(bed => bed.Position.DistanceToSquared(pawn.Position))
                .FirstOrDefault();
        }
    }

    public class JobGiver_OpportunisticSleep : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            if (!OpportunisticSleeperUtility.CanStartJob(pawn))
                return null;

            JobDef jobDef = DefDatabase<JobDef>.GetNamedSilentFail("LiAIChat_OpportunisticSleep");
            if (jobDef == null)
                return null;

            Building_Bed bed = OpportunisticSleeperUtility.FindNearbyBed(pawn);
            return bed != null
                ? JobMaker.MakeJob(jobDef, bed)
                : JobMaker.MakeJob(jobDef, pawn.Position);
        }
    }

    public class JobDriver_OpportunisticSleep : JobDriver_LayDown
    {
        // Keep the normal bed/sleep toil's periodic override checks so important work can wake the pawn.
        public override bool LookForOtherJobs => true;
    }

    [HarmonyPatch(typeof(RestUtility), nameof(RestUtility.CanFallAsleep), new[] { typeof(Pawn) })]
    internal static class OpportunisticSleeperCanFallAsleepPatch
    {
        public static void Postfix(Pawn __0, ref bool __result)
        {
            if (OpportunisticSleeperUtility.IsOpportunisticSleepJob(__0) &&
                OpportunisticSleeperUtility.CanSleepSafely(__0, __0.Position))
                __result = true;
        }
    }

    [HarmonyPatch(typeof(RestUtility), nameof(RestUtility.ShouldWakeUp), new[] { typeof(Pawn) })]
    internal static class OpportunisticSleeperShouldWakeUpPatch
    {
        public static void Postfix(Pawn __0, ref bool __result)
        {
            if (OpportunisticSleeperUtility.IsOpportunisticSleepJob(__0) &&
                __0.needs?.rest?.CurLevelPercentage >= 0.999f &&
                OpportunisticSleeperUtility.CanSleepSafely(__0, __0.Position))
                __result = false;
        }
    }
}
