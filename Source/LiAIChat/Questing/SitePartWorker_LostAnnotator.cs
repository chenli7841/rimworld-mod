using LiAIChat.Archive;
using LiAIChat.State;
using LiAIChat.Models;
using RimWorld;
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
            foreach (Pawn existing in map.mapPawns.AllPawnsSpawned)
            {
                PawnAIState existingState = PawnAIStateManager.TryGetExistingState(existing);
                if (existingState != null && existingState.IsLostAnnotator)
                {
                    ArchiveScholarInitializer.ConfigureLostAnnotator(existing);
                    return true;
                }
            }

            IntVec3 cell = CellFinderLoose.RandomCellWith(c => c.Standable(map) && c.GetFirstPawn(map) == null, map);
            if (!cell.IsValid) return false;
            Pawn scholar = PawnGenerator.GeneratePawn(PawnKindDefOf.SpaceRefugee, null);
            if (scholar == null) return false;
            ArchiveScholarInitializer.Initialize(scholar);
            PawnAIState state = PawnAIStateManager.GetState(scholar);
            state.IsLostAnnotator = true;
            ArchiveScholarInitializer.ConfigureLostAnnotator(scholar);
            GenSpawn.Spawn(scholar, cell, map);
            Messages.Message("在遗迹中发现了一名携带远古注疏的学者。", scholar, MessageTypeDefOf.PositiveEvent);
            return true;
        }
    }
}
