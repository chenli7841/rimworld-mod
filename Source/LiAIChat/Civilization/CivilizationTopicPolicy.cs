using System;

namespace LiAIChat.Civilization
{
    // Pure rules shared by the game component and standalone regression checks.
    public static class CivilizationTopicPolicy
    {
        public const int BonusDays = 7;
        public const int MinimumCooldownDays = 2;
        public const int MaximumCooldownDays = 4;
        public const float RepeatEfficiency = 0.8f;
        public const float MinimumEfficiency = 0.4f;

        public static float ResearchEfficiency(bool sameTopic, int consecutiveCompletions)
        {
            return sameTopic ? Math.Max(MinimumEfficiency,
                (float)Math.Pow(RepeatEfficiency, Math.Max(0, consecutiveCompletions))) : 1f;
        }

        public static float RewardWeight(int previousDraws)
        {
            return 1f / (1f + Math.Max(0, previousDraws));
        }
    }
}
