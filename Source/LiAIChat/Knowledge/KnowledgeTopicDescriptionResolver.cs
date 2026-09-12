using System.Collections.Generic;
using Verse;
using LiAIChat.Archive;

namespace LiAIChat.Knowledge
{
    public static class KnowledgeTopicDescriptionResolver
    {
        public static ArchiveContentDef FindArchiveContent(
            string topicId)
        {
            if (string.IsNullOrWhiteSpace(topicId))
            {
                return null;
            }

            List<ArchiveContentDef> defs =
                DefDatabase<ArchiveContentDef>.AllDefsListForReading;

            if (defs == null)
            {
                return null;
            }

            for (int i = 0; i < defs.Count; i++)
            {
                ArchiveContentDef def =
                    defs[i];

                if (def == null)
                {
                    continue;
                }

                if (def.topicId == topicId)
                {
                    return def;
                }
            }

            return null;
        }
    }
}