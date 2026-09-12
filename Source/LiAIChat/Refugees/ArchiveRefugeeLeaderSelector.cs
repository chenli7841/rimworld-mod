using System.Collections.Generic;
using RimWorld;
using Verse;

namespace LiAIChat.Refugees
{
    public static class ArchiveRefugeeLeaderSelector
    {
        public static Pawn SelectLeader(
            IList<Pawn> pawns)
        {
            if (pawns == null ||
                pawns.Count == 0)
            {
                return null;
            }

            Pawn bestPawn = null;
            float bestScore = float.MinValue;

            for (int i = 0;
                 i < pawns.Count;
                 i++)
            {
                Pawn pawn = pawns[i];

                if (pawn == null)
                {
                    continue;
                }

                float score =
                    CalculateLeaderScore(pawn);

                Log.Message(
                    "[Li AI Chat] Refugee leader candidate: " +
                    pawn.LabelShort +
                    ", score=" +
                    score.ToString("0.00"));

                if (bestPawn == null ||
                    score > bestScore)
                {
                    bestPawn = pawn;
                    bestScore = score;
                }
            }

            return bestPawn;
        }


        private static float CalculateLeaderScore(
            Pawn pawn)
        {
            if (pawn == null)
            {
                return float.MinValue;
            }

            float score = 0f;


            // -------------------------------------------------
            // Social
            // Most important factor.
            // -------------------------------------------------

            if (pawn.skills != null)
            {
                SkillRecord social =
                    pawn.skills.GetSkill(
                        SkillDefOf.Social);

                if (social != null)
                {
                    score +=
                        social.Level * 2.0f;
                }


                // ---------------------------------------------
                // Intellectual
                // Useful for representing a group carrying
                // ancient knowledge, but less important
                // than Social.
                // ---------------------------------------------

                SkillRecord intellectual =
                    pawn.skills.GetSkill(
                        SkillDefOf.Intellectual);

                if (intellectual != null)
                {
                    score +=
                        intellectual.Level * 0.75f;
                }
            }


            // -------------------------------------------------
            // Age / maturity
            // Small bonus only.
            // -------------------------------------------------

            if (pawn.ageTracker != null)
            {
                int age =
                    pawn.ageTracker
                        .AgeBiologicalYears;

                if (age >= 30)
                {
                    score += 3f;
                }
                else if (age >= 20)
                {
                    score += 1f;
                }
                else
                {
                    score -= 4f;
                }

                if (age >= 60)
                {
                    score += 1f;
                }
            }


            // Small random variation so identical candidates
            // do not always resolve by list order.
            score +=
                Rand.Range(0f, 1f);


            return score;
        }
    }
}