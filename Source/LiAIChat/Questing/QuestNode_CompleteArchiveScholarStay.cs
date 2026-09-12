using LiAIChat.Archive;
using RimWorld.QuestGen;
using Verse;

namespace LiAIChat.Questing
{
    public class QuestNode_CompleteArchiveScholarStay
        : QuestNode
    {
        public SlateRef<Pawn> pawn;

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
                return;
            }

            bool completed =
    ArchiveScholarStayUtility
        .CompleteStayFromQuest(
            targetPawn);

            Log.Message(
                "[Li AI Chat] Scholar stay completion attempted. " +
                "Pawn=" +
                targetPawn.LabelShort +
                ", completed=" +
                completed);
        }
    }
}