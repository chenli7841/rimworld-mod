using LiAIChat.Models;
using RimWorld;
using Verse;

namespace LiAIChat.Knowledge
{
    public static class KnowledgeInitializer
    {
        public static KnowledgeState CreateInitial(
            Pawn pawn)
        {
            KnowledgeState knowledge =
                new KnowledgeState();

            if (pawn == null)
                return knowledge;

            int intellectualSkill = 0;

            if (pawn.skills != null)
            {
                SkillRecord intellectual =
                    pawn.skills.GetSkill(
                        SkillDefOf.Intellectual);

                if (intellectual != null)
                {
                    intellectualSkill =
                        intellectual.Level;
                }
            }

            float intellectualFactor =
                intellectualSkill / 20f;

            knowledge.ScienceKnowledge =
                Clamp01(
                    0.15f +
                    intellectualFactor * 0.55f);

            knowledge.PhilosophyKnowledge =
                Clamp01(
                    0.10f +
                    intellectualFactor * 0.35f);

            knowledge.EarthHistoryKnowledge =
                Clamp01(
                    0.05f +
                    intellectualFactor * 0.25f);

            knowledge.ReligiousKnowledge =
                0.10f;

            knowledge.PoliticsKnowledge =
                Clamp01(
                    0.05f +
                    intellectualFactor * 0.20f);

            return knowledge;
        }

        private static float Clamp01(
            float value)
        {
            if (value < 0f)
                return 0f;

            if (value > 1f)
                return 1f;

            return value;
        }
    }
}