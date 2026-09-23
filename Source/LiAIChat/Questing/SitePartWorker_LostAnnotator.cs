using LiAIChat.Archive;
using LiAIChat.State;
using LiAIChat.Models;
using RimWorld;
using System.Linq;
using Verse;

namespace LiAIChat.Questing
{
    public class SitePartWorker_LostAnnotator : SitePartWorker_LostEarthArchive
    {
        public override void PostMapGenerate(Map map)
        {
            base.PostMapGenerate(map);
            TrySpawnScholar(map);
        }

        public static bool TrySpawnScholar(Map map)
        {
            if (map == null || map.mapPawns == null) return false;

            AncientRuinArchiveMapComponent component =
                map.GetComponent<AncientRuinArchiveMapComponent>();
            if (component != null)
            {
                // A shuttle removes a pawn from mapPawns while it is in flight.
                // This flag records the map's single initial generation pass,
                // so that temporary absence never becomes a second scholar.
                if (component.LostAnnotatorSpawnAttempted) return false;
                component.LostAnnotatorSpawnAttempted = true;
            }

            foreach (Pawn existing in map.mapPawns.AllPawnsSpawned)
            {
                PawnAIState existingState = PawnAIStateManager.TryGetExistingState(existing);
                if (existingState != null && existingState.IsLostAnnotator)
                {
                    ArchiveScholarInitializer.ConfigureLostAnnotator(existing);
                    return true;
                }
            }

            Thing terminal = map.listerThings.AllThings.FirstOrDefault(t => t.def == LiAIChatThingDefOf.LiAIChat_AncientArchiveSignalTerminal);
            IntVec3 archiveCell = terminal != null ? terminal.Position : map.Center;
            IntVec3 cell = CellFinderLoose.RandomCellWith(c => c.Standable(map) && c.GetFirstPawn(map) == null && c.DistanceToSquared(archiveCell) > 900f, map);
            if (!cell.IsValid) return false;
            Pawn scholar = PawnGenerator.GeneratePawn(PawnKindDefOf.SpaceRefugee, null);
            if (scholar == null) return false;
            Thing_AncientEarthArchiveFragment archive = ArchiveScholarInitializer.Initialize(scholar);
            PawnAIState state = PawnAIStateManager.GetState(scholar);
            state.IsLostAnnotator = true;
            state.LostAnnotatorArchiveThingId = archive?.thingIDNumber ?? -1;
            ArchiveScholarInitializer.ConfigureLostAnnotator(scholar);
            GenSpawn.Spawn(scholar, cell, map);
            if (archive != null)
            {
                scholar.inventory.innerContainer.Remove(archive);
                GenPlace.TryPlaceThing(archive, archiveCell, map, ThingPlaceMode.Near);
            }
            SpawnDefenders(map, archiveCell);
            Messages.Message("学者被困在遗迹另一侧；他的文献由敌对守卫看守。", scholar, MessageTypeDefOf.ThreatBig);
            return true;
        }

        private static void SpawnDefenders(Map map, IntVec3 center)
        {
            int pattern = Rand.RangeInclusive(0, 2);
            PawnKindDef kind = pattern == 0 ? PawnKindDefOf.Mech_Scyther : pattern == 1 ? PawnKindDefOf.Megascarab : PawnKindDefOf.SpaceRefugee;
            Faction faction = pattern == 0 ? Faction.OfMechanoids : pattern == 1 ? Faction.OfInsects :
                Find.FactionManager.FirstFactionOfDef(FactionDefOf.Pirate);
            for (int i = 0; i < 3; i++)
            {
                IntVec3 cell = CellFinder.RandomClosewalkCellNear(center, map, 8);
                if (!cell.Standable(map) || cell.GetFirstPawn(map) != null) continue;
                Pawn defender = PawnGenerator.GeneratePawn(kind, faction);
                if (defender != null) GenSpawn.Spawn(defender, cell, map);
            }
        }
    }
}
