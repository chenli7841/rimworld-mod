using System.Collections.Generic;

namespace LiAIChat.Civilization
{
    public static class CivilizationKnowledgeDatabase
    {
        public static readonly List<
            CivilizationKnowledgeNode> Nodes =
            new List<CivilizationKnowledgeNode>
            {
                new CivilizationKnowledgeNode(
                    "ancient_earth",
                    "Ancient Earth\nFoundations",
                    null,
                    0,
                    "Recovered foundational knowledge from Ancient Earth civilization."),

                new CivilizationKnowledgeNode(
                    "philosophy",
                    "Philosophy",
                    "ancient_earth",
                    1,
                    "Study of reality, knowledge, ethics, reason, and human existence."),

                new CivilizationKnowledgeNode(
                    "religion",
                    "Religion",
                    "ancient_earth",
                    1,
                    "Recovered knowledge concerning religious traditions, beliefs, and practices."),

                new CivilizationKnowledgeNode(
                    "science",
                    "Science",
                    "ancient_earth",
                    1,
                    "Systematic investigation of the natural world through observation and reasoning."),

                new CivilizationKnowledgeNode(
                    "ethics",
                    "Ethics",
                    "philosophy",
                    2,
                    "Study of moral reasoning, virtue, duty, justice, and the good life."),

                new CivilizationKnowledgeNode(
                    "scientific_method",
                    "Scientific Method",
                    "science",
                    2,
                    "Methods for forming hypotheses, testing evidence, and building reliable knowledge.")
            };
    }
}