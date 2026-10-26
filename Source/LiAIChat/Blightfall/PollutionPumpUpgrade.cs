using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace LiAIChat.Blightfall
{
    public static class PollutionPumpUpgradeDefInjector
    {
        public static void EnsureUpgradeComp()
        {
            if (!ModsConfig.BiotechActive)
                return;

            ThingDef pump = DefDatabase<ThingDef>.GetNamedSilentFail("PollutionPump");
            if (pump == null)
            {
                Log.Warning("[LiAIChat] Biotech is active but PollutionPump ThingDef was not found.");
                return;
            }

            if (pump.comps == null)
                pump.comps = new List<CompProperties>();
            if (pump.comps.Any(props => props?.compClass == typeof(CompPollutionPumpUpgrade)))
                return;

            pump.comps.Add(new CompProperties_PollutionPumpUpgrade());
            Log.Message("[LiAIChat] Added upgrade component to PollutionPump ThingDef.");
        }
    }

    public class CompProperties_PollutionPumpUpgrade : CompProperties
    {
        public CompProperties_PollutionPumpUpgrade()
        {
            compClass = typeof(CompPollutionPumpUpgrade);
        }
    }

    public class CompPollutionPumpUpgrade : ThingComp
    {
        public const int MaximumUpgradeLevel = 4;
        public const int HoursSavedPerUpgrade = 1;
        public const int RadiusAddedPerUpgrade = 1;
        public const int SteelCost = 150;
        public const int AdvancedComponentCost = 1;
        public const int TicksPerHour = 2500;

        private static readonly MethodInfo MemberwiseCloneMethod = typeof(object).GetMethod(
            "MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo TicksUntilPumpField = typeof(CompPollutionPump).GetField(
            "ticksUntilPump", BindingFlags.Instance | BindingFlags.NonPublic);

        private int upgradeLevel;
        private CompProperties_PollutionPump sourceProperties;
        private CompProperties_PollutionPump adjustedProperties;
        private int cachedLevel = -1;

        public int UpgradeLevel => Mathf.Clamp(upgradeLevel, 0, MaximumUpgradeLevel);

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref upgradeLevel, "pollutionPumpUpgradeLevel", 0);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                upgradeLevel = Mathf.Clamp(upgradeLevel, 0, MaximumUpgradeLevel);
                InvalidateAdjustedProperties();
            }
        }

        public CompProperties_PollutionPump GetAdjustedProperties(CompProperties_PollutionPump original)
        {
            if (original == null || UpgradeLevel <= 0 || MemberwiseCloneMethod == null)
                return original;

            if (adjustedProperties == null || sourceProperties != original || cachedLevel != UpgradeLevel)
            {
                adjustedProperties = MemberwiseCloneMethod.Invoke(original, null) as CompProperties_PollutionPump;
                sourceProperties = original;
                cachedLevel = UpgradeLevel;
                if (adjustedProperties != null)
                {
                    adjustedProperties.radius = original.radius + UpgradeLevel * RadiusAddedPerUpgrade;
                    adjustedProperties.intervalTicks = Math.Max(TicksPerHour,
                        original.intervalTicks - UpgradeLevel * HoursSavedPerUpgrade * TicksPerHour);
                }
            }

            return adjustedProperties ?? original;
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo gizmo in base.CompGetGizmosExtra())
                yield return gizmo;

            if (!ModsConfig.BiotechActive || parent?.Map == null || parent.GetComp<CompPollutionPump>() == null)
                yield break;

            CompProperties_PollutionPump current = parent.GetComp<CompPollutionPump>().Props;
            string disabledReason = GetUpgradeDisabledReason();
            yield return new Command_Action
            {
                defaultLabel = "LiAIChat_PollutionPumpUpgradeLabel".Translate(UpgradeLevel, MaximumUpgradeLevel),
                defaultDesc = "LiAIChat_PollutionPumpUpgradeDescription".Translate(
                    TicksToHours(current.intervalTicks), current.radius,
                    HoursSavedPerUpgrade, RadiusAddedPerUpgrade, SteelCost, AdvancedComponentCost),
                icon = TexCommand.Install,
                Disabled = disabledReason != null,
                disabledReason = disabledReason,
                action = OpenWorkerMenu
            };
        }

        private void OpenWorkerMenu()
        {
            if (parent?.Map == null)
                return;

            string disabledReason = GetUpgradeDisabledReason();
            if (disabledReason != null)
            {
                Messages.Message(disabledReason, MessageTypeDefOf.RejectInput, false);
                return;
            }

            List<FloatMenuOption> options = parent.Map.mapPawns.FreeColonistsSpawned
                .Where(pawn => pawn != null && !pawn.Downed && !pawn.Drafted && !pawn.InMentalState &&
                    pawn.CanReserveAndReach(parent, PathEndMode.Touch, Danger.Some))
                .Select(pawn => new FloatMenuOption(
                    "LiAIChat_PollutionPumpUpgradeWorkerOption".Translate(pawn.LabelShortCap),
                    () => StartUpgradeJob(pawn)))
                .ToList();

            if (options.Count == 0)
                options.Add(new FloatMenuOption("LiAIChat_PollutionPumpUpgradeNoWorker".Translate(), null));

            Find.WindowStack.Add(new FloatMenu(options));
        }

        private void StartUpgradeJob(Pawn pawn)
        {
            if (pawn == null || parent?.Map == null)
                return;

            JobDef jobDef = DefDatabase<JobDef>.GetNamedSilentFail("LiAIChat_UpgradePollutionPump");
            if (jobDef == null)
            {
                Log.ErrorOnce("[LiAIChat] Missing JobDef LiAIChat_UpgradePollutionPump.",
                    "LiAIChat_UpgradePollutionPumpJobDefMissing".GetHashCode());
                return;
            }

            pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(jobDef, parent), JobTag.Misc);
        }

        public bool CompleteUpgrade(
            List<Thing> reservedSteelStacks,
            List<int> reservedSteelCounts,
            List<Thing> reservedComponentStacks,
            List<int> reservedComponentCounts)
        {
            if (parent?.Map == null || UpgradeLevel >= MaximumUpgradeLevel ||
                !CanConsumeReserved(reservedSteelStacks, reservedSteelCounts, SteelCost) ||
                !CanConsumeReserved(reservedComponentStacks, reservedComponentCounts, AdvancedComponentCost))
            {
                Messages.Message("LiAIChat_PollutionPumpUpgradeMaterialsLost".Translate(),
                    MessageTypeDefOf.RejectInput, false);
                return false;
            }

            ConsumeReserved(reservedSteelStacks, reservedSteelCounts);
            ConsumeReserved(reservedComponentStacks, reservedComponentCounts);
            upgradeLevel = Mathf.Clamp(upgradeLevel + 1, 0, MaximumUpgradeLevel);
            InvalidateAdjustedProperties();

            CompPollutionPump pump = parent.GetComp<CompPollutionPump>();
            CompProperties_PollutionPump current = pump.Props;
            if (TicksUntilPumpField != null)
            {
                int remainingTicks = (int)TicksUntilPumpField.GetValue(pump);
                if (remainingTicks > current.intervalTicks)
                    TicksUntilPumpField.SetValue(pump, current.intervalTicks);
            }

            parent.Map.mapDrawer.MapMeshDirty(parent.Position, MapMeshFlagDefOf.Buildings);
            Messages.Message("LiAIChat_PollutionPumpUpgradeComplete".Translate(
                parent.LabelCap, UpgradeLevel, MaximumUpgradeLevel, TicksToHours(current.intervalTicks), current.radius),
                parent, MessageTypeDefOf.PositiveEvent);
            return true;
        }

        private string GetUpgradeDisabledReason()
        {
            if (UpgradeLevel >= MaximumUpgradeLevel)
                return "LiAIChat_PollutionPumpUpgradeMaxed".Translate();
            if (parent?.Map?.reservationManager?.IsReserved(parent) == true)
                return "LiAIChat_PollutionPumpUpgradeInProgress".Translate();

            ThingDef advancedComponent = ThingDefOf.ComponentSpacer;
            if (ThingDefOf.Steel == null || advancedComponent == null)
                return "LiAIChat_PollutionPumpUpgradeDefsMissing".Translate();

            return HasResources(ThingDefOf.Steel, SteelCost) &&
                HasResources(advancedComponent, AdvancedComponentCost)
                ? null
                : "LiAIChat_PollutionPumpUpgradeMaterialsMissing".Translate();
        }

        private bool HasResources(ThingDef def, int count)
        {
            return AvailableResources(def).Sum(thing => thing.stackCount) >= count;
        }

        private IEnumerable<Thing> AvailableResources(ThingDef def)
        {
            if (def == null || parent?.Map == null)
                return Enumerable.Empty<Thing>();

            return parent.Map.listerThings.ThingsOfDef(def)
                .Where(thing => thing != null && thing.Spawned && !thing.Destroyed &&
                    !thing.IsForbidden(Faction.OfPlayer))
                .OrderBy(thing => thing.Position.DistanceToSquared(parent.Position));
        }

        private static bool CanConsumeReserved(List<Thing> stacks, List<int> counts, int required)
        {
            if (stacks == null || counts == null || stacks.Count != counts.Count || counts.Sum() != required)
                return false;

            for (int i = 0; i < stacks.Count; i++)
            {
                if (stacks[i] == null || stacks[i].Destroyed || !stacks[i].Spawned ||
                    stacks[i].stackCount < counts[i])
                    return false;
            }

            return true;
        }

        private static void ConsumeReserved(List<Thing> stacks, List<int> counts)
        {
            for (int i = 0; i < stacks.Count; i++)
                stacks[i].SplitOff(counts[i]).Destroy(DestroyMode.Vanish);
        }

        private void InvalidateAdjustedProperties()
        {
            sourceProperties = null;
            adjustedProperties = null;
            cachedLevel = -1;
        }

        private static int TicksToHours(int ticks) => Mathf.Max(1, Mathf.RoundToInt(ticks / (float)TicksPerHour));
    }

    public class JobDriver_UpgradePollutionPump : JobDriver
    {
        private const float ConstructionWorkRequired = 4000f;

        private List<Thing> reservedSteelStacks = new List<Thing>();
        private List<int> reservedSteelCounts = new List<int>();
        private List<Thing> reservedComponentStacks = new List<Thing>();
        private List<int> reservedComponentCounts = new List<int>();

        private ThingWithComps Pump => job.targetA.Thing as ThingWithComps;
        private CompPollutionPumpUpgrade Upgrade => Pump?.GetComp<CompPollutionPumpUpgrade>();

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref reservedSteelStacks, "reservedSteelStacks", LookMode.Reference);
            Scribe_Collections.Look(ref reservedSteelCounts, "reservedSteelCounts", LookMode.Value);
            Scribe_Collections.Look(ref reservedComponentStacks, "reservedComponentStacks", LookMode.Reference);
            Scribe_Collections.Look(ref reservedComponentCounts, "reservedComponentCounts", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                reservedSteelStacks ??= new List<Thing>();
                reservedSteelCounts ??= new List<int>();
                reservedComponentStacks ??= new List<Thing>();
                reservedComponentCounts ??= new List<int>();
            }
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (Pump == null || Upgrade == null || Upgrade.UpgradeLevel >= CompPollutionPumpUpgrade.MaximumUpgradeLevel ||
                !pawn.Reserve(Pump, job, 1, -1, null, errorOnFailed))
                return false;

            if (!TryReserveMaterials(ThingDefOf.Steel, CompPollutionPumpUpgrade.SteelCost,
                    reservedSteelStacks, reservedSteelCounts) ||
                !TryReserveMaterials(ThingDefOf.ComponentSpacer, CompPollutionPumpUpgrade.AdvancedComponentCost,
                    reservedComponentStacks, reservedComponentCounts))
            {
                ReleaseReservations();
                return false;
            }

            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOn(() => Upgrade == null ||
                Upgrade.UpgradeLevel >= CompPollutionPumpUpgrade.MaximumUpgradeLevel);

            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

            float workDone = 0f;
            Toil construction = ToilMaker.MakeToil("UpgradePollutionPump");
            construction.defaultCompleteMode = ToilCompleteMode.Never;
            construction.tickAction = () =>
            {
                if (Pump == null || Upgrade == null)
                {
                    pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
                    return;
                }

                pawn.rotationTracker.FaceTarget(Pump);
                workDone += Math.Max(0.1f, pawn.GetStatValue(StatDefOf.ConstructionSpeed));
                if (workDone >= ConstructionWorkRequired)
                    ReadyForNextToil();
            };
            construction.WithProgressBar(TargetIndex.A, () => Mathf.Clamp01(workDone / ConstructionWorkRequired),
                false, 0.15f, false);
            construction.WithEffect(EffecterDefOf.ConstructDirt, TargetIndex.A);
            construction.PlaySustainerOrSound(SoundDefOf.Interact_ConstructDirt, 1f);
            construction.activeSkill = () => SkillDefOf.Construction;
            yield return construction;

            Toil finish = ToilMaker.MakeToil("FinishUpgradePollutionPump");
            finish.initAction = () =>
            {
                if (Upgrade == null || !Upgrade.CompleteUpgrade(reservedSteelStacks, reservedSteelCounts,
                        reservedComponentStacks, reservedComponentCounts))
                    pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
            };
            finish.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return finish;
        }

        private bool TryReserveMaterials(ThingDef def, int required, List<Thing> stacks, List<int> counts)
        {
            int remaining = required;
            foreach (Thing stack in pawn.Map.listerThings.ThingsOfDef(def)
                         .Where(thing => thing != null && thing.Spawned && !thing.Destroyed &&
                             !thing.IsForbidden(Faction.OfPlayer))
                         .OrderBy(thing => thing.Position.DistanceToSquared(Pump.Position)))
            {
                int available = pawn.Map.reservationManager.CanReserveStack(pawn, stack, 1, null, false);
                int amount = Math.Min(remaining, Math.Min(stack.stackCount, available));
                if (amount <= 0 || !pawn.Reserve(stack, job, 1, amount, null, false))
                    continue;

                stacks.Add(stack);
                counts.Add(amount);
                remaining -= amount;
                if (remaining <= 0)
                    return true;
            }

            return false;
        }

        private void ReleaseReservations()
        {
            foreach (Thing stack in reservedSteelStacks.Concat(reservedComponentStacks))
                pawn.Map?.reservationManager?.Release(stack, pawn, job);
            if (Pump != null)
                pawn.Map?.reservationManager?.Release(Pump, pawn, job);
        }
    }

    [HarmonyPatch(typeof(CompPollutionPump), "get_Props")]
    public static class PollutionPumpUpgradePropertiesPatch
    {
        public static void Postfix(CompPollutionPump __instance, ref CompProperties_PollutionPump __result)
        {
            CompPollutionPumpUpgrade upgrade = __instance?.parent?.GetComp<CompPollutionPumpUpgrade>();
            if (upgrade != null)
                __result = upgrade.GetAdjustedProperties(__result);
        }
    }
}
