using System.Collections.Generic;
using System.Linq;
using LudeonTK;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace LiAIChat.Blightfall
{
    public static class BlightfallArrivalEvent
    {
        private const int MinDistanceFromHome = 8;
        private const int MaxDistanceFromHome = 16;

        public static bool TryTrigger(WorldComponent_Blightfall component, bool manual)
        {
            if (component == null || component.ArrivalEventTriggered ||
                component.CampaignState != BlightfallCampaignState.Dormant)
                return false;

            int now = Find.TickManager?.TicksGame ?? 0;
            if (!manual && now < WorldComponent_Blightfall.ArrivalAfterDays * 60000)
                return false;

            if (Find.WorldGrid == null || Find.WorldObjects == null)
                return false;

            List<PlanetTile> homeTiles = Find.Maps
                .Where(map => map != null && map.IsPlayerHome && map.Tile.Valid)
                .Select(map => map.Tile)
                .Concat(Find.WorldObjects.Settlements
                    .Where(settlement => settlement != null && settlement.Faction == Faction.OfPlayer && settlement.Tile.Valid)
                    .Select(settlement => settlement.Tile))
                .Distinct()
                .ToList();
            if (homeTiles.Count == 0)
                return false;

            PlanetTile center;
            if (!TryFindCenter(homeTiles, out center))
                return false;

            List<PlanetTile> nearbyTiles = FindNearbyTiles(center, Rand.RangeInclusive(5, 10));
            if (!component.StartCampaign(center.tileId, now))
                return false;

            component.SetSourceActivity(100f);
            component.SetSuppression(0f);
            component.SetTilePolluted(center.tileId, true);
            foreach (PlanetTile tile in nearbyTiles)
                component.SetTilePolluted(tile.tileId, true);
            component.EnsureFunctionalNodes();
            BlightfallSiteUtility.EnsureSitesForPollutedTiles(component);

            if (!component.TryMarkArrivalEventTriggered())
                return false;

            Find.LetterStack.ReceiveLetter(
                "LiAIChat_BlightfallArrivalTitle".Translate(),
                "LiAIChat_BlightfallArrivalText".Translate(nearbyTiles.Count),
                LetterDefOf.ThreatBig,
                new LookTargets(center),
                null,
                null);
            return true;
        }

        private static bool TryFindCenter(List<PlanetTile> homeTiles, out PlanetTile center)
        {
            foreach (PlanetTile home in homeTiles)
            {
                if (TileFinder.TryFindTileWithDistance(home, MinDistanceFromHome, MaxDistanceFromHome,
                        out center, IsAvailableLandTile, TileFinderMode.Near, true))
                    return true;
            }

            foreach (PlanetTile home in homeTiles)
            {
                if (TileFinder.TryFindTileWithDistance(home, 5, 22,
                        out center, IsAvailableLandTile, TileFinderMode.Near, true))
                    return true;
            }

            center = PlanetTile.Invalid;
            return false;
        }

        private static bool IsAvailableLandTile(PlanetTile tile)
        {
            return tile.Valid && tile.tileId >= 0 && tile.tileId < Find.WorldGrid.TilesCount &&
                   !Find.WorldGrid[tile].WaterCovered && !Find.WorldObjects.AnyWorldObjectAt(tile);
        }

        private static List<PlanetTile> FindNearbyTiles(PlanetTile center, int targetCount)
        {
            var selected = new List<PlanetTile>();
            var selectedIds = new HashSet<int> { center.tileId };
            var frontier = new List<PlanetTile> { center };

            while (selected.Count < targetCount && frontier.Count > 0)
            {
                int parentIndex = Rand.Range(0, frontier.Count);
                PlanetTile parent = frontier[parentIndex];
                frontier.RemoveAt(parentIndex);

                var neighbors = new List<PlanetTile>();
                Find.WorldGrid.GetTileNeighbors(parent, neighbors);
                foreach (PlanetTile neighbor in neighbors.InRandomOrder())
                {
                    if (!selectedIds.Add(neighbor.tileId) || !IsAvailableLandTile(neighbor))
                        continue;
                    selected.Add(neighbor);
                    frontier.Add(neighbor);
                    if (selected.Count >= targetCount)
                        break;
                }
            }

            return selected;
        }
    }

    public static class BlightfallDebugActions
    {
        [DebugAction("Li AI Chat", "Trigger Blightfall arrival", allowedGameStates = AllowedGameStates.PlayingOnWorld)]
        private static void TriggerArrival()
        {
            if (BlightfallArrivalEvent.TryTrigger(WorldComponent_Blightfall.Current, true))
                Messages.Message("LiAIChat_BlightfallDebugTriggered".Translate(), MessageTypeDefOf.PositiveEvent);
            else
                Messages.Message("LiAIChat_BlightfallDebugUnavailable".Translate(), MessageTypeDefOf.RejectInput);
        }
    }
}
