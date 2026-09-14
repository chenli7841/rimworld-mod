namespace LiAIChat.Civilization
{
    public class CivilizationKnowledgeNode
    {
        public string Id;
        public string Label;

        public string ParentId;

        public int Tier;

        public string Description;

        public CivilizationKnowledgeNode(
            string id,
            string label,
            string parentId,
            int tier,
            string description)
        {
            Id = id;
            Label = label;
            ParentId = parentId;
            Tier = tier;
            Description = description;
        }
    }
}