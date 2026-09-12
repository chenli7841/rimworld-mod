using Verse;

namespace LiAIChat.Models
{
    public class WorldviewState : IExposable
    {
        public float BeliefInGod = 0.5f;
        public float BeliefInObjectiveMorality = 0.5f;

        public float TrustInChristianity = 0.3f;
        public float KnowledgeOfChristianity = 0.2f;

        public float IntellectualResistance = 0.5f;
        public float EmotionalResistance = 0.3f;

        public float SpiritualInterest = 0.5f;

        public WorldviewState()
        {
        }

        public void ExposeData()
        {
            Scribe_Values.Look(
                ref BeliefInGod,
                "beliefInGod",
                0.5f);

            Scribe_Values.Look(
                ref BeliefInObjectiveMorality,
                "beliefInObjectiveMorality",
                0.5f);

            Scribe_Values.Look(
                ref TrustInChristianity,
                "trustInChristianity",
                0.3f);

            Scribe_Values.Look(
                ref KnowledgeOfChristianity,
                "knowledgeOfChristianity",
                0.2f);

            Scribe_Values.Look(
                ref IntellectualResistance,
                "intellectualResistance",
                0.5f);

            Scribe_Values.Look(
                ref EmotionalResistance,
                "emotionalResistance",
                0.3f);

            Scribe_Values.Look(
                ref SpiritualInterest,
                "spiritualInterest",
                0.5f);
        }
    }
}