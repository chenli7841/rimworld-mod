using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI.Group;

namespace LiAIChat.AlliedSettlementSurvival
{
    [HarmonyPatch]
    public static class AlliedCaravanSpawnPatches
    {
        public static MethodBase TargetMethod() => AccessTools.Method(
            typeof(IncidentWorker_TraderCaravanArrival), "SpawnPawns");

        public static void Postfix(IncidentWorker_NeutralGroup __instance,
            IncidentParms parms, List<Pawn> __result)
        {
            if (!(__instance is IncidentWorker_TraderCaravanArrival) || parms == null || __result == null || parms.faction == null ||
                parms.target == null || !AlliedSettlementSurvivalMod.Current.systemEnabled ||
                parms.faction == Faction.OfPlayer || parms.faction.def == null ||
                parms.faction.defeated ||
                parms.faction.RelationKindWith(Faction.OfPlayer) != FactionRelationKind.Ally ||
                parms.traderKind == null || parms.traderKind.orbital ||
                Find.World == null || Find.WorldGrid == null)
                return;

            try
            {
                Reinforce(parms, __result);
            }
            catch (Exception error)
            {
                Log.Error("[LiAIChat] Could not reinforce an allied trader caravan safely: " + error);
            }
        }

        private static void Reinforce(IncidentParms parms, List<Pawn> result)
        {
            Pawn spawnedMember = result.FirstOrDefault(pawn => pawn != null && pawn.Spawned && pawn.Map != null);
            if (spawnedMember == null) return;
            Map map = spawnedMember.Map;

            AlliedSettlementWorldComponent component = AlliedSettlementWorldComponent.Current;
            if (component == null) return;
            component.Refresh();
            PlanetTile targetTile = parms.target.Tile;
            if (!targetTile.Valid || !targetTile.Layer.IsRootSurface) return;

            AlliedSettlementState sourceState = component.States
                .Where(state => state != null && state.settlement != null && state.active &&
                    !state.pendingCollapse && state.strength > 0f &&
                    state.settlement.Faction == parms.faction && component.Eligible(state.settlement) &&
                    state.settlement.Tile.Valid && state.settlement.Tile.Layer == targetTile.Layer)
                .OrderBy(state => Find.WorldGrid.ApproxDistanceInTiles(targetTile, state.settlement.Tile))
                .FirstOrDefault();
            if (sourceState == null) return;

            List<PawnGenOption> options = parms.faction.def.pawnGroupMakers
                .Where(maker => maker != null && maker.kindDef == PawnGroupKindDefOf.Trader && maker.guards != null)
                .SelectMany(maker => maker.guards)
                .Where(option => option != null && option.kind != null && option.kind.RaceProps.Humanlike)
                .ToList();
            if (options.Count == 0) return;

            List<PawnKindDef> guardKinds = options.Select(option => option.kind).Distinct().ToList();
            HashSet<PawnKindDef> guardKindSet = new HashSet<PawnKindDef>(guardKinds);
            List<Pawn> guards = result.Where(pawn => IsGuard(pawn, guardKindSet)).ToList();
            bool tribal = (int)parms.faction.def.techLevel <= (int)TechLevel.Neolithic;
            int tribalTarget = tribal && sourceState.strength >= 90f ? Rand.RangeInclusive(17, 22) : 0;
            bool topTier = AlliedCaravanReinforcementPolicy.UsesTopTierCivilizedReinforcements(
                sourceState.strength, tribal);
            int extraCount = topTier
                ? AlliedCaravanReinforcementPolicy.TopTierReinforcementCount(PlayerColonistCount())
                : AlliedCaravanReinforcementPolicy.AdditionalGuardCount(
                    sourceState.strength, tribal, guards.Count, tribalTarget);

            for (int i = 0; i < extraCount; i++)
            {
                PawnKindDef kind = ChooseGuardKind(options, sourceState.strength, tribal);
                if (kind == null) break;
                Pawn guard = PawnGenerator.GeneratePawn(kind, parms.faction, targetTile);
                if (guard == null) continue;
                IntVec3 spawnCell = CellFinder.RandomClosewalkCellNear(parms.spawnCenter, map, 5);
                GenSpawn.Spawn(guard, spawnCell, map);
                if (!guard.Spawned)
                {
                    guard.Destroy(DestroyMode.Vanish);
                    continue;
                }
                result.Add(guard);
                guards.Add(guard);
            }

            if (guards.Count == 0) return;
            if (topTier)
                AlliedEliteReinforcementLoadout.EquipGroup(guards);

            Pawn leader = guards.Where(pawn => pawn.kindDef.factionLeader)
                .OrderByDescending(pawn => pawn.kindDef.combatPower).FirstOrDefault() ??
                guards.OrderByDescending(pawn => pawn.kindDef.combatPower).First();
            int eliteCount = AlliedCaravanReinforcementPolicy.EliteGuardCount(sourceState.strength);
            List<Pawn> elites = guards.Where(pawn => pawn != leader)
                .OrderByDescending(pawn => pawn.kindDef.combatPower)
                .Take(eliteCount).ToList();
            component.RegisterCaravanGuards(sourceState.settlement, guards, leader, elites);
        }

