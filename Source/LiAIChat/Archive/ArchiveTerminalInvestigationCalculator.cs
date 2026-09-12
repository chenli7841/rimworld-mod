using RimWorld;
using UnityEngine;
using Verse;

namespace LiAIChat.Archive
{
    public static class ArchiveTerminalInvestigationCalculator
    {
        private const int BaseTicks = 6000;
        private const int MinimumTicks = 1800;

        public static int GetInvestigationTicks(
            Pawn pawn)
        {
            if (pawn == null)
            {
                return BaseTicks;
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

            float speedMultiplier =
                1f +
                intellectualLevel * 0.06f;

            int ticks =
                Mathf.RoundToInt(
                    BaseTicks /
                    speedMultiplier);

            return Mathf.Max(
                MinimumTicks,
                ticks);
        }
    }
}