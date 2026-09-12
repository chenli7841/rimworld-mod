using LiAIChat.Questing;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;

namespace LiAIChat.Archive
{
    public class Building_AncientArchiveSignalTerminal : Building
    {
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }

            Command_Action investigate =
                new Command_Action();

            investigate.defaultLabel =
                "Investigate Ancient Signal";

            investigate.defaultDesc =
                "Order a colonist to investigate the damaged " +
                "Ancient Earth signal terminal.";

            investigate.action =
                delegate
                {
                    TryStartInvestigation();
                };

            yield return investigate;
        }
        public override string GetInspectString()
        {
            string baseText =
                base.GetInspectString();

            string archiveText =
                "The terminal is badly weathered, but a weak " +
                "data signal is still active.";

            string securityText =
                GetSecurityStatusText();

            string result =
                archiveText +
                "\n" +
                securityText;

            if (string.IsNullOrEmpty(baseText))
            {
                return result;
            }

            return baseText +
                   "\n" +
                   result;
        }
        private string GetSecurityStatusText()
        {
            Map map =
                Map;

            if (map == null)
            {
                return "";
            }

            int defenders =
                ArchiveSiteDefenderUtility
                    .CountActiveDefenders(
                        map);

            if (defenders <= 0)
            {
                return
                    "No active ancient defenders are detected nearby.";
            }

            if (defenders == 1)
            {
                return
                    "1 active ancient defender remains nearby.";
            }

            return
                defenders +
                " active ancient defenders remain nearby.";
        }

        private void TryStartInvestigation()
        {
            Map map = Map;

            if (map == null)
            {
                return;
            }

            List<FloatMenuOption> options =
                new List<FloatMenuOption>();

            foreach (Pawn pawn in map.mapPawns.FreeColonistsSpawned)
            {
                if (pawn == null)
                {
                    continue;
                }

                if (!pawn.CanReach(
                    this,
                    PathEndMode.Touch,
                    Danger.Deadly))
                {
                    continue;
                }

                Pawn selectedPawn = pawn;

                options.Add(
                    new FloatMenuOption(
                        selectedPawn.LabelShort,
                        delegate
                        {
                            ShowRiskAssessment(pawn);
                            Job job =
                                JobMaker.MakeJob(
                                    LiAIChatJobDefOf
                                        .LiAIChat_InvestigateArchiveTerminal,
                                    this);

                            selectedPawn.jobs.TryTakeOrderedJob(
                                job,
                                JobTag.Misc);
                        }));
            }

            if (options.Count == 0)
            {
                Messages.Message(
                    "No colonist can reach the terminal.",
                    MessageTypeDefOf.RejectInput,
                    false);

                return;
            }

            Find.WindowStack.Add(
                new FloatMenu(options));
        }
        private void ShowRiskAssessment(
    Pawn pawn)
        {
            string assessment =
                ArchiveTerminalRiskAssessment
                    .GetAssessment(
                        pawn,
                        this);

            if (assessment.NullOrEmpty())
            {
                return;
            }

            Messages.Message(
                pawn.LabelShort +
                ": " +
                assessment,
                this,
                MessageTypeDefOf.CautionInput);
        }
    }
}