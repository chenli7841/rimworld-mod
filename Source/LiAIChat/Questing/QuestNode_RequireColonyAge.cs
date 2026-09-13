using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace LiAIChat.Questing
{
    public class QuestNode_RequireColonyAge : QuestNode
    {
        public SlateRef<int> minDays;

        protected override bool TestRunInt(Slate slate)
        {
            if (Find.TickManager == null)
                return false;

            int currentTick = Find.TickManager.TicksGame;
            int requiredTicks = minDays.GetValue(slate) * GenDate.TicksPerDay;

            return currentTick >= requiredTicks;
        }

        protected override void RunInt()
        {
            // Generation gate only.
            // No runtime QuestPart is required.
        }
    }
}