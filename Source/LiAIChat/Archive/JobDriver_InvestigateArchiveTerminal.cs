using LiAIChat.Questing;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace LiAIChat.Archive
{
    public class JobDriver_InvestigateArchiveTerminal : JobDriver
    {
        private Building_AncientArchiveSignalTerminal Terminal
        {
            get
            {
                return job.targetA.Thing
                    as Building_AncientArchiveSignalTerminal;
            }
        }

        public override bool TryMakePreToilReservations(
            bool errorOnFailed)
        {
            return pawn.Reserve(
                job.targetA,
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

            yield return Toils_Goto.GotoThing(
                TargetIndex.A,
                PathEndMode.Touch);

            // =====================================================
            // Investigation
            // =====================================================

            int investigationTicks =
    ArchiveTerminalInvestigationCalculator
        .GetInvestigationTicks(
            pawn);
            int elapsedTicks = 0;

            bool defenseTriggered = false;
            Toil investigate =
                ToilMaker.MakeToil(
                    "InvestigateAncientArchiveTerminal");

            investigate.defaultCompleteMode =
                ToilCompleteMode.Delay;


            investigate.defaultDuration =
                investigationTicks;

            investigate.tickAction =
    delegate
    {
        elapsedTicks++;

        if (defenseTriggered)
        {
            return;
        }

        float progress =
            (float)elapsedTicks /
            investigationTicks;

        if (progress < 0.20f)
        {
            return;
        }

        defenseTriggered = true;

        TriggerAncientDefense();
    };

            investigate.WithProgressBarToilDelay(
                TargetIndex.A);

            investigate.FailOnCannotTouch(
                TargetIndex.A,
                PathEndMode.Touch);

            yield return investigate;

            // =====================================================
            // SUCCESS
            //
            // This toil is reached ONLY if the investigation
            // toil actually completed.
            // =====================================================

            Toil complete =
                ToilMaker.MakeToil(
                    "CompleteAncientArchiveInvestigation");

            complete.initAction =
                delegate
                {
                    CompleteInvestigation();
                };

            complete.defaultCompleteMode =
                ToilCompleteMode.Instant;

            yield return complete;
        }
        private void TriggerAncientDefense()
        {
            if (pawn == null)
            {
                return;
            }

            Map map = pawn.Map;

            if (map == null)
            {
                return;
            }

            int wakeCount =
    ArchiveSiteMechanoidUtility
        .WakeArchiveDefenders(
            map);

            if (wakeCount <= 0)
            {
                return;
            }

            Messages.Message(
                "The ancient terminal has activated " +
                "a dormant defense system!",
                pawn,
                MessageTypeDefOf.ThreatBig);

            Log.Message(
                "[Li AI Chat] Ancient archive defense " +
                "activated. Mechanoids awakened: " +
                wakeCount);
        }

        private void CompleteInvestigation()
        {
            Building_AncientArchiveSignalTerminal terminal =
                Terminal;

            if (terminal == null ||
                terminal.Destroyed ||
                terminal.Map == null)
            {
                return;
            }

            Map map =
                terminal.Map;

            Site site =
                map.Parent as Site;

            if (site == null)
            {
                Log.Error(
                    "[Li AI Chat] Archive terminal investigation " +
                    "completed, but map parent is not a Site.");

                return;
            }

            Messages.Message(
                pawn.LabelShort +
                " has finished investigating " +
                "the ancient Earth signal.",
                terminal,
                MessageTypeDefOf.PositiveEvent);

            Log.Message(
                "[Li AI Chat] Ancient Archive Signal " +
                "investigation completed by " +
                pawn.LabelShort +
                ". Sending quest signal.");

            if (pawn.skills != null)
            {
                SkillRecord intellectual =
                    pawn.skills.GetSkill(
                        SkillDefOf.Intellectual);

                if (intellectual != null)
                {
                    intellectual.Learn(
                        300f);
                }
            }

            QuestUtility.SendQuestTargetSignals(
                site.questTags,
                "ArchiveInvestigated",
                site.Named("SUBJECT"));
        }
    }
}