using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace LiAIChat.Archive
{
    public class JobDriver_StudyAncientArchive
        : JobDriver
    {
        private const int StudyDurationTicks =
            2000;

        private Thing_AncientEarthArchiveFragment Archive =>
    job.targetA.Thing as Thing_AncientEarthArchiveFragment;

        public override bool TryMakePreToilReservations(
            bool errorOnFailed)
        {
            return pawn.Reserve(
                Archive,
                job,
                1,
                -1,
                null,
                errorOnFailed);
        }

        protected override IEnumerable<Toil>
            MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(
                TargetIndex.A);

            yield return
                Toils_Goto.GotoThing(
                    TargetIndex.A,
                    PathEndMode.Touch);

            Toil study =
                ToilMaker.MakeToil(
                    "StudyAncientEarthArchive");

            study.defaultCompleteMode =
                ToilCompleteMode.Delay;

            study.defaultDuration =
                ArchiveStudyCalculator
                    .GetStudyTicks(
                        pawn,
                        Archive);

            study.tickAction = () =>
            {
                pawn.rotationTracker
                    .FaceTarget(Archive);
            };

            study.WithProgressBarToilDelay(
                TargetIndex.A);

            yield return study;

            Toil finish =
                ToilMaker.MakeToil(
                    "FinishAncientEarthArchiveStudy");

            finish.initAction = () =>
            {
                if (Archive == null || !Archive.Identified)
                {
                    return;
                }
                AncientArchiveStudyUtility
                    .CompleteStudy(
                        pawn,
                        Archive);
            };

            finish.defaultCompleteMode =
                ToilCompleteMode.Instant;

            yield return finish;
        }
    }
}