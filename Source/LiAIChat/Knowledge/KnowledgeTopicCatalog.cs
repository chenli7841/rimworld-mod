using System.Collections.Generic;

namespace LiAIChat.Knowledge
{
    public static class KnowledgeTopicCatalog
    {
        private static readonly Dictionary<string, string>
            names =
                new Dictionary<string, string>
                {
                    {
                        KnowledgeTopicIds.AncientEarth,
                        "Ancient Earth"
                    },
                    {
                        KnowledgeTopicIds.AncientGreece,
                        "Ancient Greece"
                    },
                    {
                        KnowledgeTopicIds.AncientPoliticalThought,
                        "Ancient Political Thought"
                    },
                    {
                        KnowledgeTopicIds.Aristotle,
                        "Aristotle"
                    },
                    {
                        KnowledgeTopicIds.ArtificialIntelligence,
                        "Artificial Intelligence"
                    },
                    {
                        KnowledgeTopicIds.Christianity,
                        "Christianity"
                    },
                    {
                        KnowledgeTopicIds.Computing,
                        "Computing"
                    },
                    {
                        KnowledgeTopicIds.ColdWar,
                        "Cold War"
                    },
                    {
                        KnowledgeTopicIds.EarlyChurch,
                        "Early Church"
                    },
                    {
                        KnowledgeTopicIds.Enlightenment,
                        "Enlightenment"
                    },
                    {
                        KnowledgeTopicIds.Evolution,
                        "Evolution"
                    },
                    {
                        KnowledgeTopicIds.HebrewBible,
                        "Hebrew Bible"
                    },
                    {
                        KnowledgeTopicIds.IndustrialRevolution,
                        "Industrial Revolution"
                    },
                    {
                        KnowledgeTopicIds.Jesus,
                        "Jesus"
                    },
                    {
                        KnowledgeTopicIds.Kant,
                        "Immanuel Kant"
                    },
                    {
                        KnowledgeTopicIds.Liberalism,
                        "Liberalism"
                    },
                    {
                        KnowledgeTopicIds.ModernNationState,
                        "Modern Nation State"
                    },
                    {
                        KnowledgeTopicIds.Plato,
                        "Plato"
                    },
                    {
                        KnowledgeTopicIds.QinHanChina,
                        "Qin Han China"
                    },
                    {
                        KnowledgeTopicIds.Reformation,
                        "Reformation"
                    },
                    {
                        KnowledgeTopicIds.RepublicanGovernment,
                        "Republican Government"
                    },
                    {
                        KnowledgeTopicIds.RomanEmpire,
                        "Roman Empire"
                    },
                    {
                        KnowledgeTopicIds.ScientificRevolution,
                        "Scientific Revolution"
                    },
                    {
                        KnowledgeTopicIds.Stoicism,
                        "Stoicism"
                    },
                    {
                        KnowledgeTopicIds.Socialism,
                        "Socialism"
                    },
                    {
                        KnowledgeTopicIds.SpaceFlight,
                        "Space Flight"
                    },
                    {
                        KnowledgeTopicIds.WorldWarII,
                        "World War II"
                    },
                };
        public static string GetDisplayName(
    string topicId)
        {
            if (string.IsNullOrWhiteSpace(topicId))
            {
                return "";
            }

            switch (topicId)
            {
                case "earth.history":
                    return "Ancient Earth History";

                case "earth.philosophy":
                    return "Philosophy";

                case "earth.religion":
                    return "Religion";

                case "earth.religion.christianity":
                    return "Christianity";

                case "earth.politics":
                    return "Politics";

                case "earth.science":
                    return "Science";

                default:
                    return topicId;
            }
        }

        public static bool Contains(string topicId)
        {
            if (string.IsNullOrWhiteSpace(topicId))
                return false;

            return names.ContainsKey(topicId);
        }

        public static IEnumerable<string> GetAllTopicIds()
        {
            return names.Keys;
        }
    }
}