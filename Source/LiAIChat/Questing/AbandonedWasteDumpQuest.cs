using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse;

namespace LiAIChat.Questing
{
    public class QuestNode_CreateAbandonedWasteDump : QuestNode
    {
        protected override bool TestRunInt(Slate slate)
        {
            PlanetTile tile = slate.Get<PlanetTile>("siteTile");
            return tile.Valid &&
                DefDatabase<ThingDef>.GetNamedSilentFail("Wastepack") != null &&
                DefDatabase<SitePartDef>.GetNamedSilentFail("LiAIChat_AbandonedWasteDump") != null;
        }

        protected override void RunInt()
        {
            PlanetTile tile = QuestGen.slate.Get<PlanetTile>("siteTile");
            SitePartDef sitePart = DefDatabase<SitePartDef>.GetNamed("LiAIChat_AbandonedWasteDump");
            Site site = SiteMaker.MakeSite(sitePart, tile, null, false, (float?)null, null);
            if (site == null)
                return;

            QuestGen.slate.Set("site", site);
        }
    }

    public class SitePartWorker_AbandonedWasteDump : SitePartWorker
    {
    }

    public class GenStep_AbandonedWasteDump : GenStep
    {
        private const int Seed = 48173921;

        public override int SeedPart => Seed;

        public override void Generate(Map map, GenStepParams parms)
        {
            ThingDef wastepackDef = DefDatabase<ThingDef>.GetNamedSilentFail("Wastepack");
            if (map?.pollutionGrid == null || wastepackDef == null)
                return;

            // Biotech's normal Pollution gen step runs at order 890. Raise this
            // quest map to a reliably visible cleanup scale while keeping its
            // contamination inside the vanilla pollution system.
            float pollutionPercent = Rand.Range(0.16f, 0.30f);
            PollutionUtility.PolluteMapToPercent(map, pollutionPercent, Rand.RangeInclusive(5, 9));

            ListSpawnablePollutedCells(map, out var candidates);
            if (candidates.Count == 0)
            {
                Log.Warning("[LiAIChat] Abandoned waste dump generated without usable polluted cells.");
                return;
            }

            int remainingWastepacks = Rand.RangeInclusive(14, 32);
            int spawnedWastepacks = 0;
            while (remainingWastepacks > 0 && candidates.Count > 0)
            {
                int index = Rand.Range(0, candidates.Count);
                IntVec3 cell = candidates[index];
                candidates.RemoveAt(index);

                Thing pack = ThingMaker.MakeThing(wastepackDef);
                pack.stackCount = System.Math.Min(remainingWastepacks, Rand.RangeInclusive(1, 5));
                if (GenPlace.TryPlaceThing(pack, cell, map, ThingPlaceMode.Near))
                {
                    spawnedWastepacks += pack.stackCount;
                    remainingWastepacks -= pack.stackCount;
                }
                else if (!pack.Destroyed)
                {
                    pack.Destroy(DestroyMode.Vanish);
                }
            }

            Log.Message("[LiAIChat] Abandoned waste dump generated: pollutedCells=" +
                map.pollutionGrid.TotalPollution + ", wastepacks=" + spawnedWastepacks + ".");
        }

        private static void ListSpawnablePollutedCells(Map map, out System.Collections.Generic.List<IntVec3> cells)
        {
            cells = new System.Collections.Generic.List<IntVec3>();
            foreach (IntVec3 cell in map.AllCells)
            {
                if (cell.DistanceToSquared(map.Center) < 30 * 30 ||
                    !cell.Standable(map) || cell.Roofed(map) ||
                    !map.pollutionGrid.IsPolluted(cell))
                    continue;

                cells.Add(cell);
            }
        }
    }
}
