using LiAIChat.Archive;
using LiAIChat.State;
using RimWorld;
using Verse;

namespace LiAIChat.Questing
{
    public class SitePartWorker_LostAnnotator : SitePartWorker_LostEarthArchive
    {
        public override void PostMapGenerate(Map map)
        {
            base.PostMapGenerate(map);
            if (map == null) return;
            IntVec3 cell = CellFinderLoose.RandomCellWith(c => c.Standable(map) && c.GetFirstPawn(map) == null, map);
            Pawn scholar = PawnGenerator.GeneratePawn(PawnKindDefOf.SpaceRefugee, null);
            if (scholar == null) return;
            ArchiveScholarInitializer.Initialize(scholar);
            PawnAIStateManager.GetState(scholar).IsLostAnnotator = true;
            scholar.guest.SetGuestStatus(Faction.OfPlayer, GuestStatus.Guest);
            GenSpawn.Spawn(scholar, cell, map);
            Messages.Message("在遗迹中发现了一名携带远古注疏的学者。", scholar, MessageTypeDefOf.PositiveEvent);
        }
    }
}
