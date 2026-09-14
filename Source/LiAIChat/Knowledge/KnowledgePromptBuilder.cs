using LiAIChat.Archive;
using LiAIChat.Models;
using System.Collections.Generic;
using System.Text;

namespace LiAIChat.Knowledge
{
    public static class KnowledgePromptBuilder
    {
        public static string Build(
    KnowledgeState knowledge)
        {
            if (knowledge == null)
            {
                return "";
            }

            List<KnowledgeTopic> topics =
                knowledge.KnownTopics;

            if (topics == null ||
                topics.Count == 0)
            {
                return "";
            }

            StringBuilder sb =
                new StringBuilder();

            sb.AppendLine(
                "ANCIENT EARTH KNOWLEDGE:");

            foreach (KnowledgeTopic topic in topics)
            {
                if (topic == null)
                {
                    continue;
                }

                if (topic.Familiarity <= 0.01f)
                {
                    continue;
                }

                ArchiveContentDef content =
                    KnowledgeTopicDescriptionResolver
                        .FindArchiveContent(
                            topic.TopicId);

                if (content != null)
                {
                    sb.Append("- ");
                    sb.Append(content.title);
                    sb.Append(": ");
                    sb.AppendLine(
                        GetFamiliarityLabel(
                            topic.Familiarity));

                    if (!string.IsNullOrWhiteSpace(
                        content.summary))
                    {
                        sb.Append(
                            "  Learned content: ");

                        sb.AppendLine(
                            content.summary.Trim());
                    }
                }
                else
                {
                    string displayName =
                        KnowledgeTopicCatalog
                            .GetDisplayName(
                                topic.TopicId);

                    sb.Append("- ");
                    sb.Append(displayName);
                    sb.Append(": ");
                    sb.AppendLine(
                        GetFamiliarityLabel(
                            topic.Familiarity));
                }
            }

            sb.AppendLine();
            return sb.ToString();
        }

        private static string GetFamiliarityLabel(
            float familiarity)
        {
            if (familiarity >= 0.80f)
            {
                return "very familiar";
            }

            if (familiarity >= 0.60f)
            {
                return "moderately familiar";
            }

            if (familiarity >= 0.35f)
            {
                return "somewhat familiar";
            }

            if (familiarity >= 0.15f)
            {
                return "basic familiarity";
            }

            return "barely familiar";
        }
    }
}