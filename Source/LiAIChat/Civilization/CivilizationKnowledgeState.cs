using Verse;

namespace LiAIChat.Civilization
{
    public class CivilizationKnowledgeState : IExposable
    {
        public string KnowledgeDefName;

        public bool Unlocked;

        public int UnlockedTick = -1;

        public int MissingSinceTick = -1;

        public void ExposeData()
        {
            Scribe_Values.Look(
                ref KnowledgeDefName,
                "knowledgeDefName",
                null);

            Scribe_Values.Look(
                ref Unlocked,
                "unlocked",
                false);

            Scribe_Values.Look(
                ref UnlockedTick,
                "unlockedTick",
                -1);

            Scribe_Values.Look(
                ref MissingSinceTick,
                "missingSinceTick",
                -1);
        }
    }
}