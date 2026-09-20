using Verse;

namespace LiAIChat.Heritage
{
    public class CivilizationHeritageWork : IExposable
    {
        public int ThingId = -1;
        public int CreatorPawnId = -1;
        public string CreatorName = "";
        public string Title = "";
        public string Description = "";
        public int CreatedTick = -1;
        public bool Pending;

        public void ExposeData()
        {
            Scribe_Values.Look(ref ThingId, "thingId", -1);
            Scribe_Values.Look(ref CreatorPawnId, "creatorPawnId", -1);
            Scribe_Values.Look(ref CreatorName, "creatorName", "");
            Scribe_Values.Look(ref Title, "title", "");
            Scribe_Values.Look(ref Description, "description", "");
            Scribe_Values.Look(ref CreatedTick, "createdTick", -1);
            Scribe_Values.Look(ref Pending, "pending", false);
        }
    }
}
