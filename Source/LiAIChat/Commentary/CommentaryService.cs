using LiAIChat.Game;
using LiAIChat.Archive;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace LiAIChat.Commentary
{
    public static class CommentaryService
    {
        public static List<EarthCommentaryWork> Works
        {
            get
            {
                LiAIChatGameComponent component = Current.Game == null ? null : Current.Game.GetComponent<LiAIChatGameComponent>();
                return component == null ? new List<EarthCommentaryWork>() : component.EarthCommentaryWorks;
            }
        }

        public static EarthCommentaryWork Get(int bookThingId)
        {
            return Works.FirstOrDefault(work => work != null && work.BookThingId == bookThingId);
        }

        public static bool IsShelf(Thing thing)
        {
            return thing != null && thing.def != null &&
                thing.def.defName.IndexOf("Shelf", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static List<Thing_EarthCommentaryManuscript> GetManuscriptsOnShelf(Thing shelf)
        {
            if (!IsShelf(shelf) || shelf.Map == null) return new List<Thing_EarthCommentaryManuscript>();
            return shelf.Map.thingGrid.ThingsListAt(shelf.Position)
                .OfType<Thing_EarthCommentaryManuscript>().ToList();
        }

        public static Thing_EarthCommentaryManuscript FindManuscript(int thingId)
        {
            foreach (Map map in Find.Maps)
            {
                Thing_EarthCommentaryManuscript book = map.listerThings.AllThings.OfType<Thing_EarthCommentaryManuscript>().FirstOrDefault(item => item.thingIDNumber == thingId);
                if (book != null) return book;
            }
            return null;
        }

        public static EarthCommentaryWork CreateDraft(Pawn author, EarthTextDef source, Thing shelf)
        {
            if (author == null || source == null || !IsShelf(shelf) || shelf.Map == null) return null;
            ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail("LiAIChat_EarthCommentaryManuscript");
            if (def == null) return null;
            Thing_EarthCommentaryManuscript book = ThingMaker.MakeThing(def) as Thing_EarthCommentaryManuscript;
            if (book == null || !GenPlace.TryPlaceThing(book, shelf.Position, shelf.Map, ThingPlaceMode.Direct)) return null;
            EarthCommentaryWork work = new EarthCommentaryWork
            {
                BookThingId = book.thingIDNumber, AuthorPawnId = author.thingIDNumber,
                AuthorName = author.LabelShort, EarthTextDefName = source.defName,
                Title = "《" + source.title + "》评注"
            };
            Works.Add(work);
            LiAIChat.Events.ColonyEventLog.Record("开始著作",
                author.LabelShort + " 开始撰写“" + work.Title + "”。", 2, author,
                "commentary-draft:" + author.thingIDNumber + ":" + source.defName);
            return work;
        }

        public static void AddPlayerAnnotation(EarthCommentaryWork work, string text)
        {
            if (work == null || string.IsNullOrWhiteSpace(text) || work.Generating || !CommentaryResearch.MarginaliaIsFinished)
                return;
            work.Entries.Add(new EarthCommentaryEntry
            {
                Kind = "mysteriousAnnotation",
                Text = text.Trim(),
                CreatedTick = Find.TickManager == null ? -1 : Find.TickManager.TicksGame,
                ReplyDueTick = Find.TickManager == null ? -1 : Find.TickManager.TicksGame + 180000,
                PendingReply = true
            });
        }

        public static string Status(EarthCommentaryWork work)
        {
            if (work == null) return "";
            if (work.Generating) return "正在形成正文……";
            if (string.IsNullOrWhiteSpace(work.Body)) return "撰写中：" + work.WritingProgress.ToStringPercent();
            EarthCommentaryEntry pending = work.Entries.LastOrDefault(entry => entry != null && entry.PendingReply);
            return pending == null ? "已完成" : "作者正在思考神秘批注。";
        }
    }
}
