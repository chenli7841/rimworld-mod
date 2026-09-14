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

        public static void EvaluateAllStatuses()
        {
            foreach (CivilizationKnowledgeNode node
                     in CivilizationKnowledgeDatabase.Nodes)
            {
                EvaluateNodeAvailability(
                    node);
            }
        }

        private static void EvaluateNodeAvailability(
    CivilizationKnowledgeNode node)
        {
            if (node == null)
            {
                return;
            }

            CivilizationKnowledgeNodeState state =
                GetState(
                    node.Id);

            if (state.Status ==
                    CivilizationKnowledgeStatus.InProgress ||
                state.Status ==
                    CivilizationKnowledgeStatus.Recovered)
            {
                return;
            }

            if (node.ParentId == null)
            {
                state.Status =
                    CivilizationKnowledgeStatus.Available;

                return;
            }

            CivilizationKnowledgeNodeState parentState =
                GetState(
                    node.ParentId);

            if (parentState.Status ==
                CivilizationKnowledgeStatus.Recovered)
            {
                state.Status =
                    CivilizationKnowledgeStatus.Available;
            }
            else
            {
                state.Status =
                    CivilizationKnowledgeStatus.Locked;
            }
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