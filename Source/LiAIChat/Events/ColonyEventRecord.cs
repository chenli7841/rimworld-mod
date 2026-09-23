using Verse;

namespace LiAIChat.Events
{
    // A compact colony fact, kept apart from each pawn's personal memories.
    public class ColonyEventRecord : IExposable
    {
        public string Category;
        public string Summary;
        public string DeduplicationKey;
        public int Importance;
        public int CreatedTick = -1;
        public int RelatedPawnId = -1;

        public void ExposeData()
        {
            Scribe_Values.Look(ref Category, "category");
            Scribe_Values.Look(ref Summary, "summary");
            Scribe_Values.Look(ref DeduplicationKey, "deduplicationKey");
            Scribe_Values.Look(ref Importance, "importance", 1);
            Scribe_Values.Look(ref CreatedTick, "createdTick", -1);
            Scribe_Values.Look(ref RelatedPawnId, "relatedPawnId", -1);
        }
    }
}
