using RimWorld;
using Verse;

namespace LiAIChat.Archive
{
    public static class ArchiveStudyCalculator
    {
        private const int MinimumTicks = 600;

        public static float GetIntellectualXp(
    Thing_AncientEarthArchiveFragment archive)
        {
            if (archive == null ||
                archive.Content == null)
            {
                return 200f;
            }

            switch (archive.Content.studyDifficulty)
            {
                case ArchiveStudyDifficulty.Easy:
                    return 150f;

                case ArchiveStudyDifficulty.Moderate:
                    return 250f;

                case ArchiveStudyDifficulty.Difficult:
                    return 400f;

                default:
                    return 200f;
            }
        }

        public static int GetStudyTicks(
            Pawn pawn,
            Thing_AncientEarthArchiveFragment archive)
        {
            int baseTicks =
                GetBaseTicks(archive);

            int intellectualLevel = 0;

            if (pawn != null &&
                pawn.skills != null)
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
                1f + intellectualLevel * 0.04f;

            int finalTicks =
                (int)(baseTicks / speedMultiplier);

            if (finalTicks < MinimumTicks)
            {
                finalTicks = MinimumTicks;
            }

            return finalTicks;
        }

        private static int GetBaseTicks(
            Thing_AncientEarthArchiveFragment archive)
        {
            if (archive == null ||
                archive.Content == null)
            {
                return 2000;
            }

            switch (archive.Content.studyDifficulty)
            {
                case ArchiveStudyDifficulty.Easy:
                    return 1800;

                case ArchiveStudyDifficulty.Moderate:
                    return 2800;

                case ArchiveStudyDifficulty.Difficult:
                    return 4200;

                default:
                    return 2000;
            }
        }
    }
}