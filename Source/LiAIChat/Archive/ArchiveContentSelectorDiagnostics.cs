using System.Collections.Generic;
using Verse;

namespace LiAIChat.Archive
{
    public static class ArchiveContentSelectorDiagnostics
    {
        public static void Run(
            ArchiveSourceType sourceType,
            int sampleCount = 1000)
        {
            Dictionary<ArchiveRarity, int> counts =
                new Dictionary<ArchiveRarity, int>
                {
                    { ArchiveRarity.Common, 0 },
                    { ArchiveRarity.Uncommon, 0 },
                    { ArchiveRarity.Rare, 0 }
                };

            for (int i = 0; i < sampleCount; i++)
            {
                ArchiveContentDef content =
                    ArchiveContentSelector
                        .SelectForSource(sourceType);

                if (content == null)
                {
                    continue;
                }

                counts[content.rarity]++;
            }

            Log.Message(
                "[Li AI Chat] Archive rarity test"
                + " source=" + sourceType
                + " samples=" + sampleCount
                + " Common=" + counts[ArchiveRarity.Common]
                + " Uncommon=" + counts[ArchiveRarity.Uncommon]
                + " Rare=" + counts[ArchiveRarity.Rare]);
        }
    }
}