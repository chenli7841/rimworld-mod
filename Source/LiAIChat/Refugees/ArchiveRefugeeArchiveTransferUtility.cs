using RimWorld;
using System.Collections.Generic;
using Verse;

namespace LiAIChat.Refugees
{
    public static class ArchiveRefugeeArchiveTransferUtility
    {
        public static bool TryLeaveArchive(
            ArchiveRefugeeGroupState group,
            IList<Pawn> pawns)
        {
            if (group == null ||
                pawns == null)
            {
                return false;
            }

            Pawn carrier =
                FindPawnById(
                    pawns,
                    group.CarrierPawnId);

            if (carrier == null)
            {
                Log.Warning(
                    "[Li AI Chat] Refugee Archive transfer: " +
                    "carrier could not be found.");

                return false;
            }

            if (carrier.inventory == null)
            {
                return false;
            }

            Thing archive = null;

            List<Thing> inventory =
                carrier.inventory.innerContainer
                    .InnerListForReading;

            for (int i = 0;
                 i < inventory.Count;
                 i++)
            {
                Thing thing =
                    inventory[i];

                if (thing != null &&
                    thing.thingIDNumber ==
                    group.ArchiveThingId)
                {
                    archive = thing;
                    break;
                }
            }

            if (archive == null)
            {
                Log.Warning(
                    "[Li AI Chat] Refugee Archive transfer: " +
                    "original archive was not found in carrier inventory.");

                return false;
            }


            Thing droppedThing;

            bool dropped =
                carrier.inventory.innerContainer.TryDrop(
                    archive,
                    ThingPlaceMode.Near,
                    archive.stackCount,
                    out droppedThing,
                    null,
                    null);

            if (!dropped)
            {
                Log.Warning(
                    "[Li AI Chat] Refugee Archive transfer failed.");

                return false;
            }

            if (droppedThing != null)
            {
                droppedThing.SetForbidden(
                    false,
                    false);
            }

            Log.Message(
                "[Li AI Chat] Refugee group left its original Archive with the colony.");

            return true;
        }


        private static Pawn FindPawnById(
            IList<Pawn> pawns,
            int pawnId)
        {
            for (int i = 0;
                 i < pawns.Count;
                 i++)
            {
                Pawn pawn = pawns[i];

                if (pawn != null &&
                    pawn.thingIDNumber ==
                    pawnId)
                {
                    return pawn;
                }
            }

            return null;
        }
    }
}