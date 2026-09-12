using Verse;

namespace LiAIChat.Archive
{
    public enum ArchiveRarity
    {
        Common,
        Uncommon,
        Rare
    }

    public enum ArchiveTheme
    {
        History,
        Philosophy,
        Religion,
        Politics,
        Science
    }
    public enum ArchiveStudyDifficulty
    {
        Easy,
        Moderate,
        Difficult
    }

    public class ArchiveContentDef : Def
    {
        public string topicId;
        public string parentTopicId;

        public string title;
        public string summary;

        public float familiarityGain = 0.20f;
        public float parentFamiliarityGain = 0.05f;

        public ArchiveRarity rarity = ArchiveRarity.Common;
        public ArchiveTheme theme = ArchiveTheme.History;
        public ArchiveStudyDifficulty studyDifficulty = ArchiveStudyDifficulty.Moderate;

    }
}