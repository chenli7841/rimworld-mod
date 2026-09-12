using System.Collections.Generic;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace LiAIChat.Questing
{
    public class QuestNode_CreateArchiveRefugees : QuestNode
    {
        public string storeAs;

        public string storeFactionAs;

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
                    "[Li AI Chat] CreateArchiveRefugees: " +
                    "storeAs was empty.");

                return;
            }

            int refugeeCount =
                Rand.RangeInclusive(3, 5);

            List<Pawn> refugees =
                new List<Pawn>();

            // ---------------------------------------------------------
            // First refugee
            // ---------------------------------------------------------

            Pawn firstPawn =
                PawnGenerator.GeneratePawn(
                    PawnKindDefOf.SpaceRefugee,
                    null);

            if (firstPawn == null)
            {
                Log.Error(
                    "[Li AI Chat] CreateArchiveRefugees: " +
                    "failed to generate first refugee.");

                return;
            }

            firstPawn.relations.everSeenByPlayer = true;

            refugees.Add(firstPawn);

            Faction homeFaction =
                firstPawn.Faction;

            // ---------------------------------------------------------
            // Remaining refugees use the same faction
            // ---------------------------------------------------------

            for (int i = 1;
                 i < refugeeCount;
                 i++)
            {
                Pawn pawn =
                    PawnGenerator.GeneratePawn(
                        PawnKindDefOf.SpaceRefugee,
                        homeFaction);

                if (pawn == null)
                {
                    Log.Warning(
                        "[Li AI Chat] CreateArchiveRefugees: " +
                        "failed to generate refugee.");

                    continue;
                }

                pawn.relations.everSeenByPlayer = true;

                refugees.Add(pawn);
            }

            if (refugees.Count == 0)
            {
                Log.Error(
                    "[Li AI Chat] CreateArchiveRefugees: " +
                    "no refugees were generated.");

                return;
            }

            slate.Set(
                storeAs,
                refugees);

            if (homeFaction != null &&
                !string.IsNullOrEmpty(storeFactionAs))
            {
                slate.Set(
                    storeFactionAs,
                    homeFaction);
            }

            Log.Message(
                "[Li AI Chat] Created Archive refugee group: " +
                refugees.Count +
                " pawns. HomeFaction=" +
                (homeFaction != null
                    ? homeFaction.Name
                    : "null"));
        }
    }
}