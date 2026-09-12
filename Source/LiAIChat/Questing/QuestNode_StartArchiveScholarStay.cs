using LiAIChat.Archive;
using RimWorld.QuestGen;
using Verse;

namespace LiAIChat.Questing
{
    public class QuestNode_StartArchiveScholarStay
        : QuestNode
    {
        public SlateRef<Pawn> pawn;
        public SlateRef<int> durationTicks;
        public SlateRef<string> deathSignal;
        public SlateRef<string> exitSignal;

        protected override bool TestRunInt(
            Slate slate)
        {
            Pawn targetPawn =
                pawn.GetValue(slate);

            return targetPawn != null;
        }

        protected override void RunInt()
        {
            Slate slate =
                QuestGen.slate;

            Pawn targetPawn =
                pawn.GetValue(slate);

            if (targetPawn == null)
            {
                Log.Warning(
                    "[Li AI Chat] StartArchiveScholarStay: pawn was null.");

                return;
            }

            int duration =
                durationTicks.GetValue(slate);

            string actualDeathSignal =
                deathSignal.GetValue(slate);

            string actualExitSignal =
                exitSignal.GetValue(slate);

            ArchiveScholarStayUtility.StartStay(
                targetPawn,
                duration,
                actualDeathSignal,
                actualExitSignal);

            Log.Message(
                "[Li AI Chat] Archive Scholar stay started: " +
                targetPawn.LabelShort +
                ", durationTicks=" +
                duration +
                ", deathSignal=" +
                actualDeathSignal +
                ", exitSignal=" +
                actualExitSignal);
        }
    }
}