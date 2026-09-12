using RimWorld;
using UnityEngine;
using Verse;

namespace LiAIChat.Archive
{
    public static class ArchiveLootRules
    {
        private const float BaseAncientSoldierChance = 0.05f;

        private const float IntellectualBonusPerSkillPoint = 0.01f;

        private const float MaxIntellectualBonus = 0.15f;

        private const float MaximumChance = 0.30f;

        public static float GetArchiveChance(
            Pawn pawn)
        {
            if (pawn == null)
            {
                return 0f;
            }

            if (pawn.kindDef !=
                PawnKindDefOf.AncientSoldier)
            {
                return 0f;
            }

            float chance =
                BaseAncientSoldierChance;

            chance +=
                GetIntellectualBonus(pawn);

            return
                Mathf.Min(
                    chance,
                    MaximumChance);
        }

        private static float GetIntellectualBonus(
            Pawn pawn)
        {
            SkillRecord intellectual =
                pawn.skills?
                    .GetSkill(
                        SkillDefOf.Intellectual);

            if (intellectual == null)
            {
                return 0f;
            }

            float bonus =
                intellectual.Level *
                IntellectualBonusPerSkillPoint;

            return
                Mathf.Min(
                    bonus,
                    MaxIntellectualBonus);
        }
    }
}