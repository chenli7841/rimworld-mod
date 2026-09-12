using LiAIChat.Models;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LiAIChat.Archive
{
    public static class ArchiveReflectionPromptBuilder
    {
        public static string Build(
    List<ArchiveReflection> reflections)
        {
            if (reflections == null ||
                reflections.Count == 0)
            {
                return "";
            }

            StringBuilder builder =
                new StringBuilder();

            builder.AppendLine(
                "RELEVANT ARCHIVE REFLECTIONS:");

            foreach (ArchiveReflection reflection
                in reflections)
            {
                if (reflection == null ||
                    string.IsNullOrWhiteSpace(
                        reflection.Text))
                {
                    continue;
                }

                builder.AppendLine(
                    "- " + reflection.Text);
            }

            return builder.ToString();
        }
    }
}