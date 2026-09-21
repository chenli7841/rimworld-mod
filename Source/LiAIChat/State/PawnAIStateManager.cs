using LiAIChat.Game;
using LiAIChat.Knowledge;
using LiAIChat.Models;
using Verse;

namespace LiAIChat.State
{
    public static class PawnAIStateManager
    {
        // Polling and UI must not create a complete knowledge record merely
        // because they encountered an unrelated pawn.
        public static PawnAIState TryGetExistingState(Pawn pawn)
        {
            if (pawn == null || Current.Game == null)
                return null;

            LiAIChatGameComponent component = Current.Game.GetComponent<LiAIChatGameComponent>();
            if (component == null || component.PawnStates == null)
                return null;

            int pawnId = pawn.thingIDNumber;
            foreach (PawnAIState state in component.PawnStates)
                if (state != null && state.PawnId == pawnId)
                    return state;
            return null;
        }

        public static PawnAIState GetState(
            Pawn pawn)
        {
            if (pawn == null)
            {
                return null;
            }

            if (Current.Game == null)
            {
                Log.Error(
                    "[Li AI Chat] Current.Game is null.");

                return null;
            }

            LiAIChatGameComponent component =
                Current.Game
                    .GetComponent<LiAIChatGameComponent>();

            if (component == null)
            {
                Log.Error(
                    "[Li AI Chat] " +
                    "LiAIChatGameComponent not found.");

                return null;
            }

            int pawnId =
                pawn.thingIDNumber;

            PawnAIState existingState = TryGetExistingState(pawn);
            if (existingState != null)
                return existingState;

            // 只有第一次没有找到时才创建
            PawnAIState newState =
                new PawnAIState(pawnId);

            // Knowledge 只在这里初始化一次
            newState.Knowledge = KnowledgeInitializer.CreateInitial(pawn);

            component.PawnStates.Add(
                newState);

            Log.Message(
                "[Li AI Chat] Created AI state for " +
                pawn.LabelShort +
                " (" +
                pawnId +
                ")");

            Log.Message(
                "[Li AI Chat] Initial knowledge for " +
                pawn.LabelShort +
                ": Earth=" +
                newState.Knowledge.EarthHistoryKnowledge.ToString("0.00") +
                ", Philosophy=" +
                newState.Knowledge.PhilosophyKnowledge.ToString("0.00") +
                ", Religion=" +
                newState.Knowledge.ReligiousKnowledge.ToString("0.00") +
                ", Politics=" +
                newState.Knowledge.PoliticsKnowledge.ToString("0.00") +
                ", Science=" +
                newState.Knowledge.ScienceKnowledge.ToString("0.00"));

            return newState;
        }
    }
}
