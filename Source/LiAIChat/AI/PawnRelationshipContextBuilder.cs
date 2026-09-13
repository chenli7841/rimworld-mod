using System.Collections.Generic;
using RimWorld;
using Verse;

namespace LiAIChat
{
    public static class PawnRelationshipContextBuilder
    {
        public static string Build(Pawn pawn)
        {
            if (pawn == null ||
                pawn.relations == null)
            {
                return "No relationship information available.";
            }

            List<string> relationships =
                new List<string>();

            foreach (DirectPawnRelation relation
                in pawn.relations.DirectRelations)
            {
                if (relation == null ||
                    relation.def == null ||
                    relation.otherPawn == null)
                {
                    continue;
                }

                Pawn otherPawn =
                    relation.otherPawn;

                if (relation.def ==
                    PawnRelationDefOf.Lover)
                {
                    relationships.Add(
                        "Lover: " +
                        otherPawn.LabelShort);
                }
                else if (relation.def ==
                         PawnRelationDefOf.Fiance)
                {
                    relationships.Add(
                        "Fiance: " +
                        otherPawn.LabelShort);
                }
                else if (relation.def ==
                         PawnRelationDefOf.Spouse)
                {
                    relationships.Add(
                        "Spouse: " +
                        otherPawn.LabelShort);
                }
            }

            if (relationships.Count == 0)
            {
                return "No current romantic partner.";
            }

            return string.Join(
                "\n",
                relationships);
        }
    }
}