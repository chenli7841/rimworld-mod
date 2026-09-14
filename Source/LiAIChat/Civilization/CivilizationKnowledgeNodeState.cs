using Verse;

namespace LiAIChat.Civilization
{
    public class CivilizationKnowledgeNodeState
        : IExposable
    {
        public string NodeId;

        public CivilizationKnowledgeStatus Status;

        public CivilizationKnowledgeNodeState()
        {
        }

        public CivilizationKnowledgeNodeState(
            string nodeId,
            CivilizationKnowledgeStatus status)
        {
            NodeId = nodeId;
            Status = status;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(
                ref NodeId,
                "nodeId");

            Scribe_Values.Look(
                ref Status,
                "status",
                CivilizationKnowledgeStatus.Locked);
        }
    }
}