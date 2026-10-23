using System;
using System.Linq;
using LiAIChat.AlliedSettlementSurvival;
using LiAIChat.Questing;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace LiAIChat.Blightfall
{
    public static class BlightfallSettlementPressure
    {
        private const int PressureRadius = 18;

        public static float Exposure(Settlement settlement)
        {
            WorldComponent_Blightfall campaign = WorldComponent_Blightfall.Current;
            if (settlement == null || !settlement.Tile.Valid || Find.WorldGrid == null || campaign == null ||
                campaign.CampaignState == BlightfallCampaignState.Dormant ||
                campaign.CampaignState == BlightfallCampaignState.Completed || campaign.PollutedTileIds.Count == 0)
                return 0f;

            float distance = campaign.PollutedTileIds
                .Where(tileId => tileId >= 0 && tileId < Find.WorldGrid.TilesCount)
                .Select(tileId => Find.WorldGrid.ApproxDistanceInTiles(settlement.Tile, new PlanetTile(tileId)))
                .DefaultIfEmpty(float.MaxValue)
                .Min();
            if (distance >= PressureRadius)
                return 0f;

            float proximity = 1f - distance / PressureRadius;
            float activity = 0.25f + campaign.SourceActivity / 100f * 0.75f;
            float suppression = 1f - campaign.Suppression / 125f;
            float nestPressure = 1f + Math.Min(0.35f, campaign.ActiveBroodNestCount * 0.08f);
            float containedFactor = campaign.CampaignState == BlightfallCampaignState.Contained ? 0.55f : 1f;
            return Math.Max(0f, Math.Min(1.25f, proximity * activity * suppression * nestPressure * containedFactor));
        }

        public static void ApplyStrengthPressure(AlliedSettlementState state, int elapsedTicks)
        {
            if (state?.settlement == null || elapsedTicks <= 0)
                return;
            float exposure = Exposure(state.settlement);
            if (exposure <= 0f)
                return;

            const float MaximumLossPerDay = 0.8f;
            float loss = exposure * MaximumLossPerDay * elapsedTicks / SettlementStrengthPolicy.TicksPerDay;
            state.strength = SettlementStrengthPolicy.Clamp(state.strength - loss);
        }

        public static void ResolvePlayerAid(Settlement settlement, AlliedSettlementAidType aidType)
        {
            AlliedSettlementState state = AlliedSettlementWorldComponent.Current?.Get(settlement);
            WorldComponent_Blightfall campaign = WorldComponent_Blightfall.Current;
            if (state == null)
                return;

            state.strength = SettlementStrengthPolicy.Clamp(state.strength + 8f);
            if (campaign == null)
                return;

            if (aidType == AlliedSettlementAidType.Purification)
            {
                campaign.AdjustSuppression(5f);
                int tileToClean = campaign.PollutedTileIds
                    .Where(tileId => tileId != campaign.SourceCenterTileId && campaign.CanPurifyTile(tileId))
                    .Where(tileId => Find.WorldGrid.ApproxDistanceInTiles(
                        settlement.Tile, new PlanetTile(tileId)) <= 6f)
                    .OrderBy(tileId => Find.WorldGrid.ApproxDistanceInTiles(
                        settlement.Tile, new PlanetTile(tileId)))
                    .DefaultIfEmpty(-1).First();
                if (tileToClean >= 0 && campaign.PurifyTile(tileToClean))
                    RemoveSiteForPurifiedTile(campaign, tileToClean);
            }
            else
            {
                campaign.AdjustSuppression(2f);
            }
        }

        public static void ResolveMilitaryAid(Settlement settlement)
        {
            AlliedSettlementState state = AlliedSettlementWorldComponent.Current?.Get(settlement);
            if (state != null)
                state.strength = SettlementStrengthPolicy.Clamp(state.strength + 12f);

            WorldComponent_Blightfall campaign = WorldComponent_Blightfall.Current;
            campaign?.AdjustActivity(-5f);
            campaign?.AdjustSuppression(4f);
        }

        private static void RemoveSiteForPurifiedTile(WorldComponent_Blightfall campaign, int tileId)
        {
            BlightfallSiteState state = campaign.SiteStates.FirstOrDefault(item => item != null &&
                item.TileId == tileId && item.Status != BlightfallSiteStatus.Completed &&
                item.Status != BlightfallSiteStatus.Removed);
            Site site = state == null ? null : Find.WorldObjects?.Sites.FirstOrDefault(item =>
                item != null && item.ID == state.SiteId);
            if (site == null || site.Destroyed || site.HasMap || Find.Maps.Any(map => map.Parent == site))
                return;
            bool questUsesSite = Find.QuestManager != null && Find.QuestManager.QuestsListForReading.Any(quest =>
                quest != null && quest.State == QuestState.Ongoing &&
                quest.PartsListForReading.Any(part => part.QuestLookTargets != null &&
                    part.QuestLookTargets.Any(target => target.HasWorldObject && target.WorldObject == site)));
            if (!questUsesSite)
                site.Destroy();
        }
    }
}
