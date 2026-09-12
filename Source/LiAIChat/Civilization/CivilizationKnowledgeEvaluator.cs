using RimWorld;
using System.Collections.Generic;
using Verse;

namespace LiAIChat.Civilization
{
    public static class CivilizationKnowledgeEvaluator
    {
        public static void NotifyLibraryMayHaveChanged()
        {
            EvaluateAll();
        }

        public static void EvaluateAll()
        {
            List<CivilizationKnowledgeDef> defs =
                DefDatabase<CivilizationKnowledgeDef>
                    .AllDefsListForReading;

            if (defs == null)
            {
                return;
            }

            foreach (CivilizationKnowledgeDef def
                in defs)
            {
                Evaluate(def);
            }
        }

        public static bool Evaluate(
            CivilizationKnowledgeDef knowledgeDef)
        {
            if (knowledgeDef == null)
            {
                return false;
            }

            if (CivilizationKnowledgeManager
                .IsUnlocked(knowledgeDef))
            {
                return false;
            }

            if (!CivilizationKnowledgeUtility
                .HasRequiredTexts(knowledgeDef))
            {
                return false;
            }

            bool unlocked =
                CivilizationKnowledgeManager
                    .Unlock(knowledgeDef);

            if (!unlocked)
            {
                return false;
            }

            Messages.Message(
                "Civilization knowledge reconstructed: "
                + knowledgeDef.label,
                MessageTypeDefOf.PositiveEvent);

            return true;
        }
    }
}