        private static bool IsGuard(Pawn pawn, HashSet<PawnKindDef> guardKinds)
        {
            return pawn != null && !pawn.Dead && pawn.kindDef != null && pawn.RaceProps.Humanlike &&
                guardKinds.Contains(pawn.kindDef);
        }

        private static int PlayerColonistCount() => PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive
            .Count(pawn => pawn != null && pawn.IsColonistPlayerControlled);

        private static PawnKindDef ChooseGuardKind(List<PawnGenOption> options, float strength, bool tribal)
        {
            if (tribal || strength < 50f)
            {
                float totalWeight = options.Sum(option => option.selectionWeight);
                if (totalWeight <= 0f) return options.RandomElement().kind;
                float roll = Rand.Value * totalWeight;
                foreach (PawnGenOption option in options)
                {
                    roll -= option.selectionWeight;
                    if (roll <= 0f) return option.kind;
                }
                return options[options.Count - 1].kind;
            }

            List<PawnKindDef> ordered = options.Select(option => option.kind).Distinct()
                .OrderBy(kind => kind.combatPower).ToList();
            if (ordered.Count == 0) return null;
            float percentile = strength < 75f ? 0.5f : strength < 90f ? 0.75f : 1f;
            int index = System.Math.Min(ordered.Count - 1,
                (int)System.Math.Floor((ordered.Count - 1) * percentile));
            return ordered[index];
        }
    }

    // Quick military aid requested over the comms console uses the vanilla
    // friendly-raid worker, with a dedicated IncidentParms flag. Add the
    // reinforcements after vanilla has generated the group, then send them
    // through the same arrival mode before the incident creates its Lord.
    [HarmonyPatch(typeof(IncidentWorker_Raid), "TryGenerateRaidInfo")]
    public static class AlliedMilitaryAidReinforcementPatch
    {
        public static void Postfix(IncidentWorker_Raid __instance, IncidentParms parms,
            ref List<Pawn> pawns, bool debugTest, ref bool __result)
        {
            Map map = parms?.target as Map;
            if (!__result || !(__instance is IncidentWorker_RaidFriendly) || debugTest ||
                pawns == null || parms == null || !parms.raidArrivalModeForQuickMilitaryAid ||
                parms.faction == null || parms.faction == Faction.OfPlayer ||
                parms.faction.def == null || parms.faction.defeated ||
                parms.faction.RelationKindWith(Faction.OfPlayer) != FactionRelationKind.Ally ||
                map == null || !map.IsPlayerHome ||
                parms.raidArrivalMode == null || Find.World == null || Find.WorldGrid == null ||
                !AlliedSettlementSurvivalMod.Current.systemEnabled)
                return;

            try
            {
                Reinforce(parms, map, ref pawns);
            }
            catch (Exception error)
            {
                Log.Error("[LiAIChat] Could not reinforce requested allied military aid safely: " + error);
            }
        }

