using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;

namespace LiAIChat.Blightfall
{
    public class BlightTechnologyProjectExtension : DefModExtension
    {
        public string blueprintDefName;
        public int researchDataCost;
    }

    public enum BlightTechnologyTier
    {
        Basic,
        Advanced,
        Core
    }

    public class BlightTechnologyGameComponent : GameComponent
    {
        private List<string> unlockedBlueprints = new List<string>();
        private List<string> paidProjects = new List<string>();
        private int wasteDumpRewardsSinceBlueprint;
        private int advancedSiteRewardsSinceBlueprint;
        private int coreSiteRewardsSinceBlueprint;

        public BlightTechnologyGameComponent(Verse.Game game) { }

        public static BlightTechnologyGameComponent Instance
        {
            get { return Current.Game == null ? null : Current.Game.GetComponent<BlightTechnologyGameComponent>(); }
        }

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref unlockedBlueprints, "blightTechnologyUnlockedBlueprints", LookMode.Value);
            Scribe_Collections.Look(ref paidProjects, "blightTechnologyPaidProjects", LookMode.Value);
            Scribe_Values.Look(ref wasteDumpRewardsSinceBlueprint, "blightWasteDumpRewardsSinceBlueprint", 0);
            Scribe_Values.Look(ref advancedSiteRewardsSinceBlueprint, "blightAdvancedSiteRewardsSinceBlueprint", 0);
            Scribe_Values.Look(ref coreSiteRewardsSinceBlueprint, "blightCoreSiteRewardsSinceBlueprint", 0);
            if (unlockedBlueprints == null) unlockedBlueprints = new List<string>();
            if (paidProjects == null) paidProjects = new List<string>();
            wasteDumpRewardsSinceBlueprint = Mathf.Clamp(wasteDumpRewardsSinceBlueprint, 0, 4);
            advancedSiteRewardsSinceBlueprint = Mathf.Clamp(advancedSiteRewardsSinceBlueprint, 0, 4);
            coreSiteRewardsSinceBlueprint = Mathf.Clamp(coreSiteRewardsSinceBlueprint, 0, 4);
        }

        public bool IsBlueprintUnlocked(string defName)
        {
            return !string.IsNullOrEmpty(defName) && unlockedBlueprints.Contains(defName);
        }

        public bool UnlockBlueprint(string defName)
        {
            if (string.IsNullOrEmpty(defName) || IsBlueprintUnlocked(defName)) return false;
            unlockedBlueprints.Add(defName);
            return true;
        }

        public bool HasPaidFor(ResearchProjectDef project)
        {
            return project != null && paidProjects.Contains(project.defName);
        }

        public void RecordPayment(ResearchProjectDef project)
        {
            if (project != null && !HasPaidFor(project)) paidProjects.Add(project.defName);
        }

        public bool RecordWasteDumpSuccessAndShouldGuaranteeBlueprint()
        {
            return RecordSiteSuccessAndShouldGuaranteeBlueprint(BlightTechnologyTier.Basic);
        }

        public bool RecordSiteSuccessAndShouldGuaranteeBlueprint(BlightTechnologyTier tier)
        {
            if (!BlightTechnologyUtility.HasUndecodedBlueprintsForTier(tier))
            {
                SetBlueprintRewardProgress(tier, 0);
                return false;
            }
            int progress = Mathf.Min(4, BlueprintRewardProgress(tier) + 1);
            SetBlueprintRewardProgress(tier, progress);
            return progress >= 4;
        }

        public void RecordNewBlueprintReward()
        {
            wasteDumpRewardsSinceBlueprint = 0;
        }

        public void RecordNewBlueprintReward(BlightTechnologyTier tier) => SetBlueprintRewardProgress(tier, 0);

        private int BlueprintRewardProgress(BlightTechnologyTier tier) => tier switch
        {
            BlightTechnologyTier.Advanced => advancedSiteRewardsSinceBlueprint,
            BlightTechnologyTier.Core => coreSiteRewardsSinceBlueprint,
            _ => wasteDumpRewardsSinceBlueprint
        };

        private void SetBlueprintRewardProgress(BlightTechnologyTier tier, int value)
        {
            switch (tier)
            {
                case BlightTechnologyTier.Advanced: advancedSiteRewardsSinceBlueprint = value; break;
                case BlightTechnologyTier.Core: coreSiteRewardsSinceBlueprint = value; break;
                default: wasteDumpRewardsSinceBlueprint = value; break;
            }
        }
    }

    public static class BlightTechnologyUtility
    {
        public const string ResearchDataDefName = "LiAIChat_BlightResearchData";
        private static readonly string[] BasicBlueprints =
        {
            "LiAIChat_SporeBioengineeringBlueprint",
            "LiAIChat_CorrosiveTechnologyBlueprint",
            "LiAIChat_SymbioticArmorBlueprint"
        };
        private static readonly string[] AdvancedBlueprints = { "LiAIChat_ResonanceTechnologyBlueprint" };
        private static readonly string[] CoreBlueprints = { "LiAIChat_PhaseveilTechnologyBlueprint" };
        private static readonly string[] AllBlueprints = BasicBlueprints.Concat(AdvancedBlueprints).Concat(CoreBlueprints).ToArray();

        public static bool HasUndecodedBlueprints
        {
            get
            {
                BlightTechnologyGameComponent component = BlightTechnologyGameComponent.Instance;
                return AllBlueprints.Any(name => component == null || !component.IsBlueprintUnlocked(name));
            }
        }

        public static bool HasUndecodedBlueprintsForTier(BlightTechnologyTier tier) =>
            BlueprintNamesFor(tier).Any(name => BlightTechnologyGameComponent.Instance?.IsBlueprintUnlocked(name) != true);

        public static IReadOnlyList<string> BlueprintNamesFor(BlightTechnologyTier tier) => tier switch
        {
            BlightTechnologyTier.Advanced => AdvancedBlueprints,
            BlightTechnologyTier.Core => CoreBlueprints,
            _ => BasicBlueprints
        };

        public static bool IsBlueprint(ThingDef def)
        {
            return def != null && AllBlueprints.Contains(def.defName);
        }

        public static bool IsProjectUnlocked(ResearchProjectDef project)
        {
            BlightTechnologyProjectExtension extension = project?.GetModExtension<BlightTechnologyProjectExtension>();
            if (extension == null) return true;
            return BlightTechnologyGameComponent.Instance?.IsBlueprintUnlocked(extension.blueprintDefName) == true;
        }

        public static bool TryDecodeBlueprint(Pawn pawn, Thing blueprint)
        {
            if (pawn == null || blueprint == null || blueprint.Destroyed || !blueprint.Spawned ||
                blueprint.Map != pawn.Map || !IsBlueprint(blueprint.def))
                return false;
            if (!pawn.CanReach(blueprint, PathEndMode.Touch, Danger.Some))
            {
                Messages.Message("LiAIChat_BlightBlueprintUnreachable".Translate(), blueprint, MessageTypeDefOf.RejectInput, false);
                return false;
            }

            BlightTechnologyGameComponent component = BlightTechnologyGameComponent.Instance;
            if (component == null) return false;
            IntVec3 position = blueprint.Position;
            Map map = blueprint.Map;
            bool alreadyDecoded = component.IsBlueprintUnlocked(blueprint.def.defName);
            if (alreadyDecoded)
            {
                ThingDef dataDef = DefDatabase<ThingDef>.GetNamedSilentFail(ResearchDataDefName);
                if (dataDef == null) return false;
                Thing data = ThingMaker.MakeThing(dataDef);
                if (!GenPlace.TryPlaceThing(data, position, map, ThingPlaceMode.Near))
                {
                    if (!data.Destroyed) data.Destroy(DestroyMode.Vanish);
                    return false;
                }
                ConsumeOne(blueprint);
                Messages.Message("LiAIChat_BlightBlueprintDuplicate".Translate(), data, MessageTypeDefOf.PositiveEvent, false);
                return true;
            }

            component.UnlockBlueprint(blueprint.def.defName);
            ConsumeOne(blueprint);
            Messages.Message("LiAIChat_BlightBlueprintDecoded".Translate(blueprint.def.label), MessageTypeDefOf.PositiveEvent);
            return true;
        }

        public static bool HasResearchData(int count)
        {
            return CountResearchData() >= count;
        }

        public static bool TryConsumeResearchData(int count)
        {
            if (count <= 0) return true;
            List<Thing> stacks = ResearchDataStacks().ToList();
            if (stacks.Sum(thing => thing.stackCount) < count) return false;

            int remaining = count;
            foreach (Thing stack in stacks)
            {
                if (remaining <= 0) break;
                int consumed = Math.Min(remaining, stack.stackCount);
                if (consumed == stack.stackCount)
                    stack.Destroy(DestroyMode.Vanish);
                else
                    stack.SplitOff(consumed).Destroy(DestroyMode.Vanish);
                remaining -= consumed;
            }
            return remaining == 0;
        }

        public static int CountResearchData()
        {
            return ResearchDataStacks().Sum(thing => thing.stackCount);
        }

        public static Thing CreateResearchData(int count)
        {
            ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(ResearchDataDefName);
            if (def == null || count <= 0) return null;
            Thing data = ThingMaker.MakeThing(def);
            data.stackCount = count;
            return data;
        }

        public static bool TryGiveResearchData(Map map, int count)
        {
            Thing data = CreateResearchData(count);
            if (map == null || data == null) return false;
            if (GenPlace.TryPlaceThing(data, map.Center, map, ThingPlaceMode.Near)) return true;
            if (!data.Destroyed) data.Destroy(DestroyMode.Vanish);
            return false;
        }

        private static IEnumerable<Thing> ResearchDataStacks()
        {
            ThingDef dataDef = DefDatabase<ThingDef>.GetNamedSilentFail(ResearchDataDefName);
            if (dataDef == null || Current.Game == null) yield break;
            var seen = new HashSet<Thing>();

            if (Find.Maps != null)
            {
                foreach (Map map in Find.Maps)
                {
                    if (map == null || !map.IsPlayerHome) continue;
                    foreach (Thing thing in map.listerThings.ThingsOfDef(dataDef))
                        if (thing != null && !thing.Destroyed && seen.Add(thing)) yield return thing;
                    foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
                        foreach (Thing thing in InventoryStacks(pawn, dataDef))
                            if (seen.Add(thing)) yield return thing;
                }
            }

            if (Find.WorldObjects?.Caravans != null)
            {
                foreach (Caravan caravan in Find.WorldObjects.Caravans)
                {
                    if (caravan == null || !caravan.IsPlayerControlled) continue;
                    foreach (Pawn pawn in caravan.PawnsListForReading)
                        foreach (Thing thing in InventoryStacks(pawn, dataDef))
                            if (seen.Add(thing)) yield return thing;
                }
            }
        }

        private static IEnumerable<Thing> InventoryStacks(Pawn pawn, ThingDef def)
        {
            if (pawn?.inventory?.innerContainer == null) yield break;
            foreach (Thing thing in pawn.inventory.innerContainer)
                if (thing != null && thing.def == def && !thing.Destroyed) yield return thing;
        }

        private static void ConsumeOne(Thing thing)
        {
            if (thing == null || thing.Destroyed) return;
            if (thing.stackCount <= 1) thing.Destroy(DestroyMode.Vanish);
            else thing.SplitOff(1).Destroy(DestroyMode.Vanish);
        }
    }

    public enum BlightWasteDumpRewardType
    {
        Consumable,
        MedicalOrPurification
    }

    public static class BlightTechnologyRewards
    {
        private static readonly Dictionary<BlightWasteDumpRewardType, Func<Map, Thing>> Providers =
            new Dictionary<BlightWasteDumpRewardType, Func<Map, Thing>>();

        // Future Blightfall systems can register their special item factories here.
        public static void RegisterProvider(BlightWasteDumpRewardType type, Func<Map, Thing> provider)
        {
            if (provider == null) Providers.Remove(type);
            else Providers[type] = provider;
        }

        public static void AwardForWasteDump(Map map)
        {
            AwardForTier(map, BlightTechnologyTier.Basic);
        }

        public static void AwardForTier(Map map, BlightTechnologyTier tier)
        {
            if (map == null) return;
            BlightTechnologyGameComponent component = BlightTechnologyGameComponent.Instance;
            if (component == null) return;

            bool guaranteedBlueprint = component.RecordSiteSuccessAndShouldGuaranteeBlueprint(tier);
            float roll = Rand.Value;
            Thing reward = null;
            bool newBlueprint = false;

            if (guaranteedBlueprint || roll >= 0.90f)
            {
                reward = MakeUndecodedBlueprint(tier);
                newBlueprint = reward != null;
            }
            else if (roll >= 0.70f)
                reward = TryProvider(BlightWasteDumpRewardType.MedicalOrPurification, map);
            else if (roll >= 0.45f)
                reward = TryProvider(BlightWasteDumpRewardType.Consumable, map);

            if (reward == null)
                reward = MakeResearchData();
            if (reward == null)
            {
                Log.Warning("[LiAIChat Blight Technology] Could not create the Blightfall site reward.");
                return;
            }

            if (!GenPlace.TryPlaceThing(reward, map.Center, map, ThingPlaceMode.Near))
            {
                if (!reward.Destroyed) reward.Destroy(DestroyMode.Vanish);
                Log.Warning("[LiAIChat Blight Technology] Could not place the Blightfall site reward.");
                return;
            }

            if (newBlueprint)
            {
                component.RecordNewBlueprintReward(tier);
                Messages.Message("LiAIChat_BlightWasteDumpBlueprintReward".Translate(reward.LabelCap), reward,
                    MessageTypeDefOf.PositiveEvent, false);
            }
            else if (reward.def.defName == BlightTechnologyUtility.ResearchDataDefName)
                Messages.Message("LiAIChat_BlightWasteDumpDataReward".Translate(reward.stackCount), reward,
                    MessageTypeDefOf.PositiveEvent, false);
        }

        private static Thing TryProvider(BlightWasteDumpRewardType type, Map map)
        {
            Func<Map, Thing> provider;
            return Providers.TryGetValue(type, out provider) ? provider(map) : null;
        }

        private static Thing MakeResearchData()
        {
            return BlightTechnologyUtility.CreateResearchData(Rand.RangeInclusive(1, 3));
        }

        private static Thing MakeUndecodedBlueprint(BlightTechnologyTier tier)
        {
            BlightTechnologyGameComponent component = BlightTechnologyGameComponent.Instance;
            List<ThingDef> available = BlightTechnologyUtility.BlueprintNamesFor(tier)
                .Where(name => component == null || !component.IsBlueprintUnlocked(name))
                .Select(DefDatabase<ThingDef>.GetNamedSilentFail)
                .Where(def => def != null).ToList();
            return available.Count == 0 ? null : ThingMaker.MakeThing(available.RandomElement());
        }
    }

    [HarmonyPatch(typeof(ResearchProjectDef), "get_CanStartNow")]
    public static class BlightTechnologyCanStartPatch
    {
        public static void Postfix(ResearchProjectDef __instance, ref bool __result)
        {
            if (__result) __result = BlightTechnologyUtility.IsProjectUnlocked(__instance);
        }
    }

    [HarmonyPatch(typeof(ResearchProjectDef), "CanBeResearchedAt")]
    public static class BlightTechnologyBenchPatch
    {
        public static void Postfix(ResearchProjectDef __instance, ref bool __result)
        {
            if (__result) __result = BlightTechnologyUtility.IsProjectUnlocked(__instance);
        }
    }

    [HarmonyPatch(typeof(ResearchProjectDef), "get_IsHidden")]
    public static class BlightTechnologyHiddenPatch
    {
        public static void Postfix(ResearchProjectDef __instance, ref bool __result)
        {
            if (!__result && __instance?.GetModExtension<BlightTechnologyProjectExtension>() != null)
                __result = !BlightTechnologyUtility.IsProjectUnlocked(__instance);
        }
    }

    [HarmonyPatch(typeof(ResearchManager), "SetCurrentProject")]
    public static class BlightTechnologyPaymentPatch
    {
        public static bool Prefix(ResearchProjectDef proj)
        {
            BlightTechnologyProjectExtension extension = proj?.GetModExtension<BlightTechnologyProjectExtension>();
            if (extension == null) return true;
            if (!BlightTechnologyUtility.IsProjectUnlocked(proj))
            {
                Messages.Message("LiAIChat_BlightResearchBlueprintRequired".Translate(), MessageTypeDefOf.RejectInput, false);
                return false;
            }

            BlightTechnologyGameComponent component = BlightTechnologyGameComponent.Instance;
            if (component == null || component.HasPaidFor(proj)) return true;
            int required = Math.Max(0, extension.researchDataCost);
            if (!BlightTechnologyUtility.HasResearchData(required))
            {
                Messages.Message("LiAIChat_BlightResearchDataInsufficient".Translate(required,
                    BlightTechnologyUtility.CountResearchData()), MessageTypeDefOf.RejectInput, false);
                return false;
            }
            if (!BlightTechnologyUtility.TryConsumeResearchData(required)) return false;
            component.RecordPayment(proj);
            Messages.Message("LiAIChat_BlightResearchDataConsumed".Translate(required, proj.label),
                MessageTypeDefOf.PositiveEvent, false);
            return true;
        }
    }

    [HarmonyPatch]
    public static class BlightBlueprintFloatMenuPatch
    {
        public static System.Reflection.MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(Thing), "GetFloatMenuOptions", new[] { typeof(Pawn) });
        }

        public static void Postfix(Thing __instance, object[] __args, ref IEnumerable<FloatMenuOption> __result)
        {
            Pawn pawn = __args != null && __args.Length > 0 ? __args[0] as Pawn : null;
            if (pawn == null || !BlightTechnologyUtility.IsBlueprint(__instance?.def)) return;
            List<FloatMenuOption> options = __result?.ToList() ?? new List<FloatMenuOption>();
            options.Add(new FloatMenuOption("LiAIChat_DecodeBlightBlueprint".Translate(__instance.LabelCap),
                () => BlightTechnologyUtility.TryDecodeBlueprint(pawn, __instance), MenuOptionPriority.Default));
            __result = options;
        }
    }
}
