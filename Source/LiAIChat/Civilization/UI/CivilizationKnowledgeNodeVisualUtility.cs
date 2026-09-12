namespace LiAIChat.Civilization.UI
{
    public static class CivilizationKnowledgeNodeVisualUtility
    {
        public static CivilizationKnowledgeNodeVisualState
            GetVisualState(
                CivilizationKnowledgeDef def)
        {
            if (def == null)
            {
                return CivilizationKnowledgeNodeVisualState.Locked;
            }

            CivilizationKnowledgeState state =
                CivilizationKnowledgeManager
                    .GetState(def);

            if (state == null ||
                !state.Unlocked)
            {
                return CivilizationKnowledgeNodeVisualState.Locked;
            }

            if (state.Dormant)
            {
                return CivilizationKnowledgeNodeVisualState.Dormant;
            }

            if (state.AwaitingReactivation)
            {
                return CivilizationKnowledgeNodeVisualState
                    .AwaitingReactivation;
            }

            if (state.Unstable)
            {
                return CivilizationKnowledgeNodeVisualState.Unstable;
            }

            return CivilizationKnowledgeNodeVisualState.Active;
        }

        public static string GetStatusLabel(
            CivilizationKnowledgeNodeVisualState state)
        {
            switch (state)
            {
                case CivilizationKnowledgeNodeVisualState.Active:
                    return "ACTIVE";

                case CivilizationKnowledgeNodeVisualState.Unstable:
                    return "UNSTABLE";

                case CivilizationKnowledgeNodeVisualState.Dormant:
                    return "DORMANT";

                case CivilizationKnowledgeNodeVisualState
                    .AwaitingReactivation:
                    return "REACTIVATION REQUIRED";

                default:
                    return "LOCKED";
            }
        }
    }
}