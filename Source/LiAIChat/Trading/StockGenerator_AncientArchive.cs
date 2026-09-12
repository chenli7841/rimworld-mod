using System.Collections.Generic;
using RimWorld;
using Verse;
using LiAIChat.Archive;
using RimWorld.Planet;

namespace LiAIChat.Trading
{
    public class StockGenerator_AncientArchive : StockGenerator
    {
        public float chance = 0.10f;

        public override IEnumerable<Thing> GenerateThings(PlanetTile forTile, Faction faction = null)
        {
            if (!Rand.Chance(chance))
            {
                yield break;
            }

            Thing archive =
                AncientArchiveFactory.Create(
                    ArchiveSourceType.Trader,
                    "Acquired from a rare goods trader.");

            yield return archive;
        }

        public override bool HandlesThingDef(ThingDef thingDef)
        {
            return thingDef ==
                ThingDefOfArchive
                    .LiAIChat_AncientEarthArchiveFragment;
        }
    }
}