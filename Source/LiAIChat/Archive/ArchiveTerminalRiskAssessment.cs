using LiAIChat.Questing;
using RimWorld;
using Verse;

namespace LiAIChat.Archive
{
    public static class ArchiveTerminalRiskAssessment
    {
        public static string GetAssessment(
            Pawn pawn,
            Building_AncientArchiveSignalTerminal terminal)
        {
            if (pawn == null ||
                terminal == null ||
                terminal.Map == null)
            {
                return null;
            }

            ArchiveSiteDefenseComponent
                defenseComponent =
                    terminal.Map.GetComponent<
                        ArchiveSiteDefenseComponent>();

            if (defenseComponent == null ||
                !defenseComponent
                    .HasRegisteredDefenders)
            {
                return null;
            }

            int intellectualLevel = 0;

            if (pawn.skills != null)
            {
                SkillRecord intellectual =
                    pawn.skills.GetSkill(
                        SkillDefOf.Intellectual);

                if (intellectual != null)
                {
                    intellectualLevel =
                        intellectual.Level;
                }
            }

            if (intellectualLevel >= 12)
            {
                return
                    "The terminal appears to remain connected " +
                    "to an active security subsystem.";
            }

            if (intellectualLevel >= 6)
            {
                return
                    "Some secondary systems still appear " +
                    "to be active.";
            }

            return null;
        }
    }
}