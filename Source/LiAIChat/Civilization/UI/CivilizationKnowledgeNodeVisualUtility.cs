using LiAIChat.Archive;

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
        public static int GetSatisfiedPrerequisiteCount(
    CivilizationKnowledgeDef def)
        {
            if (def == null ||
                def.prerequisites == null)
            {
                return 0;
            }

            int count = 0;

            foreach (CivilizationKnowledgeDef prerequisite
                in def.prerequisites)
            {
                if (prerequisite == null)
                {
                    continue;
                }

                if (CivilizationKnowledgeManager
                    .IsUnlocked(prerequisite))
                {
                    count++;
                }
            }

            return count;
        }
        public static int GetPrerequisiteCount(
    CivilizationKnowledgeDef def)
        {
            if (def == null ||
                def.prerequisites == null)
            {
                return 0;
            }

            int count = 0;

            foreach (CivilizationKnowledgeDef prerequisite
                in def.prerequisites)
            {
                if (prerequisite != null)
                {
                    count++;
                }
            }

            return count;
        }
        public static int GetAvailableRequiredTextCount(
    CivilizationKnowledgeDef def)
        {
            if (def == null ||
                def.requiredTexts == null)
            {
                return 0;
            }

            int count = 0;

            foreach (EarthTextDef text
                in def.requiredTexts)
            {
                if (text == null)
                {
                    continue;
                }

                if (ColonyLibrary.HasText(text))
                {
                    count++;
                }
            }

            return count;
        }
        public static int GetRequiredTextCount(
    CivilizationKnowledgeDef def)
        {
            if (def == null ||
                def.requiredTexts == null)
            {
                return 0;
            }

            int count = 0;

            foreach (EarthTextDef text
                in def.requiredTexts)
            {
                if (text != null)
                {
                    count++;
                }
            }

            return count;
        }
        public static string GetRequirementSummary(
    CivilizationKnowledgeDef def)
        {
            if (def == null)
            {
                return string.Empty;
            }

            int prerequisiteCount =
                GetPrerequisiteCount(def);

            int satisfiedPrerequisiteCount =
                GetSatisfiedPrerequisiteCount(def);

            int requiredTextCount =
                GetRequiredTextCount(def);

            int availableRequiredTextCount =
                GetAvailableRequiredTextCount(def);

            if (prerequisiteCount > 0 &&
                requiredTextCount > 0)
            {
                return
                    "Prereq "
                    + satisfiedPrerequisiteCount
                    + "/"
                    + prerequisiteCount
                    + "   Texts "
                    + availableRequiredTextCount
                    + "/"
                    + requiredTextCount;
            }

            if (prerequisiteCount > 0)
            {
                return
                    "Prereq "
                    + satisfiedPrerequisiteCount
                    + "/"
                    + prerequisiteCount;
            }

            if (requiredTextCount > 0)
            {
                return
                    "Texts "
                    + availableRequiredTextCount
                    + "/"
                    + requiredTextCount;
            }

            return string.Empty;
        }
    }
}