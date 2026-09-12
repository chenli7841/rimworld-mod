using RimWorld;
using Verse;

namespace LiAIChat.Archive
{
    public static class ArchiveIdentificationCalculator
    {
        private const int MinimumTicks = 300;

        public static string GetDifficultyLabel(
    Thing_AncientEarthArchiveFragment archive)
        {
            if (archive == null ||
                archive.Content == null)
            {
                return "Unknown";
            }

            switch (archive.Content.rarity)
            {
                case ArchiveRarity.Common:
                    return "Easy";

                case ArchiveRarity.Uncommon:
                    return "Moderate";

                case ArchiveRarity.Rare:
                    return "Difficult";

                default:
                    return "Unknown";
            }
        }

        public static int GetIdentificationTicks(
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
                1f + intellectualLevel * 0.05f;

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
                return 1500;
            }

            switch (archive.Content.rarity)
            {
                case ArchiveRarity.Common:
                    return 1000;

                case ArchiveRarity.Uncommon:
                    return 1800;

                case ArchiveRarity.Rare:
                    return 3000;

                default:
                    return 1500;
            }
        }
    }
}