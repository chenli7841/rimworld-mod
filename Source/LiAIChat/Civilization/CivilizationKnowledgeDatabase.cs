using System.Collections.Generic;

namespace LiAIChat.Civilization
{
    public static class CivilizationKnowledgeDatabase
    {
        public static readonly List<
            CivilizationKnowledgeNode> Nodes =
            new List<CivilizationKnowledgeNode>
            {
                new CivilizationKnowledgeNode("ancient_earth", "Ancient Earth\nFoundations", null, 0),
                new CivilizationKnowledgeNode("philosophy", "Philosophy", "ancient_earth", 1),
                new CivilizationKnowledgeNode("religion", "Religion", "ancient_earth", 1),
                new CivilizationKnowledgeNode("science", "Science", "ancient_earth", 1),
                new CivilizationKnowledgeNode("ethics", "Ethics", "philosophy", 2),
                new CivilizationKnowledgeNode("scientific_method", "Scientific Method", "science", 2)
            };
    }
}