using Verse;

namespace LiAIChat.Models
{
    public class ChatMessage : IExposable
    {
        public bool IsPlayer;
        public string Text;

        public ChatMessage()
        {
        }

        public ChatMessage(
            bool isPlayer,
            string text)
        {
            IsPlayer = isPlayer;
            Text = text;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(
                ref IsPlayer,
                "isPlayer",
                false);

            Scribe_Values.Look(
                ref Text,
                "text",
                "");
        }
    }
}