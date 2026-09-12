using System.Collections.Generic;
using Verse;

namespace LiAIChat.Models
{
    public class KnowledgeState : IExposable
    {
        /// <summary>
        /// 0.05 - 可能只知道：人类起源于一个叫 Earth 的古老世界。
        /// 0.80 - 可能知道：Roman Empire, Renaissance, World Wars, Cold War, 21st century, 但仍不等于知道每个细节。
        /// </summary>
        public float EarthHistoryKnowledge = 0.1f;

        /// <summary>
        /// 0.10 - 也许只知道：古人讨论过伦理、生命意义之类的问题。
        /// 0.90 - 可能熟悉：Plato, Aristotle, Stoicism, Kant, Kierkegaard,, Nietzsche
        /// </summary>
        public float PhilosophyKnowledge = 0.1f;
        public float ReligiousKnowledge = 0.1f;
        public float PoliticsKnowledge = 0.1f;
        public float ScienceKnowledge = 0.2f;

        public List<KnowledgeTopic> KnownTopics =   new List<KnowledgeTopic>();

        public KnowledgeState()
        {
        }

        public void ExposeData()
        {
            Scribe_Values.Look(
                ref EarthHistoryKnowledge,
                "earthHistoryKnowledge",
                0.1f);

            Scribe_Values.Look(
                ref PhilosophyKnowledge,
                "philosophyKnowledge",
                0.1f);

            Scribe_Values.Look(
                ref ReligiousKnowledge,
                "religiousKnowledge",
                0.1f);

            Scribe_Values.Look(
                ref PoliticsKnowledge,
                "politicsKnowledge",
                0.1f);

            Scribe_Values.Look(
                ref ScienceKnowledge,
                "scienceKnowledge",
                0.2f);

            Scribe_Collections.Look(
                ref KnownTopics,
                "knownTopics",
                LookMode.Deep);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (KnownTopics == null)
                {
                    KnownTopics =
                        new List<KnowledgeTopic>();
                }
            }
        }

        public KnowledgeTopic GetTopic(string topicId)
        {
            if (string.IsNullOrWhiteSpace(topicId))
                return null;

            foreach (KnowledgeTopic topic
                     in KnownTopics)
            {
                if (topic == null)
                    continue;

                if (topic.TopicId == topicId)
                    return topic;
            }

            return null;
        }

        public bool KnowsTopic(string topicId)
        {
            KnowledgeTopic topic =
                GetTopic(topicId);

            return topic != null &&
                   topic.Familiarity > 0f;
        }

        public float GetFamiliarity(string topicId)
        {
            KnowledgeTopic topic =
                GetTopic(topicId);

            if (topic == null)
                return 0f;

            return topic.Familiarity;
        }

        public void LearnTopic(string topicId, float familiarityGain)
        {
            if (string.IsNullOrWhiteSpace(topicId))
                return;

            if (familiarityGain <= 0f)
                return;

            KnowledgeTopic topic =
                GetTopic(topicId);

            if (topic == null)
            {
                topic =
                    new KnowledgeTopic(
                        topicId,
                        0f);

                KnownTopics.Add(topic);
            }

            topic.Familiarity =
                Clamp01(
                    topic.Familiarity +
                    familiarityGain);
        }

        private static float Clamp01(float value)
        {
            if (value < 0f)
                return 0f;

            if (value > 1f)
                return 1f;

            return value;
        }
    }
}