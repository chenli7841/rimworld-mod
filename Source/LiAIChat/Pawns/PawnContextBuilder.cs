using LiAIChat.Models;
using RimWorld;
using Verse;

namespace LiAIChat.Pawns
{
    public static class PawnContextBuilder
    {
        public static PawnContext Build(Pawn pawn)
        {
            var context =
                new PawnContext();

            // -----------------------------------------
            // Name
            // -----------------------------------------

            context.Name =
                pawn.Name?.ToStringFull
                ?? pawn.LabelShort;

            // -----------------------------------------
            // Gender
            // -----------------------------------------

            context.Gender =
                pawn.gender.ToString();

            // -----------------------------------------
            // Age
            // -----------------------------------------

            context.Age =
                pawn.ageTracker.AgeBiologicalYears;

            // -----------------------------------------
            // Traits
            // -----------------------------------------

            if (pawn.story?.traits != null)
            {
                foreach (Trait trait
                         in pawn.story.traits.allTraits)
                {
                    context.Traits.Add(
                        trait.Label
                    );
                }
            }

            // -----------------------------------------
            // Childhood
            // -----------------------------------------

            if (pawn.story?.Childhood != null)
            {
                context.Childhood =
                    pawn.story.Childhood.title;
            }

            // -----------------------------------------
            // Adulthood
            // -----------------------------------------

            if (pawn.story?.Adulthood != null)
            {
                context.Adulthood =
                    pawn.story.Adulthood.title;
            }

            context.CurrentGameTick = Find.TickManager.TicksGame;

            return context;
        }
    }
}