using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

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
                action = Upgrade
            };
        }

        private void Upgrade()
        {
            if (parent?.Map == null)
                return;

            string disabledReason = GetUpgradeDisabledReason();
            if (disabledReason != null)
            {
                Messages.Message(disabledReason, MessageTypeDefOf.RejectInput, false);
                return;
            }

            ConsumeResources(ThingDefOf.Steel, SteelCost);
            ConsumeResources(ThingDefOf.ComponentSpacer, AdvancedComponentCost);
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
        }

        private string GetUpgradeDisabledReason()
        {
            if (UpgradeLevel >= MaximumUpgradeLevel)
                return "LiAIChat_PollutionPumpUpgradeMaxed".Translate();

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

        private void ConsumeResources(ThingDef def, int count)
        {
            int remaining = count;
            foreach (Thing stack in AvailableResources(def).ToList())
            {
                int toConsume = Math.Min(remaining, stack.stackCount);
                stack.SplitOff(toConsume).Destroy(DestroyMode.Vanish);
                remaining -= toConsume;
                if (remaining <= 0)
                    return;
            }
        }

        private void InvalidateAdjustedProperties()
        {
            sourceProperties = null;
            adjustedProperties = null;
            cachedLevel = -1;
        }

        private static int TicksToHours(int ticks) => Mathf.Max(1, Mathf.RoundToInt(ticks / (float)TicksPerHour));
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
