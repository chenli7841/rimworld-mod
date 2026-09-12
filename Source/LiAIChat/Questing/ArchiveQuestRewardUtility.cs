using LiAIChat.Archive;

namespace LiAIChat.Questing
{
    public static class ArchiveQuestRewardUtility
    {
        public static
            Thing_AncientEarthArchiveFragment
            CreateQuestReward()
        {
            return
                AncientArchiveFactory.Create(
                    ArchiveSourceType.QuestReward,
                    "Recovered as a quest reward.");
        }
    }
}