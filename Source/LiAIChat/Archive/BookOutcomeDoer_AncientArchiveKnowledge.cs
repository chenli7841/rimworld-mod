using LiAIChat.Knowledge;
using LiAIChat.Models;
using LiAIChat.State;
using RimWorld;
using Verse;

namespace LiAIChat.Archive
{
    public class BookOutcomeDoer_AncientArchiveKnowledge
        : BookOutcomeDoer
    {
        public new ReadingOutcomeProperties_AncientArchiveKnowledge Props =>
            (ReadingOutcomeProperties_AncientArchiveKnowledge)props;

        public override bool DoesProvidesOutcome(Pawn reader)
        {
            if (reader == null)
                return false;

            Thing_AncientEarthArchiveFragment archive =
                Parent as Thing_AncientEarthArchiveFragment;

            if (archive == null)
                return false;

            if (!archive.Identified)
                return false;

            if (string.IsNullOrWhiteSpace(
                archive.KnowledgeTopicId))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(
                archive.StudyIdentityId))
            {
                return false;
            }

            return true;
        }

        public override void OnReadingTick(
            Pawn reader,
            float factor)
        {
            if (!DoesProvidesOutcome(reader))
                return;

            Thing_AncientEarthArchiveFragment archive =
                Parent as Thing_AncientEarthArchiveFragment;

            if (archive == null)
                return;

            PawnAIState state =
                PawnAIStateManager.GetState(reader);

            if (state == null)
                return;

            string studyId =
                archive.StudyIdentityId;


            float progress = state.GetArchiveReadingProgress(studyId);

            progress += factor;

            while (progress >= Props.learningInterval)
            {
                progress -= Props.learningInterval;

                ApplyKnowledge(
                    state,
                    archive);
            }

            state.SetArchiveReadingProgress(studyId, progress);
        }

        private void ApplyKnowledge(
            PawnAIState state,
            Thing_AncientEarthArchiveFragment archive)
        {
            string studyId =
                archive.StudyIdentityId;

            KnowledgeAcquisition acquisition =
                new KnowledgeAcquisition
                {
                    TopicId =
                        archive.KnowledgeTopicId,

                    LearningStrength =
                        Props.learningStrength,

                    Reason =
                        "Reading ancient Earth archive"
                };

            KnowledgeUpdater.Apply(
                state.Knowledge,
                acquisition);

            state.LearnEarthText(
                studyId,
                Props.learningStrength);

            CivilizationDomainKnowledgeUtility.ReconcileTextContribution(
                state,
                studyId,
                archive.KnowledgeTopicId);
        }
    }
}
