using System;
using LiAIChat.Models;
using Verse;

namespace LiAIChat.Goals
{
    public static class LifeGoalUpdater
    {
        public static bool TryCreateGoal(
            PawnAIState state,
            LifeGoalProposal proposal)
        {
            if (state == null ||
                proposal == null)
            {
                return false;
            }

            if (state.LifeGoal != null)
                return false;

            if (!proposal.ShouldCreateGoal)
                return false;

            if (string.IsNullOrWhiteSpace(
                proposal.Title))
            {
                return false;
            }

            // 为什么要再检查 Commitment >= 0.20
            // 因为 AI 可能返回：ShouldCreateGoal | true，Commitment | 0.05
            // 这其实说明：连 AI 自己都不觉得角色真正形成目标。那我们干脆不创建。
            if (proposal.Commitment < 0.20f)
                return false;

            LifeGoal goal =
                new LifeGoal
                {
                    GoalId =
                        Guid.NewGuid().ToString("N"),

                    Title =
                        proposal.Title,

                    Description =
                        proposal.Description,

                    Reason =
                        proposal.Reason,

                    Commitment =
                        Clamp01(
                            proposal.Commitment),

                    IsActive =
                        true,

                    CreatedTick =
                        Find.TickManager.TicksGame
                };

            state.LifeGoal =
                goal;

            return true;
        }

        private static float Clamp01(
            float value)
        {
            if (value < 0f)
                return 0f;

            if (value > 1f)
                return 1f;

            return value;
        }
    }
}