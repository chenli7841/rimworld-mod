using Verse;

namespace LiAIChat.Models
{
    public class PawnIntellectualExchange : IExposable
    {
        public int OtherPawnId = -1;

        public string Summary = "";

        public int CreatedTick = 0;

        public PawnIntellectualExchange()
        {
        }

        public PawnIntellectualExchange(
            int otherPawnId,
            string summary,
            int createdTick)
        {
            OtherPawnId = otherPawnId;
            Summary = summary;
            CreatedTick = createdTick;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(
                ref OtherPawnId,
                "otherPawnId",
                -1);

            Scribe_Values.Look(
                ref Summary,
                "summary",
                "");

            Scribe_Values.Look(
                ref CreatedTick,
                "createdTick",
                0);
        }
    }
}