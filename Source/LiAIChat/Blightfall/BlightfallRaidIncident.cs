using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace LiAIChat.Blightfall
{
    /// <summary>Schedules Blightfall raids independently of vanilla raid incidents.</summary>
    public sealed class IncidentWorker_BlightfallRaid : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            Map map = parms?.target as Map;
            WorldComponent_Blightfall campaign = WorldComponent_Blightfall.Current;
            return map != null && map.IsPlayerHome && map.mapPawns.FreeColonists.Any() &&
                   campaign != null && campaign.CampaignState == BlightfallCampaignState.Active &&
                   campaign.SourceActivity > 0f && Find.FactionManager.FirstFactionOfDef(
                       DefDatabase<FactionDef>.GetNamedSilentFail("Insect")) != null;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = parms.target as Map;
            WorldComponent_Blightfall campaign = WorldComponent_Blightfall.Current;
            Faction faction = Find.FactionManager.FirstFactionOfDef(
                DefDatabase<FactionDef>.GetNamed("Insect"));
            if (map == null || campaign == null || faction == null)
                return false;

            List<Pawn> attackers = GenerateAttackers(map, faction, campaign);
            if (attackers.Count == 0)
                return false;

            IntVec3 root;
            if (!CellFinder.TryFindRandomEdgeCellWith(cell => cell.Standable(map), map, 0f, out root))
                root = CellFinder.RandomEdgeCell(map);
            foreach (Pawn pawn in attackers)
            {
                IntVec3 cell = CellFinder.RandomSpawnCellForPawnNear(root, map, 12);
                GenSpawn.Spawn(pawn, cell, map);
            }
            LordMaker.MakeNewLord(faction,
                new LordJob_AssaultColony(faction, false, false, false, false, false, false, false), map, attackers);

            Find.LetterStack.ReceiveLetter("LiAIChat_BlightfallRaidLetterTitle".Translate(),
                "LiAIChat_BlightfallRaidLetterText".Translate(), LetterDefOf.ThreatBig, map.Parent);
            return true;
        }

        private static List<Pawn> GenerateAttackers(Map map, Faction faction, WorldComponent_Blightfall campaign)
        {
            int colonists = map.mapPawns.FreeColonists.Count;
            if (colonists <= 0)
                return new List<Pawn>();

            float distance = Find.WorldGrid.ApproxDistanceInTiles(map.Tile,
                new PlanetTile(campaign.SourceCenterTileId));
            float proximity = Mathf.Clamp(1f - distance / 250f, 0.25f, 1f);
            float nestFactor = 1f + Mathf.Min(campaign.ActiveBroodNestCount, 5) * 0.12f;
            float activity = campaign.SourceActivity / 100f;
            float colonyThreat = StorytellerUtility.DefaultThreatPointsNow(map);
            float points = Mathf.Clamp(colonyThreat * (0.16f + activity * 0.25f) * nestFactor, 80f, 900f);
            // Distance changes pressure gradually; colonist count remains the main size limiter.
            points *= Mathf.Lerp(0.75f, 1f, proximity);

            List<PawnKindDef> kinds = new List<PawnKindDef>
            {
                DefDatabase<PawnKindDef>.GetNamedSilentFail("LiAIChat_BlightCrawler"),
                DefDatabase<PawnKindDef>.GetNamedSilentFail("LiAIChat_SporeSpitter"),
                DefDatabase<PawnKindDef>.GetNamedSilentFail("LiAIChat_BlightBehemoth")
            };
            kinds.RemoveAll(kind => kind == null);
            if (kinds.Count == 0)
                return new List<Pawn>();

            List<Pawn> result = new List<Pawn>();
            int maxUnits = Mathf.Clamp(3 + colonists, 4, 18);
            while (result.Count < maxUnits)
            {
                PawnKindDef kind = ChooseKind(kinds, result.Count, campaign.ActiveBroodNestCount, points);
                if (kind == null)
                    break;
                Pawn pawn = PawnGenerator.GeneratePawn(kind, faction);
                if (pawn == null)
                    break;
                result.Add(pawn);
                points -= kind.combatPower;
            }
            return result;
        }

        private static PawnKindDef ChooseKind(List<PawnKindDef> kinds, int currentCount, int activeNests, float remainingPoints)
        {
            // The Behemoth is uncommon and never appears in the first four units.
            List<PawnKindDef> affordable = kinds.Where(kind => kind.combatPower <= remainingPoints).ToList();
            if (affordable.Count == 0)
                return null;
            float roll = Rand.Value;
            string desired = currentCount >= 4 && roll < Mathf.Min(0.35f, 0.08f + activeNests * 0.04f)
                ? "LiAIChat_BlightBehemoth"
                : roll < 0.42f ? "LiAIChat_SporeSpitter" : "LiAIChat_BlightCrawler";
            return affordable.FirstOrDefault(kind => kind.defName == desired) ?? affordable.RandomElement();
        }
    }

    public static class BlightfallRaidIncident
    {
        private const int TicksPerDay = 60000;

        public static void TryLaunch(WorldComponent_Blightfall campaign, int now)
        {
            if (campaign == null || campaign.CampaignState != BlightfallCampaignState.Active)
                return;
            if (Find.WorldGrid == null || campaign.SourceCenterTileId < 0 ||
                campaign.SourceCenterTileId >= Find.WorldGrid.TilesCount)
            {
                campaign.ScheduleNextRaid(now + TicksPerDay);
                return;
            }

            List<Map> candidates = Find.Maps.Where(map => map != null && map.IsPlayerHome &&
                map.mapPawns.FreeColonists.Any()).OrderBy(map => Find.WorldGrid.ApproxDistanceInTiles(
                    map.Tile, new PlanetTile(campaign.SourceCenterTileId))).ToList();
            if (candidates.Count == 0)
            {
                campaign.ScheduleNextRaid(now + TicksPerDay);
                return;
            }

            Map target = candidates[0];
            float distance = Find.WorldGrid.ApproxDistanceInTiles(target.Tile,
                new PlanetTile(campaign.SourceCenterTileId));
            float proximity = Mathf.Clamp(1f - distance / 250f, 0.25f, 1f);
            float activityPressure = campaign.SourceActivity / 100f;
            float nestPressure = Mathf.Min(campaign.ActiveBroodNestCount, 5) * 0.12f;
            float chance = Mathf.Clamp01(0.12f + activityPressure * 0.62f + nestPressure + proximity * 0.18f - campaign.Suppression / 160f);

            if (Rand.Value > chance)
            {
                campaign.ScheduleNextRaid(now + TicksPerDay);
                return;
            }

            IncidentDef incident = DefDatabase<IncidentDef>.GetNamedSilentFail("LiAIChat_BlightfallRaid");
            if (incident == null)
            {
                Log.Error("[LiAIChat Blightfall] Raid incident definition is missing.");
                campaign.ScheduleNextRaid(now + TicksPerDay);
                return;
            }

            IncidentParms parms = new IncidentParms
            {
                target = target,
                points = StorytellerUtility.DefaultThreatPointsNow(target),
                faction = Find.FactionManager.FirstFactionOfDef(DefDatabase<FactionDef>.GetNamed("Insect")),
                raidArrivalMode = PawnsArrivalModeDefOf.EdgeWalkIn
            };
            bool succeeded = incident.Worker.TryExecute(parms);
            campaign.ScheduleNextRaid(succeeded ? now : now + TicksPerDay);
        }
    }
}
