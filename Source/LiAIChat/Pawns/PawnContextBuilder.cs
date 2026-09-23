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

            context.PawnId = pawn.thingIDNumber;

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

            // -----------------------------------------
            // Skills and learning passions
            // -----------------------------------------

            if (pawn.skills?.skills != null)
            {
                foreach (SkillRecord skill in pawn.skills.skills)
                {
                    if (skill == null || skill.def == null ||
                        skill.TotallyDisabled)
                    {
                        continue;
                    }

                    context.Skills.Add(
                        new PawnSkillContext
                        {
                            Name = skill.def.LabelCap,
                            Level = skill.Level,
                            Passion = GetPassionLabel(skill.passion)
                        });
                }
            }

            context.CurrentGameTick = Find.TickManager.TicksGame;

            return context;
        }

        private static string GetPassionLabel(Passion passion)
        {
            switch (passion)
            {
                case Passion.Major:
                    return "major";

                case Passion.Minor:
                    return "minor";

                default:
                    return "none";
            }
        }
    }
}
