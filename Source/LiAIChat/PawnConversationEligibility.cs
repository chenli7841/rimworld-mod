using LiAIChat.Models;
using LiAIChat.State;
using Verse;

namespace LiAIChat
{
    public static class PawnConversationEligibility
    {
        public static bool CanTalkToPlayer(
    Pawn pawn)
        {
            if (pawn == null)
            {
                return false;
            }

            if (pawn.Dead ||
                pawn.Downed)
            {
                return false;
            }

            if (pawn.RaceProps == null ||
                !pawn.RaceProps.Humanlike)
            {
                return false;
            }

            if (pawn.IsColonist)
            {
                return true;
            }

            PawnAIState state =
                PawnAIStateManager.GetState(
                    pawn);

            if (state == null)
            {
                return false;
            }

            return state.AllowsPlayerConversation;
        }
    }
}