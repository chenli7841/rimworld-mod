using System.Collections.Generic;

namespace LiAIChat.Background
{
    public class PawnAISnapshot
    {
        public int PawnId;

        public string Name = "";

        public List<string> Traits =
            new List<string>();

        // Worldview
        public float BeliefInGod;
        public float BeliefInObjectiveMorality;
        public float TrustInChristianity;
        public float KnowledgeOfChristianity;
        public float IntellectualResistance;
        public float EmotionalResistance;
        public float SpiritualInterest;

        // Meaning
        public float Purpose;
        public float Belonging;
        public float Hope;
        public float Coherence;
        public float Transcendence;

        // Knowledge
        public List<KnowledgeTopicSnapshot>
            KnownTopics =
                new List<KnowledgeTopicSnapshot>();

        // Memories
        public List<string> ImportantMemories =
            new List<string>();

        // Life goal
        public string LifeGoalTitle = "";
        public string LifeGoalDescription = "";

        public string CurrentRelationships;
    }
}