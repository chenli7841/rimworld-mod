using System.Collections.Generic;
using System.Linq;
using LiAIChat.Archive;
using LiAIChat.Models;
using LiAIChat.State;
using RimWorld;
using Verse;
using Verse.AI;

namespace LiAIChat.Commentary
{
    public static class CommentaryIdleWritingManager
    {
        public static void TryAssignIdleWriters()
        {
            if (!CommentaryResearch.IsFinished)
                return;

            // The library index used to be rebuilt for every candidate text of
            // every idle colonist. Build it once for this complete pass instead.
            List<EarthTextDef> availableTexts = ColonyLibrary.GetAvailableTexts();
            if (availableTexts.Count == 0)
                return;

            foreach (Map map in Find.Maps.Where(candidate => candidate != null && candidate.IsPlayerHome))
            {
                List<Thing> allThings = map.listerThings.AllThings;
                Thing desk = allThings.FirstOrDefault(thing => thing.def.defName == "LiAIChat_WritingDesk");
                Thing shelf = allThings.FirstOrDefault(CommentaryService.IsShelf);
                bool hasPen = allThings.Any(thing => thing.def.defName == "LiAIChat_SteelPen" ||
                    thing.def.defName == "LiAIChat_SilverPen" || thing.def.defName == "LiAIChat_GoldPen");
                if (desk == null || shelf == null || !hasPen || !HasUsableWritingRoom(desk))
                    continue;

                foreach (Pawn pawn in map.mapPawns.FreeColonistsSpawned)
                {
                    if (pawn.CurJob == null || pawn.CurJob.def != JobDefOf.Wait || pawn.Drafted ||
                        pawn.needs?.mood == null || pawn.needs.mood.CurLevelPercentage < 0.65f)
                        continue;

                    PawnAIState state = PawnAIStateManager.TryGetExistingState(pawn);
                    if (state == null)
                        continue;

                    EarthTextDef text = availableTexts.FirstOrDefault(candidate =>
                        state.GetEarthTextFamiliarity(candidate.defName) >= 0.4f);
                    if (text == null || !ConsumePaper(map))
                        continue;

                    EarthCommentaryWork work = CommentaryService.Works.FirstOrDefault(candidate =>
                        candidate != null && candidate.AuthorPawnId == pawn.thingIDNumber &&
                        string.IsNullOrWhiteSpace(candidate.Body));
                    if (work == null)
                        work = CommentaryService.CreateDraft(pawn, text, shelf);

                    Thing_EarthCommentaryManuscript manuscript = work == null ? null :
                        map.listerThings.AllThings.OfType<Thing_EarthCommentaryManuscript>()
                            .FirstOrDefault(book => book.thingIDNumber == work.BookThingId);
                    if (manuscript != null)
                        pawn.jobs.TryTakeOrderedJob(new Job(LiAIChatJobDefOf.LiAIChat_WriteEarthCommentary, desk, manuscript), JobTag.Misc);
                }
            }
        }

        private static bool HasUsableWritingRoom(Thing desk)
        {
            Room room = desk.GetRoom();
            if (room == null || room.PsychologicallyOutdoors)
                return false;

            return GenAdj.CellsAdjacent8Way(desk).Any(cell => cell.InBounds(desk.Map) &&
                desk.Map.thingGrid.ThingsListAt(cell).Any(thing => thing.def.building != null && thing.def.building.isSittable));
        }

        private static bool ConsumePaper(Map map)
        {
            Thing paper = map.listerThings.AllThings.FirstOrDefault(thing =>
                thing.def.defName == "LiAIChat_WritingPaper" && thing.stackCount >= 5);
            if (paper == null)
                return false;
            paper.SplitOff(5).Destroy();
            return true;
        }

        public static void AddWritingTick(Thing_EarthCommentaryManuscript manuscript)
        {
            EarthCommentaryWork work = manuscript == null ? null : CommentaryService.Get(manuscript.thingIDNumber);
            if (work != null && string.IsNullOrWhiteSpace(work.Body))
                work.WritingProgress = System.Math.Min(1f, work.WritingProgress + 1f / 240000f);
        }
    }
}
