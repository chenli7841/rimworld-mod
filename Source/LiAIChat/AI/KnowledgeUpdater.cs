using LiAIChat.Models;

namespace LiAIChat.Knowledge
{
    public static class KnowledgeUpdater
    {
        public static float Apply(
            KnowledgeState knowledge,
            KnowledgeAcquisition acquisition)
        {
            if (knowledge == null ||
                acquisition == null)
            {
                return 0f;
            }

            if (!KnowledgeTopicCatalog.Contains(
                acquisition.TopicId))
            {
                return 0f;
            }

            float current =
                knowledge.GetFamiliarity(
                    acquisition.TopicId);

            float proposed =
                acquisition.LearningStrength;

            if (proposed < 0f)
                proposed = 0f;

            if (proposed > 0.05f)
                proposed = 0.05f;

            // 越熟悉，同样一次普通谈话带来的新增知识越少
            // 比如给一个完全不知道 Kant 的人解释“物自体”，可能带来明显新知识。但给一个 Kant 专家讲同样一句，则基本没学到什么。
            float remainingKnowledge =
                1f - current;

            float applied =
                proposed *
                remainingKnowledge;

            if (applied < 0.001f)
                return 0f;

            knowledge.LearnTopic(
                acquisition.TopicId,
                applied);

            return applied;
        }
    }
}