        private static void Reinforce(IncidentParms parms, Map map, ref List<Pawn> pawns)
        {
            AlliedSettlementWorldComponent component = AlliedSettlementWorldComponent.Current;
            if (component == null) return;
            component.Refresh();

            PlanetTile targetTile = map.Tile;
            if (!targetTile.Valid || !targetTile.Layer.IsRootSurface) return;

            AlliedSettlementState sourceState = component.States
                .Where(state => state != null && state.settlement != null && state.active &&
                    !state.pendingCollapse && state.strength > 0f &&
                    state.settlement.Faction == parms.faction && component.Eligible(state.settlement) &&
                    state.settlement.Tile.Valid && state.settlement.Tile.Layer == targetTile.Layer)
                .OrderBy(state => Find.WorldGrid.ApproxDistanceInTiles(targetTile, state.settlement.Tile))
                .FirstOrDefault();
            if (sourceState == null) return;

            PawnGroupKindDef groupKind = parms.pawnGroupKind ?? PawnGroupKindDefOf.Combat;
            List<PawnGenOption> options = parms.faction.def.pawnGroupMakers
                .Where(maker => maker != null && maker.kindDef == groupKind && maker.options != null)
                .SelectMany(maker => maker.options)
                .Where(option => option != null && option.kind != null && option.kind.RaceProps.Humanlike)
                .ToList();
            if (options.Count == 0) return;

            HashSet<PawnKindDef> guardKinds = new HashSet<PawnKindDef>(options.Select(option => option.kind));
            int currentCount = pawns.Count(pawn => pawn != null && !pawn.Dead && pawn.kindDef != null &&
                pawn.RaceProps.Humanlike && guardKinds.Contains(pawn.kindDef));
            bool tribal = (int)parms.faction.def.techLevel <= (int)TechLevel.Neolithic;
            int tribalTarget = tribal && sourceState.strength >= 90f
                ? Rand.RangeInclusive(17, 22)
                : 0;
            bool topTier = AlliedCaravanReinforcementPolicy.UsesTopTierCivilizedReinforcements(
                sourceState.strength, tribal);
            int extraCount = topTier
                ? AlliedCaravanReinforcementPolicy.TopTierReinforcementCount(PlayerColonistCount())
                : AlliedCaravanReinforcementPolicy.AdditionalGuardCount(
                    sourceState.strength, tribal, currentCount, tribalTarget);

            List<Pawn> reinforcements = new List<Pawn>();
            for (int i = 0; i < extraCount; i++)
            {
                PawnKindDef kind = ChooseCombatKind(options, sourceState.strength, tribal);
                if (kind == null) break;
                Pawn pawn = PawnGenerator.GeneratePawn(kind, parms.faction, targetTile);
                if (pawn != null) reinforcements.Add(pawn);
            }
            if (reinforcements.Count > 0)
            {
                parms.raidArrivalMode.Worker.Arrive(reinforcements, parms);
                pawns.AddRange(reinforcements);
                Log.Message("[LiAIChat] Reinforced requested military aid from " +
                    sourceState.settlement.Label + " (strength " + sourceState.strength.ToString("0") +
                    ") with " + reinforcements.Count + " additional fighters.");
            }

            List<Pawn> fighters = pawns.Where(pawn => pawn != null && !pawn.Dead &&
                pawn.kindDef != null && pawn.RaceProps.Humanlike && guardKinds.Contains(pawn.kindDef)).ToList();
            if (fighters.Count == 0) return;
            if (topTier)
                AlliedEliteReinforcementLoadout.EquipGroup(fighters);

            Pawn leader = fighters.Where(pawn => pawn.kindDef.factionLeader)
                .OrderByDescending(pawn => pawn.kindDef.combatPower).FirstOrDefault() ??
                fighters.OrderByDescending(pawn => pawn.kindDef.combatPower).First();
            int eliteCount = AlliedCaravanReinforcementPolicy.EliteGuardCount(sourceState.strength);
            List<Pawn> elites = fighters.Where(pawn => pawn != leader)
                .OrderByDescending(pawn => pawn.kindDef.combatPower)
                .Take(eliteCount).ToList();
            component.RegisterMilitaryAidGuards(sourceState.settlement, fighters, leader, elites);
        }

        private static int PlayerColonistCount() => PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive
            .Count(pawn => pawn != null && pawn.IsColonistPlayerControlled);

        private static PawnKindDef ChooseCombatKind(List<PawnGenOption> options, float strength, bool tribal)
        {
            if (tribal || strength < 50f)
            {
                float totalWeight = options.Sum(option => option.selectionWeight);
                if (totalWeight <= 0f) return options.RandomElement().kind;
                float roll = Rand.Value * totalWeight;
                foreach (PawnGenOption option in options)
                {
                    roll -= option.selectionWeight;
                    if (roll <= 0f) return option.kind;
                }
                return options[options.Count - 1].kind;
            }

            List<PawnKindDef> ordered = options.Select(option => option.kind).Distinct()
                .OrderBy(kind => kind.combatPower).ToList();
            if (ordered.Count == 0) return null;
            float percentile = strength < 75f ? 0.5f : strength < 90f ? 0.75f : 1f;
            int index = System.Math.Min(ordered.Count - 1,
                (int)System.Math.Floor((ordered.Count - 1) * percentile));
            return ordered[index];
        }
    }

}
