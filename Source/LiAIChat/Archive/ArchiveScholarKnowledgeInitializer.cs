using LiAIChat.Knowledge;
using LiAIChat.Models;
using LiAIChat.State;
using Verse;

namespace LiAIChat.Archive
{
    public static class ArchiveScholarKnowledgeInitializer
    {
        public static void InitializeFromArchive(
            Pawn pawn,
            Thing_AncientEarthArchiveFragment archive)
        {
            if (pawn == null || archive == null)
            {
                return;
            }

            ArchiveContentDef content =
                archive.Content;

            if (content == null)
            {
                Log.Warning(
                    "[Li AI Chat] Scholar archive has no content.");

                return;
            }

            PawnAIState state =
                PawnAIStateManager.GetState(
                    pawn);

            if (state == null ||
                state.Knowledge == null)
            {
                return;
            }

            float specificFamiliarity =
                Rand.Range(
                    0.70f,
                    0.90f);

            EnsureMinimumKnowledge(
                state.Knowledge,
                content.topicId,
                specificFamiliarity);

            if (!content.parentTopicId.NullOrEmpty() &&
                KnowledgeTopicCatalog.Contains(
                    content.parentTopicId))
            {
                float parentFamiliarity =
                    Rand.Range(
                        0.45f,
                        0.70f);

                EnsureMinimumKnowledge(
                    state.Knowledge,
                    content.parentTopicId,
                    parentFamiliarity);
            }

            Log.Message(
                "[Li AI Chat] Scholar knowledge initialized. " +
                pawn.LabelShort +
                " topic=" +
                content.topicId +
                " familiarity=" +
                specificFamiliarity.ToString("0.00"));
        }

        private static void EnsureMinimumKnowledge(
            KnowledgeState knowledge,
            string topicId,
            float targetFamiliarity)
        {
            if (knowledge == null ||
                topicId.NullOrEmpty())
            {
                return;
            }

            float current =
                knowledge.GetFamiliarity(
                    topicId);

            if (current >= targetFamiliarity)
            {
                return;
            }

            float remaining =
                1f - current;

            if (remaining <= 0f)
            {
                return;
            }

            float learningStrength =
                (targetFamiliarity - current) /
                remaining;

            KnowledgeAcquisition acquisition =
                new KnowledgeAcquisition
                {
                    TopicId = topicId,
                    LearningStrength =
                        learningStrength
                };

            KnowledgeUpdater.Apply(
                knowledge,
                acquisition);
        }
    }
}