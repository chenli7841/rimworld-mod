namespace LiAIChat.Models
{
    public class PawnExchangeImpact
    {
        // Knowledge
        public string LearnedTopicId = "";
        public float LearningStrength = 0f;

        // Worldview
        public float BeliefInGodDelta = 0f;
        public float BeliefInObjectiveMoralityDelta = 0f;
        public float TrustInChristianityDelta = 0f;
        public float KnowledgeOfChristianityDelta = 0f;
        public float IntellectualResistanceDelta = 0f;
        public float EmotionalResistanceDelta = 0f;
        public float SpiritualInterestDelta = 0f;

        public string Reason = "";
    }
}