using LiAIChat.Models;
using LiAIChat.State;
using Verse;

namespace LiAIChat.Archive
{
    public static class ArchiveScholarStayUtility
    {
        public static void StartStay(
    Pawn pawn,
    int durationTicks,
    string deathSignal,
    string exitSignal)
        {
            if (pawn == null)
            {
                return;
            }

            PawnAIState state =
                PawnAIStateManager.GetState(pawn);

            if (state == null)
            {
                return;
            }

            if (state.ScholarStay == null)
            {
                state.ScholarStay =
                    new ArchiveScholarStayState();
            }

            ArchiveScholarStayState stay =
                state.ScholarStay;

            stay.Active = true;
            stay.Completed = false;

            stay.StartTick =
                Find.TickManager.TicksGame;

            stay.EndTick =
                stay.StartTick + durationTicks;

            stay.DeathSignal =
                deathSignal;

            stay.ExitSignal =
                exitSignal;
        }

        public static bool IsStayComplete(
    Pawn pawn)
        {
            if (pawn == null ||
                pawn.Dead)
            {
                return false;
            }

            PawnAIState state =
                PawnAIStateManager.GetState(pawn);

            if (state == null ||
                state.ScholarStay == null)
            {
                return false;
            }

            ArchiveScholarStayState stay =
                state.ScholarStay;

            if (!stay.Active)
            {
                return stay.Completed;
            }

            return Find.TickManager.TicksGame >=
                   stay.EndTick;
        }

        public static bool TryCompleteStay(
    Pawn pawn)
        {
            if (!IsStayComplete(pawn))
            {
                return false;
            }

            PawnAIState state =
                PawnAIStateManager.GetState(pawn);

            if (state == null ||
                state.ScholarStay == null)
            {
                return false;
            }

            ArchiveScholarStayState stay =
                state.ScholarStay;

            if (stay.Completed)
            {
                return false;
            }

            stay.Active = false;
            stay.Completed = true;

            Log.Message(
                "[Li AI Chat] Scholar stay completed: " +
                pawn.LabelShort);

            return true;
        }
        public static bool CompleteStayFromQuest(
    Pawn pawn)
        {
            if (pawn == null ||
                pawn.Dead)
            {
                return false;
            }

            PawnAIState state =
                PawnAIStateManager.GetState(
                    pawn);

            if (state == null ||
                state.ScholarStay == null)
            {
                return false;
            }

            ArchiveScholarStayState stay =
                state.ScholarStay;

            if (stay.Completed)
            {
                return false;
            }

            stay.Active = false;
            stay.Completed = true;

            Log.Message(
                "[Li AI Chat] Scholar stay completed by quest: " +
                pawn.LabelShort);

            return true;
        }
    }
}