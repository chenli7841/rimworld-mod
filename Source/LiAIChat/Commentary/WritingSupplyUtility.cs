using Verse;

namespace LiAIChat.Commentary
{
    // Values are intentionally centralized before the writing Job is wired in.
    public static class WritingSupplyUtility
    {
        public const int PaperPerWritingSession = 5;
        public const int PenLifetimeTicks = 30000; // 12 in-game hours

        public static float MoodFactor(ThingDef pen)
        {
            if (pen == null) return 1f;
            if (pen.defName == "LiAIChat_GoldPen") return 1.35f;
            if (pen.defName == "LiAIChat_SilverPen") return 1.15f;
            return 1f;
        }

        public static float NeedDrainFactor(ThingDef pen)
        {
            if (pen == null) return 1f;
            if (pen.defName == "LiAIChat_GoldPen") return 0.65f;
            if (pen.defName == "LiAIChat_SilverPen") return 0.82f;
            return 1f;
        }
    }
}
