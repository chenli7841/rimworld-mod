using LiAIChat.Archive;
using RimWorld.QuestGen;
using Verse;

namespace LiAIChat.Questing
{
    public class QuestNode_GiveScholarArchive
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
            Pawn targetPawn =
                pawn.GetValue(
                    QuestGen.slate);

            if (targetPawn == null)
            {
                return;
            }

            bool success =
                ArchiveScholarGiftUtility
                    .TryGiveArchiveToColony(
                        targetPawn);

            Log.Message(
                "[Li AI Chat] Scholar archive gift: " +
                targetPawn.LabelShort +
                ", success=" +
                success);
        }
    }
}