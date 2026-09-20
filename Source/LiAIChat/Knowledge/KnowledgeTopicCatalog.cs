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

            string displayName;

            if (names.TryGetValue(topicId, out displayName))
            {
                return displayName;
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

        public static string GetChineseDisplayName(string topicId)
        {
            if (string.IsNullOrWhiteSpace(topicId))
            {
                return "";
            }

            switch (topicId)
            {
                case KnowledgeTopicIds.AncientEarth: return "远古地球";
                case KnowledgeTopicIds.AncientGreece: return "古希腊";
                case KnowledgeTopicIds.AncientPoliticalThought: return "古典政治思想";
                case KnowledgeTopicIds.Aristotle: return "亚里士多德";
                case KnowledgeTopicIds.ArtificialIntelligence: return "人工智能";
                case KnowledgeTopicIds.Christianity: return "基督教";
                case KnowledgeTopicIds.ColdWar: return "冷战";
                case KnowledgeTopicIds.Computing: return "计算机科学";
                case KnowledgeTopicIds.EarlyChurch: return "早期教会";
                case KnowledgeTopicIds.Enlightenment: return "启蒙运动";
                case KnowledgeTopicIds.Evolution: return "演化论";
                case KnowledgeTopicIds.HebrewBible: return "希伯来圣经";
                case KnowledgeTopicIds.IndustrialRevolution: return "工业革命";
                case KnowledgeTopicIds.Jesus: return "耶稣";
                case KnowledgeTopicIds.Kant: return "康德";
                case KnowledgeTopicIds.Liberalism: return "自由主义";
                case KnowledgeTopicIds.ModernNationState: return "现代民族国家";
                case KnowledgeTopicIds.Plato: return "柏拉图";
                case KnowledgeTopicIds.QinHanChina: return "秦汉中国";
                case KnowledgeTopicIds.Reformation: return "宗教改革";
                case KnowledgeTopicIds.RepublicanGovernment: return "共和政体";
                case KnowledgeTopicIds.RomanEmpire: return "罗马帝国";
                case KnowledgeTopicIds.ScientificRevolution: return "科学革命";
                case KnowledgeTopicIds.Socialism: return "社会主义";
                case KnowledgeTopicIds.SpaceFlight: return "太空飞行";
                case KnowledgeTopicIds.Stoicism: return "斯多葛主义";
                case KnowledgeTopicIds.WorldWarII: return "第二次世界大战";
                case "earth.history": return "远古地球历史";
                case "earth.philosophy": return "哲学";
                case "earth.religion": return "宗教";
                case "earth.politics": return "政治思想";
                case "earth.science": return "科学";
                default: return GetDisplayName(topicId);
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
