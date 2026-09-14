namespace LiAIChat.Civilization
{
    public class CivilizationKnowledgeNode
    {
        public string Id;
        public string Label;

        public string ParentId;

        public int Tier;

        public CivilizationKnowledgeNode(
            string id,
            string label,
            string parentId,
            int tier)
        {
            Id = id;
            Label = label;
            ParentId = parentId;
            Tier = tier;
        }
    }
}