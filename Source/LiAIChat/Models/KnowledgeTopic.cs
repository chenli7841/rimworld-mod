using Verse;

namespace LiAIChat.Models
{
    public class KnowledgeTopic : IExposable
    {
        public string TopicId = "";

        /// <summary>
        /// 0.10 - 听过名字，0.25	- 知道非常基本的概念，0.50 - 有一般理解，0.75 - 相当熟悉，1.00 - 深入掌握
        /// </summary>
        public float Familiarity = 0f;

        public KnowledgeTopic()
        {
        }

        public KnowledgeTopic(
            string topicId,
            float familiarity)
        {
            TopicId = topicId;
            Familiarity = familiarity;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(
                ref TopicId,
                "topicId",
                "");

            Scribe_Values.Look(
                ref Familiarity,
                "familiarity",
                0f);
        }
    }
}