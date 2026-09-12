using LiAIChat.Game;
using LiAIChat.Knowledge;
using LiAIChat.Models;
using Verse;

namespace LiAIChat.State
{
    public static class PawnAIStateManager
    {
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

            // 先找已经存在的 state
            foreach (PawnAIState state in component.PawnStates)
            {
                if (state.PawnId == pawnId)
                {
                    // 已存在：直接返回，千万不要在这里重新初始化 Knowledge
                    return state;
                }
            }

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