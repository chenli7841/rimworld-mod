using LiAIChat.Models;
using Verse;

namespace LiAIChat.Dialogue
{
    /// <summary>
    /// 规定什么时候允许主动谈话
    /// </summary>
    public static class ProactiveDialogueRules
    {
        /// <summary>
        /// 180000 ticks ≈ 3 in-game day
        /// 同一个 Pawn 大约每三天最多主动找你一次（不会太烦）。
        /// </summary>
        public const int MinimumCooldownTicks =
            180000;

        public static bool ShouldTrigger(
            Pawn pawn,
            PawnAIState state)
        {
            if (pawn == null ||
                state == null)
            {
                return false;
            }

            if (state.HasPendingProactiveDialogue)
                return false;

            int currentTick =
                Find.TickManager.TicksGame;

            if (currentTick -
                state.LastProactiveDialogueTick <
                MinimumCooldownTicks)
            {
                return false;
            }

            // 当前有任何 LifeEvent，且未处理它，就会允许触发。
            if (state.LifeEvents != null)
            {
                foreach (PawnLifeEvent lifeEvent in state.LifeEvents)
                {
                    // 每个重要事件最多触发一次主动谈话。
                    if (lifeEvent != null && !lifeEvent.ProactiveDialogueUsed)
                    {
                        return true;
                    }
                }
            }
            if (state.LifeGoal != null && state.LifeGoal.IsActive && !state.LifeGoal.ProactiveDialogueUsed && state.LifeGoal.Commitment >= 0.50f)
            {
                return true;
            }

            if (state.Meaning != null)
            {
                if (state.Meaning.Purpose < 0.30f ||
                    state.Meaning.Hope < 0.30f ||
                    state.Meaning.Coherence < 0.30f)
                {
                    return true;
                }
            }

            return false;
        }
    }
}