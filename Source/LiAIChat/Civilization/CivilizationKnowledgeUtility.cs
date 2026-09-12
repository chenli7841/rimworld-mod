using System.Collections.Generic;
using LiAIChat.Archive;

namespace LiAIChat.Civilization
{
    public static class CivilizationKnowledgeUtility
    {
        public static bool HasRequiredTexts(
            CivilizationKnowledgeDef knowledgeDef)
        {
            if (knowledgeDef == null)
            {
                return false;
            }

            return ColonyLibrary.HasAllTexts(
                knowledgeDef.requiredTexts);
        }

        public static List<EarthTextDef>
            GetMissingRequiredTexts(
                CivilizationKnowledgeDef knowledgeDef)
        {
            if (knowledgeDef == null)
            {
                return new List<EarthTextDef>();
            }

            return ColonyLibrary.GetMissingTexts(
                knowledgeDef.requiredTexts);
        }
        public static bool IsMissingRequiredTexts(
    CivilizationKnowledgeDef knowledgeDef)
        {
            if (knowledgeDef == null)
            {
                return false;
            }

            return !HasRequiredTexts(
                knowledgeDef);
        }
        public static bool IsUnlockedButIncomplete(
    CivilizationKnowledgeDef knowledgeDef)
        {
            if (knowledgeDef == null)
            {
                return false;
            }

            if (!CivilizationKnowledgeManager
                .IsUnlocked(knowledgeDef))
            {
                return false;
            }

            return IsMissingRequiredTexts(
                knowledgeDef);
        }
    }
}