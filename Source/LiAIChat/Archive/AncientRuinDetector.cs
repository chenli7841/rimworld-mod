using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace LiAIChat.Archive
{
    public static class AncientRuinDetector
    {
        public static void ScanMap(Map map)
        {
            if (map == null)
            {
                return;
            }

            List<Thing> caskets =
                map.listerThings.ThingsOfDef(
                    ThingDefOf.AncientCryptosleepCasket);

            Log.Message(
                "[Li AI Chat] Ancient ruin scan: found "
                + caskets.Count
                + " ancient cryptosleep casket(s).");

            HashSet<Room> rooms =
                new HashSet<Room>();

            foreach (Thing casket in caskets)
            {
                Room room =
                    casket.Position.GetRoom(map);

                if (room == null)
                {
                    continue;
                }

                rooms.Add(room);
            }

            Log.Message(
                "[Li AI Chat] Ancient ruin scan: "
                + rooms.Count
                + " unique casket room(s).");
        }

        public static void DebugPlaceArchiveInAncientRuin(
            Map map)
        {
            if (map == null)
            {
                return;
            }

            List<Thing> caskets =
                map.listerThings.ThingsOfDef(
                    ThingDefOf.AncientCryptosleepCasket);

            Room room =
                caskets
                    .Select(c =>
                        c.Position.GetRoom(map))
                    .FirstOrDefault(r =>
                        r != null);

            if (room == null)
            {
                Log.Warning(
                    "[Li AI Chat] No Ancient Danger room found.");

                return;
            }

            IntVec3 targetCell =
                room.Cells
                    .Where(cell =>
                        cell.Standable(map))
                    .Where(cell =>
                        cell.GetFirstItem(map) == null)
                    .FirstOrDefault();

            if (!targetCell.IsValid)
            {
                Log.Warning(
                    "[Li AI Chat] No valid cell found "
                    + "inside Ancient Danger room.");

                return;
            }

            Thing archive =
                AncientArchiveFactory.Create(
                    ArchiveSourceType.AncientRuin,
                    "Recovered from an ancient ruin.");

            GenSpawn.Spawn(
                archive,
                targetCell,
                map);

            Messages.Message(
                "An Ancient Earth Archive was placed "
                + "inside the ancient ruin.",
                archive,
                MessageTypeDefOf.PositiveEvent);

            Log.Message(
                "[Li AI Chat] Ancient ruin archive placed at "
                + targetCell);
        }
    }
}