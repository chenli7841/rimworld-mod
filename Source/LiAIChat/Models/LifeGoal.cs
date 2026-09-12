using Verse;

namespace LiAIChat.Models
{
    public class LifeGoal : IExposable
    {
        public string GoalId = "";

        public string Title = "";

        public string Description = "";

        public string Reason = "";

        /// <summary>
        /// 这个 Pawn 对这个目标有多认真。
        /// 0.20 - “最近偶尔在想……”，0.45 - “这是我觉得值得做的事。”，0.70 - “我真的希望能做到。”，0.90 - “这已经成为我人生最重要的方向之一。”
        /// 目标以后可以：增强、减弱、被新的事件改变、完成、失败、被放弃
        /// </summary>
        public float Commitment = 0f;

        public bool IsActive = true;

        public int CreatedTick = 0;

        public bool ProactiveDialogueUsed = false;
        public LifeGoal()
        {
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref GoalId, "goalId", "");
            Scribe_Values.Look(ref Title, "title", "");
            Scribe_Values.Look(ref Description, "description", "");
            Scribe_Values.Look(ref Reason, "reason", "");
            Scribe_Values.Look(ref Commitment, "commitment", 0f);
            Scribe_Values.Look(ref IsActive, "isActive", true);
            Scribe_Values.Look(ref CreatedTick, "createdTick", 0);
            Scribe_Values.Look(ref ProactiveDialogueUsed, "proactiveDialogueUsed", false);
        }
    }
}