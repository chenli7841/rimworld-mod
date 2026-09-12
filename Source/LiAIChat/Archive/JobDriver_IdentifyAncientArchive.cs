using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace LiAIChat.Archive
{
    public class JobDriver_IdentifyAncientArchive
        : JobDriver
    {
        private Thing_AncientEarthArchiveFragment Archive =>
            job.targetA.Thing
                as Thing_AncientEarthArchiveFragment;

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

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(
                TargetIndex.A);

            yield return Toils_Reserve.Reserve(
                TargetIndex.A);

            yield return Toils_Goto.GotoThing(
                TargetIndex.A,
                PathEndMode.Touch);

            int identificationTicks =
                ArchiveIdentificationCalculator
                    .GetIdentificationTicks(
                        pawn,
                        Archive);

            Toil identify =
                Toils_General.Wait(
                    identificationTicks);

            identify.WithProgressBarToilDelay(
                TargetIndex.A);

            yield return identify;

            Toil finish =
                Toils_General.Do(delegate
                {
                    if (Archive == null)
                    {
                        return;
                    }

                    Archive.Identify();

                    if (pawn.skills != null)
                    {
                        pawn.skills.Learn(
                            SkillDefOf.Intellectual,
                            150f);
                    }

                    Messages.Message(
                        pawn.LabelShort
                        + " identified "
                        + Archive.Content.title
                        + ".",
                        Archive,
                        MessageTypeDefOf.PositiveEvent);
                });

            yield return finish;
        }

        private void CompleteIdentification()
        {
            if (Archive == null)
            {
                return;
            }

            if (Archive.Identified)
            {
                return;
            }

            Archive.Identify();

            Messages.Message(
                pawn.LabelShort +
                " identified the ancient Earth archive.",
                Archive,
                MessageTypeDefOf.PositiveEvent);
        }
    }
}