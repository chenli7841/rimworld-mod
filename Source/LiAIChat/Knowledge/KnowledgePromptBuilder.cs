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

            sb.AppendLine(
                "Knowledge rules:");

            sb.AppendLine(
                "- Use this knowledge naturally when relevant.");

            sb.AppendLine(
                "- Familiarity represents approximate understanding, not perfect recall.");

            sb.AppendLine(
                "- The descriptions above define the core Ancient Earth knowledge the pawn has acquired.");

            sb.AppendLine(
                "- Do not silently expand this knowledge into expert-level knowledge.");

            sb.AppendLine(
                "- Do not claim detailed Ancient Earth knowledge outside known topics.");

            sb.AppendLine(
                "- Low familiarity should result in uncertainty, partial recall, or questions.");

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