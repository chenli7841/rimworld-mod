using System.Collections.Generic;
using System.Linq;
using Verse;

namespace LiAIChat.Archive
{
    public static class ArchiveContentSelector
    {
        public static ArchiveContentDef SelectForSource(
    ArchiveSourceType sourceType)
        {
            ArchiveRarity rarity =
                SelectRarity(sourceType);

            ArchiveTheme theme =
                SelectTheme(sourceType);

            List<ArchiveContentDef> candidates =
                DefDatabase<ArchiveContentDef>
                    .AllDefsListForReading
                    .Where(def =>
                        def != null &&
                        def.rarity == rarity &&
                        def.theme == theme)
                    .ToList();

            if (candidates.Count > 0)
            {
                return candidates.RandomElement();
            }

            // 第一级 fallback：
            // 保留 rarity，但放宽 theme
            candidates =
                DefDatabase<ArchiveContentDef>
                    .AllDefsListForReading
                    .Where(def =>
                        def != null &&
                        def.rarity == rarity)
                    .ToList();

            if (candidates.Count > 0)
            {
                return candidates.RandomElement();
            }

            // 最后 fallback：
            // 从全部内容中随机
            return SelectFallback();
        }

        private static ArchiveTheme SelectTheme(
    ArchiveSourceType sourceType)
        {
            float roll = Rand.Value * 100f;

            switch (sourceType)
            {
                case ArchiveSourceType.AncientPawn:
                    if (roll < 35f)
                        return ArchiveTheme.History;

                    if (roll < 60f)
                        return ArchiveTheme.Politics;

                    if (roll < 75f)
                        return ArchiveTheme.Religion;

                    if (roll < 90f)
                        return ArchiveTheme.Philosophy;

                    return ArchiveTheme.Science;

                case ArchiveSourceType.Trader:
                    if (roll < 20f)
                        return ArchiveTheme.History;

                    if (roll < 40f)
                        return ArchiveTheme.Philosophy;

                    if (roll < 60f)
                        return ArchiveTheme.Religion;

                    if (roll < 80f)
                        return ArchiveTheme.Politics;

                    return ArchiveTheme.Science;

                case ArchiveSourceType.QuestReward:
                    if (roll < 15f)
                        return ArchiveTheme.History;

                    if (roll < 40f)
                        return ArchiveTheme.Philosophy;

                    if (roll < 65f)
                        return ArchiveTheme.Religion;

                    if (roll < 80f)
                        return ArchiveTheme.Politics;

                    return ArchiveTheme.Science;

                case ArchiveSourceType.AncientRuin:
                    if (roll < 35f)
                        return ArchiveTheme.History;

                    if (roll < 50f)
                        return ArchiveTheme.Philosophy;

                    if (roll < 70f)
                        return ArchiveTheme.Religion;

                    if (roll < 80f)
                        return ArchiveTheme.Politics;

                    return ArchiveTheme.Science;

                default:
                    if (roll < 20f)
                        return ArchiveTheme.History;

                    if (roll < 40f)
                        return ArchiveTheme.Philosophy;

                    if (roll < 60f)
                        return ArchiveTheme.Religion;

                    if (roll < 80f)
                        return ArchiveTheme.Politics;

                    return ArchiveTheme.Science;
            }
        }

        private static ArchiveRarity SelectRarity(
    ArchiveSourceType sourceType)
        {
            switch (sourceType)
            {
                case ArchiveSourceType.AncientPawn:
                    return RandomRarity(
                        70f,
                        25f,
                        5f);

                case ArchiveSourceType.Trader:
                    return RandomRarity(
                        35f,
                        45f,
                        20f);

                case ArchiveSourceType.QuestReward:
                    return RandomRarity(
                        20f,
                        45f,
                        35f);

                case ArchiveSourceType.AncientRuin:
                    return RandomRarity(
                        50f,
                        35f,
                        15f);

                default:
                    return RandomRarity(
                        60f,
                        30f,
                        10f);
            }
        }

        private static ArchiveRarity RandomRarity(
    float commonWeight,
    float uncommonWeight,
    float rareWeight)
        {
            float total =
                commonWeight +
                uncommonWeight +
                rareWeight;

            float roll =
                Rand.Value * total;

            if (roll < commonWeight)
            {
                return ArchiveRarity.Common;
            }

            roll -= commonWeight;

            if (roll < uncommonWeight)
            {
                return ArchiveRarity.Uncommon;
            }

            return ArchiveRarity.Rare;
        }

        private static ArchiveContentDef SelectFallback()
        {
            List<ArchiveContentDef> all =
                DefDatabase<ArchiveContentDef>
                    .AllDefsListForReading
                    .Where(def => def != null)
                    .ToList();

            if (all.Count == 0)
            {
                return null;
            }

            return all.RandomElement();
        }
    }
}