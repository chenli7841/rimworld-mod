using RimWorld;
using Verse;

namespace LiAIChat.Questing
{
    public static class ArchiveSiteDefenderUtility
    {
        public static bool HasActiveDefenders(
            Map map)
        {
            if (map == null)
            {
                return false;
            }

            foreach (Pawn pawn in
                map.mapPawns.AllPawnsSpawned)
            {
                if (IsActiveArchiveDefender(
                    pawn))
                {
                    return true;
                }
            }

            return false;
        }

        public static int CountActiveDefenders(
            Map map)
        {
            if (map == null)
            {
                return 0;
            }

            int count = 0;

            foreach (Pawn pawn in
                map.mapPawns.AllPawnsSpawned)
            {
                if (IsActiveArchiveDefender(
                    pawn))
                {
                    count++;
                }
            }

            return count;
        }

        private static bool IsActiveArchiveDefender(
            Pawn pawn)
        {
            if (pawn == null)
            {
                return false;
            }

            if (pawn.Dead)
            {
                return false;
            }

            if (pawn.Downed)
            {
                return false;
            }

            if (pawn.Faction !=
                Faction.OfAncientsHostile)
            {
                return false;
            }

            return true;
        }
    }
}