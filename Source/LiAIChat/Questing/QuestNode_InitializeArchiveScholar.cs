using LiAIChat.Archive;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace LiAIChat.Questing
{
    public class QuestNode_InitializeArchiveScholar
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
                Log.Warning(
                    "[Li AI Chat] Scholar Quest: " +
                    "pawn was null.");

                return;
            }

            ArchiveScholarInitializer
                .Initialize(targetPawn);

            Log.Message(
                "[Li AI Chat] Quest pawn initialized " +
                "as Archive Scholar: " +
                targetPawn.LabelShort);

        }
    }
}