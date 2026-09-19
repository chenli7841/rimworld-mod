using RimWorld;
using Verse;

namespace LiAIChat.Archive
{
    public class BookOutcomeDoer_AncientArchiveKnowledge
        : BookOutcomeDoer
    {
        private const float LearningInterval = 2500f;

        private float accumulatedReading;

        public override bool DoesProvidesOutcome(Pawn reader)
        {
            Thing_AncientEarthArchiveFragment archive =
                Parent as Thing_AncientEarthArchiveFragment;

            if (archive == null)
                return false;

            if (!archive.Identified)
                return false;

            if (archive.EarthText == null)
                return false;

            return !string.IsNullOrEmpty(
                archive.KnowledgeTopicId);
        }

        public override void OnReadingTick(
            Pawn reader,
            float factor)
        {
            if (!DoesProvidesOutcome(reader))
                return;

            accumulatedReading += factor;

            if (accumulatedReading < LearningInterval)
                return;

            accumulatedReading -= LearningInterval;

            ApplyKnowledge(reader);
        }

        private void ApplyKnowledge(Pawn reader)
        {
            // 下一小步实现
        }

        public override void PostExposeData()
        {
            base.PostExposeData();

            Scribe_Values.Look(
                ref accumulatedReading,
                "liAIChatAccumulatedReading",
                0f);
        }
    }
}