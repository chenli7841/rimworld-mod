using RimWorld;
using Verse;

namespace LiAIChat.Archive
{
    public static class ArchiveScholarPawnFactory
    {
        public static Pawn Create(
            PawnKindDef pawnKind,
            Faction faction)
        {
            if (pawnKind == null)
            {
                Log.Warning(
                    "[Li AI Chat] Cannot create Archive Scholar: " +
                    "pawnKind is null.");

                return null;
            }

            if (faction == null)
            {
                Log.Warning(
                    "[Li AI Chat] Cannot create Archive Scholar: " +
                    "faction is null.");

                return null;
            }

            Pawn pawn =
                PawnGenerator.GeneratePawn(
                    pawnKind,
                    faction);

            if (pawn == null)
            {
                Log.Warning(
                    "[Li AI Chat] PawnGenerator failed " +
                    "to create Archive Scholar.");

                return null;
            }

            Thing_AncientEarthArchiveFragment archive =
                ArchiveScholarInitializer.Initialize(
                    pawn);

            if (archive == null)
            {
                Log.Warning(
                    "[Li AI Chat] Archive Scholar Pawn created, " +
                    "but Scholar initialization failed.");
            }

            Log.Message(
                "[Li AI Chat] Archive Scholar Pawn created: " +
                pawn.LabelShort +
                ", faction=" +
                faction.Name);

            return pawn;
        }
    }
}