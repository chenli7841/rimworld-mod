using RimWorld;
using Verse;

namespace LiAIChat.Archive
{
    public static class ArchiveScholarGiftUtility
    {
        public static bool TryGiveArchiveToColony(
            Pawn pawn)
        {
            if (pawn == null)
            {
                return false;
            }

            if (!pawn.Spawned ||
                pawn.Map == null)
            {
                Log.Warning(
                    "[Li AI Chat] Scholar gift failed: " +
                    "pawn is not spawned.");

                return false;
            }

            if (pawn.inventory == null)
            {
                Log.Warning(
                    "[Li AI Chat] Scholar gift failed: " +
                    "pawn has no inventory.");

                return false;
            }

            Thing_AncientEarthArchiveFragment archive =
                FindScholarArchive(pawn);

            if (archive == null)
            {
                Log.Warning(
                    "[Li AI Chat] Scholar gift failed: " +
                    "no Ancient Earth Archive was found in " +
                    pawn.LabelShort +
                    "'s inventory.");

                return false;
            }

            Thing droppedThing;

            bool dropped =
                pawn.inventory.innerContainer.TryDrop(
                    archive,
                    ThingPlaceMode.Near,
                    archive.stackCount,
                    out droppedThing,
                    null,
                    null);

            if (!dropped)
            {
                Log.Warning(
                    "[Li AI Chat] Scholar gift failed: " +
                    "TryDrop returned false.");

                return false;
            }

            if (droppedThing != null)
            {
                droppedThing.SetForbidden(
                    false,
                    false);

                Messages.Message(
                    pawn.LabelShort +
                    " 决定将自己保存的古代地球档案留给殖民地。",
                    droppedThing,
                    MessageTypeDefOf.PositiveEvent);
            }

            return true;
        }


        private static Thing_AncientEarthArchiveFragment
            FindScholarArchive(
                Pawn pawn)
        {
            if (pawn == null ||
                pawn.inventory == null)
            {
                return null;
            }

            foreach (Thing thing in
                     pawn.inventory.innerContainer)
            {
                Thing_AncientEarthArchiveFragment archive =
                    thing as Thing_AncientEarthArchiveFragment;

                if (archive != null)
                {
                    return archive;
                }
            }

            return null;
        }
    }
}