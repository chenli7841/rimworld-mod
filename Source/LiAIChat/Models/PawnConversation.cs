using System.Collections.Generic;
using Verse;

namespace LiAIChat.Models
{
    public class PawnConversation : IExposable
    {
        public List<ChatMessage> Messages =
            new List<ChatMessage>();

        public string Summary = "";

        public int SummarizedMessageCount = 0;

        public PawnConversation()
        {
        }

        public void ExposeData()
        {
            Scribe_Collections.Look(
                ref Messages,
                "messages",
                LookMode.Deep);

            Scribe_Values.Look(
                ref Summary,
                "summary",
                "");

            Scribe_Values.Look(
                ref SummarizedMessageCount,
                "summarizedMessageCount",
                0);

            if (Scribe.mode ==
                LoadSaveMode.PostLoadInit)
            {
                if (Messages == null)
                {
                    Messages =
                        new List<ChatMessage>();
                }

                if (Summary == null)
                {
                    Summary = "";
                }
            }
        }
    }
}