using System.Collections.Generic;

namespace LiAIChat.Civilization
{
    public static class CivilizationKnowledgeStateManager
    {
        private static readonly Dictionary<
            string,
            CivilizationKnowledgeNodeState> states =
            new Dictionary<
                string,
                CivilizationKnowledgeNodeState>();

        public static CivilizationKnowledgeNodeState
            GetState(
                string nodeId)
        {
            CivilizationKnowledgeNodeState state;

            if (states.TryGetValue(
                    nodeId,
                    out state))
            {
                return state;
            }

            state =
                CreateDefaultState(
                    nodeId);

            states[nodeId] =
                state;

            return state;
        }

        private static CivilizationKnowledgeNodeState
            CreateDefaultState(
                string nodeId)
        {
            CivilizationKnowledgeStatus status =
                nodeId == "ancient_earth"
                    ? CivilizationKnowledgeStatus.Available
                    : CivilizationKnowledgeStatus.Locked;

            return new CivilizationKnowledgeNodeState(
                nodeId,
                status);
        }
    }
}