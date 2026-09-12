using LiAIChat.Archive;
using LiAIChat.Models;
using LiAIChat.State;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace LiAIChat.Questing
{
    public class QuestNode_ResolveArchiveScholarGift
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
                    "[Li AI Chat] ResolveArchiveScholarGift: pawn was null.");

                return;
            }

            PawnAIState state =
                PawnAIStateManager.GetState(
                    targetPawn);

            if (state == null ||
                state.ScholarStay == null)
            {
                Log.Warning(
                    "[Li AI Chat] ResolveArchiveScholarGift: " +
                    "ScholarStay was missing for " +
                    targetPawn.LabelShort);

                return;
            }

            if (!state.ScholarStay.Completed)
            {
                Log.Warning(
                    "[Li AI Chat] ResolveArchiveScholarGift: " +
                    "stay was not completed for " +
                    targetPawn.LabelShort);

                return;
            }

            bool willGift =
                ArchiveScholarGiftDecisionUtility
                    .Decide(state);

            if (willGift)
            {
                bool gifted =
                    ArchiveScholarGiftUtility
                        .TryGiveArchiveToColony(
                            targetPawn);

                if (!gifted)
                {
                    Log.Warning(
                        "[Li AI Chat] Scholar intended to gift Archive, " +
                        "but physical transfer failed: " +
                        targetPawn.LabelShort);
                }
                return;
            }

            Messages.Message(
                targetPawn.LabelShort +
                " 决定继续随身保存这份古代地球档案，" +
                "并在离开后继续自己的旅程。",
                targetPawn,
                MessageTypeDefOf.NeutralEvent);

            Log.Message(
                "[Li AI Chat] Scholar gift resolved: " +
                targetPawn.LabelShort +
                ", decision=keep");
        }
    }
}