using System;
using System.Collections.Generic;
using System.Linq;
using LiAIChat.AlliedSettlementSurvival;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace LiAIChat.Blightfall
{
    public enum BlightfallFactionAction
    {
        None,
        Containment,
        Purification,
        MilitaryExpedition,
        Evacuation
    }

    public sealed class BlightfallFactionResponseRecord : IExposable
    {
        public int SettlementId = -1;
        public int LastEvaluationTick = -1;
        public int NextEvaluationTick = -1;
        public BlightfallFactionAction LastAction;
        public bool LastActionSucceeded;
        public bool Evacuated;

        public void ExposeData()
        {
            Scribe_Values.Look(ref SettlementId, "settlementId", -1);
            Scribe_Values.Look(ref LastEvaluationTick, "lastEvaluationTick", -1);
            Scribe_Values.Look(ref NextEvaluationTick, "nextEvaluationTick", -1);
            Scribe_Values.Look(ref LastAction, "lastAction", BlightfallFactionAction.None);
            Scribe_Values.Look(ref LastActionSucceeded, "lastActionSucceeded", false);
            Scribe_Values.Look(ref Evacuated, "evacuated", false);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (SettlementId < -1) SettlementId = -1;
                if (LastEvaluationTick < -1) LastEvaluationTick = -1;
                if (NextEvaluationTick < -1) NextEvaluationTick = -1;
                if (!Enum.IsDefined(typeof(BlightfallFactionAction), LastAction))
                    LastAction = BlightfallFactionAction.None;
            }
        }
    }

    /// <summary>Settles abstract NPC responses to The Blightfall without generating maps or pawns.</summary>
    public sealed class WorldComponent_BlightfallFactionEnvironmentalResponse : WorldComponent
    {
        private const int TicksPerDay = 60000;
        private const int MinimumEvaluationDays = 5;
        private const int MaximumEvaluationDays = 10;
        private const int MaximumThreatDistance = 18;
        private const int SevereThreatDistance = 3;
        private const int MaximumQuarantineTiles = 4;
        private List<BlightfallFactionResponseRecord> records = new List<BlightfallFactionResponseRecord>();

        public WorldComponent_BlightfallFactionEnvironmentalResponse(World world) : base(world) { }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref records, "liBlightfallFactionResponseRecords", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (records == null) records = new List<BlightfallFactionResponseRecord>();
                var seen = new HashSet<int>();
                records.RemoveAll(record => record == null || record.SettlementId < 0 ||
                    !seen.Add(record.SettlementId));
            }
        }

        public override void WorldComponentTick()
        {
            base.WorldComponentTick();
            int now = Find.TickManager?.TicksGame ?? 0;
            if (now % 2500 != 0 || Find.WorldObjects == null)
                return;

            WorldComponent_Blightfall campaign = WorldComponent_Blightfall.Current;
            if (campaign == null || campaign.CampaignState == BlightfallCampaignState.Dormant ||
                campaign.CampaignState == BlightfallCampaignState.Completed || campaign.PollutedTileIds.Count == 0)
                return;

            HashSet<int> liveSettlementIds = new HashSet<int>();
            foreach (Settlement settlement in Find.WorldObjects.Settlements.ToList())
            {
                if (!EligibleSettlement(settlement))
                    continue;
                liveSettlementIds.Add(settlement.ID);
                BlightfallFactionResponseRecord record = GetOrCreateRecord(settlement.ID);
                if (record.Evacuated)
                    continue;
                if (record.NextEvaluationTick < 0)
                    record.NextEvaluationTick = now + RandomIntervalTicks();
                if (now < record.NextEvaluationTick)
                    continue;

                EvaluateSettlement(campaign, settlement, record, now);
                record.LastEvaluationTick = now;
                record.NextEvaluationTick = now + RandomIntervalTicks();
            }

            records.RemoveAll(record => record == null || !liveSettlementIds.Contains(record.SettlementId));
        }

        private static bool EligibleSettlement(Settlement settlement)
        {
            return settlement != null && settlement.Spawned && settlement.Tile.Valid &&
                   settlement.Tile.Layer.IsRootSurface && settlement.Faction != null &&
                   settlement.Faction != Faction.OfPlayer && !settlement.Faction.defeated &&
                   !settlement.Faction.HostileTo(Faction.OfPlayer);
        }

        private BlightfallFactionResponseRecord GetOrCreateRecord(int settlementId)
        {
            BlightfallFactionResponseRecord record = records.FirstOrDefault(item =>
                item != null && item.SettlementId == settlementId);
            if (record != null)
                return record;
            record = new BlightfallFactionResponseRecord { SettlementId = settlementId };
            records.Add(record);
            return record;
        }

        private static int RandomIntervalTicks() =>
            Rand.RangeInclusive(MinimumEvaluationDays, MaximumEvaluationDays) * TicksPerDay;

        private static void EvaluateSettlement(WorldComponent_Blightfall campaign, Settlement settlement,
            BlightfallFactionResponseRecord record, int now)
        {
            if (Find.WorldGrid == null || campaign.PollutedTileIds.Count == 0)
                return;

            int nearestPollution = campaign.PollutedTileIds
                .Where(IsUsableTile)
                .OrderBy(tile => Find.WorldGrid.ApproxDistanceInTiles(settlement.Tile, new PlanetTile(tile)))
                .DefaultIfEmpty(-1).First();
            if (nearestPollution < 0)
                return;

            float distance = Find.WorldGrid.ApproxDistanceInTiles(settlement.Tile, new PlanetTile(nearestPollution));
            if (distance > MaximumThreatDistance)
                return;

            float tech = TechScore(settlement.Faction.def.techLevel);
            float strength = StrengthScore(settlement);
            float proximity = 1f - Math.Min(1f, distance / (float)MaximumThreatDistance);
            float successChance = Clamp01(0.24f + tech * 0.28f + strength * 0.30f + proximity * 0.18f);
            List<BlightfallPollutionNodeState> vulnerableNodes = campaign.PollutionNodes
                .Where(node => node != null && node.Status == BlightfallPollutionNodeStatus.Active &&
                    node.Type != BlightfallPollutionNodeType.ResonanceSpire &&
                    node.TileId != campaign.SourceCenterTileId && IsUsableTile(node.TileId) &&
                    Find.WorldGrid.ApproxDistanceInTiles(settlement.Tile, new PlanetTile(node.TileId)) <= MaximumThreatDistance)
                .OrderBy(node => Find.WorldGrid.ApproxDistanceInTiles(settlement.Tile, new PlanetTile(node.TileId)))
                .ToList();
            List<int> frontline = campaign.PollutedTileIds
                .Where(tile => tile != campaign.SourceCenterTileId && campaign.CanPurifyTile(tile) &&
                    CanNpcPurifyTile(campaign, tile))
                .OrderBy(tile => Find.WorldGrid.ApproxDistanceInTiles(settlement.Tile, new PlanetTile(tile)))
                .ToList();

            BlightfallFactionAction action;
            if (distance <= SevereThreatDistance && (campaign.SourceActivity >= 60f || distance <= 1) &&
                Rand.Value < 0.28f + tech * 0.28f + strength * 0.18f)
                action = BlightfallFactionAction.Evacuation;
            else if (vulnerableNodes.Count > 0 &&
                     Rand.Value < 0.08f + tech * 0.40f + strength * 0.20f)
                action = BlightfallFactionAction.MilitaryExpedition;
            else if (frontline.Count > 0 &&
                     Rand.Value < 0.18f + tech * 0.34f + strength * 0.18f)
                action = BlightfallFactionAction.Purification;
            else
                action = BlightfallFactionAction.Containment;

            bool success = Rand.Value < successChance;
            switch (action)
            {
                case BlightfallFactionAction.Evacuation:
                    int oldTile = settlement.Tile.tileId;
                    bool evacuated = success && TryEvacuate(settlement, campaign, out oldTile);
                    success = success && evacuated;
                    if (success)
                    {
                        record.Evacuated = true;
                        campaign.SetTileProtected(oldTile);
                    }
                    else campaign.AdjustActivity(2f);
                    break;

                case BlightfallFactionAction.MilitaryExpedition:
                    BlightfallPollutionNodeState node = vulnerableNodes.FirstOrDefault();
                    if (success && node != null)
                    {
                        node.Status = BlightfallPollutionNodeStatus.Destroyed;
                        node.Activity = 0f;
                        node.Suppression = 0f;
                        node.TargetThingId = -1;
                        node.LastUpdatedTick = now;
                        campaign.AdjustActivity(-5f);
                        campaign.AdjustSuppression(3f);
                    }
                    else
                    {
                        success = false;
                        campaign.AdjustActivity(3f);
                    }
                    break;

                case BlightfallFactionAction.Purification:
                    int tileToClean = frontline.Count > 0 ? frontline[0] : -1;
                    if (success && tileToClean >= 0 && campaign.PurifyTile(tileToClean))
                    {
                        RemovePurifiedSite(campaign, tileToClean);
                        campaign.AdjustSuppression(2f);
                    }
                    else
                    {
                        success = false;
                        campaign.AdjustActivity(2f);
                    }
                    break;

                default:
                    int protectedCount = success ? EstablishContainment(campaign, settlement) : 0;
                    success = protectedCount > 0;
                    if (success) campaign.AdjustSuppression(Math.Min(3f, protectedCount));
                    else campaign.AdjustActivity(1.5f);
                    break;
            }

            record.LastAction = action;
            record.LastActionSucceeded = success;
            Log.Message("[LiAIChat Blightfall] " + settlement.Faction.Name + " settlement " + settlement.Label +
                " resolved " + action + (success ? " successfully." : " unsuccessfully."));
            NotifyPlayer(settlement, action, success);
        }

        private static float StrengthScore(Settlement settlement)
        {
            AlliedSettlementState tracked = AlliedSettlementWorldComponent.Current?.Get(settlement);
            if (tracked != null)
                return BlightfallCampaignUtility.ClampPercent(tracked.strength) / 100f;

            int sameFactionSettlements = Find.WorldObjects.Settlements.Count(item =>
                item != null && item.Faction == settlement.Faction && item.Spawned);
            return Clamp01(0.20f + TechScore(settlement.Faction.def.techLevel) * 0.45f +
                           Math.Min(0.35f, Math.Max(0, sameFactionSettlements - 1) * 0.07f));
        }

        private static float TechScore(TechLevel techLevel)
        {
            int level = (int)techLevel;
            return Clamp01((level - (int)TechLevel.Neolithic + 1) / 6f);
        }

        private static int EstablishContainment(WorldComponent_Blightfall campaign, Settlement settlement)
        {
            var candidates = new HashSet<int>();
            var firstRing = new List<PlanetTile>();
            Find.WorldGrid.GetTileNeighbors(settlement.Tile, firstRing);
            foreach (PlanetTile first in firstRing)
            {
                candidates.Add(first.tileId);
                var secondRing = new List<PlanetTile>();
                Find.WorldGrid.GetTileNeighbors(first, secondRing);
                foreach (PlanetTile second in secondRing)
                    candidates.Add(second.tileId);
            }

            int[] tiles = candidates.Where(IsUsableTile)
                .Where(tile => !campaign.IsTilePolluted(tile) && !campaign.IsTilePurified(tile))
                .Where(tile => !Find.WorldObjects.AnyWorldObjectAt(new PlanetTile(tile)))
                .OrderBy(tile => campaign.PollutedTileIds.Min(polluted =>
                    Find.WorldGrid.ApproxDistanceInTiles(new PlanetTile(tile), new PlanetTile(polluted))))
                .Take(MaximumQuarantineTiles)
                .ToArray();
            int protectedCount = 0;
            foreach (int tile in tiles)
                if (campaign.SetTileProtected(tile)) protectedCount++;
            return protectedCount;
        }

        private static bool TryEvacuate(Settlement settlement, WorldComponent_Blightfall campaign, out int oldTile)
        {
            oldTile = settlement.Tile.tileId;
            if (settlement.HasMap || Find.Maps.Any(map => map.Parent == settlement) || Find.WorldGrid == null ||
                Find.WorldObjects == null)
                return false;

            int destination = -1;
            int currentTile = settlement.Tile.tileId;
            for (int tileId = 0; tileId < Find.WorldGrid.TilesCount; tileId++)
            {
                if (!IsUsableTile(tileId) || campaign.IsTilePolluted(tileId) ||
                    Find.WorldObjects.AnyWorldObjectAt(new PlanetTile(tileId)))
                    continue;
                float distanceFromThreat = campaign.PollutedTileIds.Min(polluted =>
                    Find.WorldGrid.ApproxDistanceInTiles(new PlanetTile(tileId), new PlanetTile(polluted)));
                float distanceFromSettlement = Find.WorldGrid.ApproxDistanceInTiles(
                    new PlanetTile(currentTile), new PlanetTile(tileId));
                if (distanceFromThreat < MaximumThreatDistance + 8 || distanceFromSettlement < 2 || distanceFromSettlement > 20)
                    continue;
                if (destination < 0 || distanceFromSettlement < Find.WorldGrid.ApproxDistanceInTiles(
                        new PlanetTile(currentTile), new PlanetTile(destination)))
                    destination = tileId;
            }

            if (destination < 0)
                return false;
            settlement.Tile = new PlanetTile(destination);
            return true;
        }

        private static bool CanNpcPurifyTile(WorldComponent_Blightfall campaign, int tileId)
        {
            BlightfallSiteState state = campaign.SiteStates.FirstOrDefault(item => item != null &&
                item.TileId == tileId && item.Status != BlightfallSiteStatus.Completed &&
                item.Status != BlightfallSiteStatus.Removed);
            if (state == null || Find.WorldObjects?.Sites == null)
                return true;

            Site site = Find.WorldObjects.Sites.FirstOrDefault(item => item != null && item.ID == state.SiteId);
            if (site == null)
                return true;
            if (site.HasMap || Find.Maps.Any(map => map.Parent == site))
                return false;

            return Find.QuestManager == null || !Find.QuestManager.QuestsListForReading.Any(quest =>
                quest != null && quest.State == QuestState.Ongoing &&
                quest.PartsListForReading.Any(part => part.QuestLookTargets != null &&
                    part.QuestLookTargets.Any(target => target.HasWorldObject && target.WorldObject == site)));
        }

        private static void RemovePurifiedSite(WorldComponent_Blightfall campaign, int tileId)
        {
            BlightfallSiteState state = campaign.SiteStates.FirstOrDefault(item => item != null &&
                item.TileId == tileId && item.Status != BlightfallSiteStatus.Completed &&
                item.Status != BlightfallSiteStatus.Removed);
            Site site = state == null ? null : Find.WorldObjects?.Sites.FirstOrDefault(item =>
                item != null && item.ID == state.SiteId);
            if (site != null && !site.Destroyed)
                site.Destroy();
        }

        private static bool IsUsableTile(int tileId)
        {
            if (Find.WorldGrid == null || tileId < 0 || tileId >= Find.WorldGrid.TilesCount)
                return false;
            PlanetTile tile = new PlanetTile(tileId);
            return tile.Layer.IsRootSurface && Find.WorldGrid[tile].OnSurface && !Find.WorldGrid[tile].WaterCovered;
        }

        private static void NotifyPlayer(Settlement settlement, BlightfallFactionAction action, bool success)
        {
            string verb;
            switch (action)
            {
                case BlightfallFactionAction.Evacuation: verb = "Blightfall_ResponseEvacuation"; break;
                case BlightfallFactionAction.MilitaryExpedition: verb = "Blightfall_ResponseMilitary"; break;
                case BlightfallFactionAction.Purification: verb = "Blightfall_ResponsePurification"; break;
                default: verb = "Blightfall_ResponseContainment"; break;
            }
            string result = success ? "Blightfall_ResponseSuccess" : "Blightfall_ResponseFailure";
            Find.LetterStack?.ReceiveLetter(
                "Blightfall_ResponseLetterLabel".Translate(settlement.Faction.Name),
                "Blightfall_ResponseLetterText".Translate(settlement.Faction.Name, settlement.LabelCap,
                    verb.Translate(), result.Translate()),
                LetterDefOf.NeutralEvent,
                settlement);
        }

        private static float Clamp01(float value) => Math.Max(0f, Math.Min(1f, value));
    }
}
