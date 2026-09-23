using RimWorld;
using Verse;

namespace LiAIChat.Travel
{
    public class ThoughtWorker_TravelWishMood : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn pawn)
        {
            if (pawn?.health?.hediffSet == null)
                return ThoughtState.Inactive;

            HediffDef benefit = DefDatabase<HediffDef>.GetNamedSilentFail("LiAIChat_TravelWish_Mood");
            return benefit != null && pawn.health.hediffSet.HasHediff(benefit)
                ? ThoughtState.ActiveDefault
                : ThoughtState.Inactive;
        }
    }
}
