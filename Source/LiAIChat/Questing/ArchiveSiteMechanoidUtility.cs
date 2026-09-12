using RimWorld;
using Verse;

namespace LiAIChat.Questing
{
    public static class
        ArchiveSiteMechanoidUtility
    {
        public static int
            WakeArchiveDefenders(
                Map map)
        {
            if (map == null)
            {
                return 0;
            }

            ArchiveSiteDefenseComponent
                defenseComponent =
                    map.GetComponent<
                        ArchiveSiteDefenseComponent>();

            if (defenseComponent == null)
            {
                return 0;
            }

            int wakeCount = 0;

            foreach (Pawn pawn in
                map.mapPawns.AllPawnsSpawned)
            {
                if (pawn == null)
                {
                    continue;
                }

                if (!defenseComponent
                    .IsRegisteredDefender(
                        pawn))
                {
                    continue;
                }

                if (pawn.Dead)
                {
                    continue;
                }

                CompCanBeDormant dormant =
                    pawn.TryGetComp<
                        CompCanBeDormant>();

                if (dormant == null)
                {
                    continue;
                }

                if (dormant.Awake)
                {
                    continue;
                }

                dormant.WakeUp();

                wakeCount++;
            }

            return wakeCount;
        }
    }
}