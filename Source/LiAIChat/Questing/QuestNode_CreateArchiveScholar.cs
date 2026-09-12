using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace LiAIChat.Questing
{
    public class QuestNode_CreateArchiveScholar : QuestNode
    {
        public string storeAs;

        protected override bool TestRunInt(Slate slate)
        {
            return !string.IsNullOrEmpty(storeAs);
        }

        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;

            if (string.IsNullOrEmpty(storeAs))
            {
                Log.Warning(
                    "[Li AI Chat] CreateArchiveScholar: " +
                    "storeAs was empty.");

                return;
            }

            Pawn pawn =
                PawnGenerator.GeneratePawn(
                    PawnKindDefOf.SpaceRefugee,
                    null);

            if (pawn == null)
            {
                Log.Error(
                    "[Li AI Chat] CreateArchiveScholar: " +
                    "PawnGenerator returned null.");

                return;
            }

            pawn.relations.everSeenByPlayer = true;

            slate.Set(
                storeAs,
                pawn);

            if (pawn.Faction != null)
            {
                slate.Set(
                    storeAs + "Faction",
                    pawn.Faction);
            }
            Log.Message(
                "[Li AI Chat] Created Archive Scholar candidate: " +
                pawn.LabelShort +
                ", PawnKind=" +
                pawn.kindDef.defName +
                ", Faction=" +
                (pawn.Faction != null
                    ? pawn.Faction.Name
                    : "None") +
                ", SlateKey=" +
                storeAs);
        }
    }
}