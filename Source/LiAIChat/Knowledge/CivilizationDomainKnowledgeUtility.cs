using System.Collections.Generic;
using LiAIChat.Archive;
using LiAIChat.Models;
using Verse;

namespace LiAIChat.Knowledge
{
    /// <summary>
    /// Keeps the broad five-domain knowledge display in step with the detailed
    /// text/topic system. A fully understood individual text contributes 10%
    /// to its matching domain, applied gradually as the book is read.
    /// </summary>
    public static class CivilizationDomainKnowledgeUtility
    {
        private const float FullTextDomainGain = 0.10f;

        public static void ReconcileTextContribution(PawnAIState state, string textId, string topicId)
        {
            if (state == null || state.Knowledge == null || string.IsNullOrWhiteSpace(textId))
                return;

            float familiarity = state.GetEarthTextFamiliarity(textId);
            if (familiarity <= 0f)
                return;

            if (state.EarthTextDomainContribution == null)
                state.EarthTextDomainContribution = new Dictionary<string, float>();

            float credited;
            state.EarthTextDomainContribution.TryGetValue(textId, out credited);
            float delta = familiarity - credited;
            if (delta <= 0f)
                return;

            ApplyDomainGain(state.Knowledge, topicId, delta * FullTextDomainGain);
            state.EarthTextDomainContribution[textId] = familiarity;
        }

        // Handles books completed before this feature existed. It is cheap and
        // idempotent because each text records how much has already been added.
        public static void ReconcileAllKnownTexts(PawnAIState state)
        {
            if (state == null || state.Knowledge == null)
                return;

            foreach (EarthTextDef text in DefDatabase<EarthTextDef>.AllDefsListForReading)
            {
                if (text == null)
                    continue;
                ReconcileTextContribution(state, text.defName, text.primaryTopicId);
            }
        }

        private static void ApplyDomainGain(KnowledgeState knowledge, string topicId, float gain)
        {
            if (knowledge == null || gain <= 0f || string.IsNullOrWhiteSpace(topicId))
                return;

            if (topicId.StartsWith("earth.history"))
                knowledge.EarthHistoryKnowledge = Clamp01(knowledge.EarthHistoryKnowledge + gain);
            else if (topicId.StartsWith("earth.philosophy"))
                knowledge.PhilosophyKnowledge = Clamp01(knowledge.PhilosophyKnowledge + gain);
            else if (topicId.StartsWith("earth.religion") || topicId.StartsWith("earth.christianity"))
                knowledge.ReligiousKnowledge = Clamp01(knowledge.ReligiousKnowledge + gain);
            else if (topicId.StartsWith("earth.politics"))
                knowledge.PoliticsKnowledge = Clamp01(knowledge.PoliticsKnowledge + gain);
            else if (topicId.StartsWith("earth.science"))
                knowledge.ScienceKnowledge = Clamp01(knowledge.ScienceKnowledge + gain);
        }

        private static float Clamp01(float value)
        {
            if (value < 0f) return 0f;
            return value > 1f ? 1f : value;
        }
    }
}
