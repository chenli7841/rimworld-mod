using Verse;

namespace LiAIChat.Civilization
{
    public class CivilizationKnowledgeState : IExposable
    {
        public string KnowledgeDefName;

        public bool Unlocked;

        public int UnlockedTick = -1;

        public int MissingSinceTick = -1;

        public bool Unstable;

        public bool Dormant;

        public bool AwaitingReactivation;

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

            Scribe_Values.Look(
                ref Unstable,
                "unstable",
                false);

            Scribe_Values.Look(
                ref Dormant,
                "dormant",
                false);

            Scribe_Values.Look(
                ref AwaitingReactivation,
                "awaitingReactivation",
                false);
        }
    }
}