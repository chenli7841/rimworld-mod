using RimWorld;
using Verse;
using LiAIChat.Civilization;

namespace LiAIChat.Archive
{
    public static class ArchiveStudyCalculator
    {
        private const int MinimumTicks = 600;

        public static float GetIntellectualXp(
    Thing_AncientEarthArchiveFragment archive)
        {
            float baseXp;

            if (archive == null ||
                archive.Content == null)
            {
                baseXp = 200f;
            }
            else
            {
                switch (archive.Content.studyDifficulty)
                {
                    case ArchiveStudyDifficulty.Easy:
                        baseXp = 150f;
                        break;

                    case ArchiveStudyDifficulty.Moderate:
                        baseXp = 250f;
                        break;

                    case ArchiveStudyDifficulty.Difficult:
                        baseXp = 400f;
                        break;

                    default:
                        baseXp = 200f;
                        break;
                }
            }

            return baseXp *
                CivilizationKnowledgeEffectUtility
                    .GetArchiveStudyXpFactor();
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

            speedMultiplier *=
                CivilizationKnowledgeEffectUtility
                    .GetArchiveStudySpeedFactor();

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
