using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace LiAIChat.AlliedSettlementSurvival
{
    public class CompProperties_WastepackAtomizerUpgrade : CompProperties
    {
        public CompProperties_WastepackAtomizerUpgrade()
        {
            compClass = typeof(CompWastepackAtomizerUpgrade);
        }
    }

    public class CompWastepackAtomizerUpgrade : ThingComp
    {
        public const int MaximumUpgradeLevel = 8;
        public const int HoursSavedPerUpgrade = 1;
        public const int CapacityAddedPerUpgrade = 5;
        public const int SteelCost = 100;
        public const int PlasteelCost = 50;
        public const int NanostructuringChipCost = 1;
        public const int TicksPerHour = 2500;

        private const string PlasteelDefName = "Plasteel";
        private int upgradeLevel;

        public int UpgradeLevel => Mathf.Clamp(upgradeLevel, 0, MaximumUpgradeLevel);

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref upgradeLevel, "wastepackAtomizerUpgradeLevel", 0);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
                upgradeLevel = Mathf.Clamp(upgradeLevel, 0, MaximumUpgradeLevel);
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo gizmo in base.CompGetGizmosExtra())
                yield return gizmo;

            if (!ModsConfig.BiotechActive || parent?.Map == null)
                yield break;

            ThingDef plasteel = PlasteelDef;
            ThingDef chip = DefDatabase<ThingDef>.GetNamedSilentFail("NanostructuringChip");
            string disabledReason = GetUpgradeDisabledReason(plasteel);
            int currentHours = Math.Max(4, 12 - UpgradeLevel);
            int currentCapacity = 10 + UpgradeLevel * CapacityAddedPerUpgrade;
            yield return new Command_Action
            {
                defaultLabel = "LiAIChat_AtomizerUpgradeLabel".Translate(UpgradeLevel, MaximumUpgradeLevel),
                defaultDesc = "LiAIChat_AtomizerUpgradeDescription".Translate(
                    currentHours, currentCapacity, HoursSavedPerUpgrade, CapacityAddedPerUpgrade,
                    NanostructuringChipCost, chip?.LabelCap ?? "NanostructuringChip",
                    SteelCost, PlasteelCost, plasteel?.LabelCap ?? "Plasteel"),
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

            ThingDef chip = DefDatabase<ThingDef>.GetNamedSilentFail("NanostructuringChip");
            ThingDef plasteel = PlasteelDef;
            string reason = GetUpgradeDisabledReason(plasteel);
            if (reason != null)
            {
                Messages.Message(reason, MessageTypeDefOf.RejectInput, false);
                return;
            }

            var costs = new List<ResourceCost>
            {
                new ResourceCost(chip, NanostructuringChipCost),
                new ResourceCost(ThingDefOf.Steel, SteelCost),
                new ResourceCost(plasteel, PlasteelCost)
            };
            if (!HasAllResources(costs))
            {
                Messages.Message("LiAIChat_AtomizerUpgradeMaterialsMissing".Translate(),
                    MessageTypeDefOf.RejectInput, false);
                return;
            }

            foreach (ResourceCost cost in costs)
                ConsumeResources(cost);

            upgradeLevel = Mathf.Clamp(upgradeLevel + 1, 0, MaximumUpgradeLevel);
            parent.Map.mapDrawer.MapMeshDirty(parent.Position, MapMeshFlagDefOf.Buildings);
            Messages.Message("LiAIChat_AtomizerUpgradeComplete".Translate(
                parent.LabelCap, UpgradeLevel, MaximumUpgradeLevel, 12 - UpgradeLevel,
                10 + UpgradeLevel * CapacityAddedPerUpgrade), parent,
                MessageTypeDefOf.PositiveEvent);
        }

        private string GetUpgradeDisabledReason(ThingDef plasteel)
        {
            if (UpgradeLevel >= MaximumUpgradeLevel)
                return "LiAIChat_AtomizerUpgradeMaxed".Translate();
            if (DefDatabase<ThingDef>.GetNamedSilentFail("NanostructuringChip") == null ||
                ThingDefOf.Steel == null || plasteel == null)
                return "LiAIChat_AtomizerUpgradeDefsMissing".Translate(
                    plasteel == null ? PlasteelDefName : "NanostructuringChip / Steel");

            var costs = new List<ResourceCost>
            {
                new ResourceCost(DefDatabase<ThingDef>.GetNamedSilentFail("NanostructuringChip"), NanostructuringChipCost),
                new ResourceCost(ThingDefOf.Steel, SteelCost),
                new ResourceCost(plasteel, PlasteelCost)
            };
            return HasAllResources(costs) ? null : "LiAIChat_AtomizerUpgradeMaterialsMissing".Translate();
        }

        private bool HasAllResources(List<ResourceCost> costs)
        {
            return costs.All(cost => AvailableResources(cost.def).Sum(thing => thing.stackCount) >= cost.count);
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

        private void ConsumeResources(ResourceCost cost)
        {
            int remaining = cost.count;
            foreach (Thing stack in AvailableResources(cost.def).ToList())
            {
                int count = Math.Min(remaining, stack.stackCount);
                Thing consumed = stack.SplitOff(count);
                consumed.Destroy(DestroyMode.Vanish);
                remaining -= count;
                if (remaining <= 0)
                    break;
            }
        }

        private static ThingDef PlasteelDef => DefDatabase<ThingDef>.GetNamedSilentFail(PlasteelDefName);

        private struct ResourceCost
        {
            public readonly ThingDef def;
            public readonly int count;

            public ResourceCost(ThingDef def, int count)
            {
                this.def = def;
                this.count = count;
            }
        }
    }

    [HarmonyPatch(typeof(CompAtomizer), "get_TicksPerAtomize")]
    public static class WastepackAtomizerProcessingTimePatch
    {
        public static void Postfix(CompAtomizer __instance, ref int __result)
        {
            int level = __instance?.parent?.GetComp<CompWastepackAtomizerUpgrade>()?.UpgradeLevel ?? 0;
            __result = Math.Max(CompWastepackAtomizerUpgrade.TicksPerHour,
                __result - level * CompWastepackAtomizerUpgrade.HoursSavedPerUpgrade *
                CompWastepackAtomizerUpgrade.TicksPerHour);
        }
    }

    [HarmonyPatch(typeof(CompAtomizer), "get_SpaceLeft")]
    public static class WastepackAtomizerCapacityPatch
    {
        public static void Postfix(CompAtomizer __instance, ref int __result)
        {
            int level = __instance?.parent?.GetComp<CompWastepackAtomizerUpgrade>()?.UpgradeLevel ?? 0;
            __result += level * CompWastepackAtomizerUpgrade.CapacityAddedPerUpgrade;
        }
    }
}
