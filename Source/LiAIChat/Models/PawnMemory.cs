using Verse;

namespace LiAIChat.Models
{
    public class PawnMemory : IExposable
    {
        public string Text = "";
        public float Importance = 0f;

        public PawnMemory()
        {
        }

        public PawnMemory(
            string text,
            float importance)
        {
            Text = text;
            Importance = importance;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(
                ref Text,
                "text",
                "");

            Scribe_Values.Look(
                ref Importance,
                "importance",
                0f);
        }
    }
}