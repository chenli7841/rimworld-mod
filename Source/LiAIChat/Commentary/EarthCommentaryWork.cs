using System.Collections.Generic;
using Verse;

namespace LiAIChat.Commentary
{
    public class EarthCommentaryEntry : IExposable
    {
        public string Kind = "";
        public string Text = "";
        public int CreatedTick = -1;
        public int ReplyDueTick = -1;
        public bool PendingReply;

        public void ExposeData()
        {
            Scribe_Values.Look(ref Kind, "kind", "");
            Scribe_Values.Look(ref Text, "text", "");
            Scribe_Values.Look(ref CreatedTick, "createdTick", -1);
            Scribe_Values.Look(ref ReplyDueTick, "replyDueTick", -1);
            Scribe_Values.Look(ref PendingReply, "pendingReply", false);
        }
    }

    public class EarthCommentaryWork : IExposable
    {
        public int BookThingId = -1;
        public int AuthorPawnId = -1;
        public string AuthorName = "";
        public string EarthTextDefName = "";
        public string Title = "未完成的地球文献评注";
        public string Body = "";
        public float WritingProgress;
        public bool Generating;
        public List<EarthCommentaryEntry> Entries = new List<EarthCommentaryEntry>();

        public void ExposeData()
        {
            Scribe_Values.Look(ref BookThingId, "bookThingId", -1);
            Scribe_Values.Look(ref AuthorPawnId, "authorPawnId", -1);
            Scribe_Values.Look(ref AuthorName, "authorName", "");
            Scribe_Values.Look(ref EarthTextDefName, "earthTextDefName", "");
            Scribe_Values.Look(ref Title, "title", "未完成的地球文献评注");
            Scribe_Values.Look(ref Body, "body", "");
            Scribe_Values.Look(ref WritingProgress, "writingProgress", 0f);
            Scribe_Values.Look(ref Generating, "generating", false);
            Scribe_Collections.Look(ref Entries, "entries", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && Entries == null)
                Entries = new List<EarthCommentaryEntry>();
        }
    }
}
