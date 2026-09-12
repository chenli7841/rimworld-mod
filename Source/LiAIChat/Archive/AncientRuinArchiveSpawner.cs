using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace LiAIChat.Archive
{
    public static class AncientRuinArchiveSpawner
    {
        // 测试阶段保持100%
        private const float SpawnChance = 0.25f;

        public static void TrySpawnArchive(Map map)
        {
            if (map == null)
            {
                return;
            }

            AncientRuinArchiveMapComponent component =
                map.GetComponent<
                    AncientRuinArchiveMapComponent>();

            if (component == null)
            {
                Log.Warning(
                    "[Li AI Chat] Ancient ruin archive "
                    + "MapComponent not found.");

                return;
            }

            // 这张地图以前已经处理过
            if (component.SpawnProcessed)
            {
                Log.Message(
                    "[Li AI Chat] Ancient ruin archive "
                    + "spawn already processed for this map.");

                return;
            }

            /*
             * 从这里开始，这张地图只有一次机会。
             *
             * 注意：
             * 要在 Rand.Chance() 之前设为 true，
             * 否则掷骰失败后读档可以重新掷骰。
             */
            component.SpawnProcessed = true;

            List<Thing> caskets =
                map.listerThings.ThingsOfDef(
                    ThingDefOf.AncientCryptosleepCasket);

            if (caskets == null ||
                caskets.Count == 0)
            {
                Log.Message(
                    "[Li AI Chat] No ancient ruin found "
                    + "on this map.");

                return;
            }

            if (!Rand.Chance(SpawnChance))
            {
                Log.Message(
                    "[Li AI Chat] Ancient ruin found, "
                    + "but no archive spawned.");

                return;
            }

            Room room =
                caskets
                    .Select(c =>
                        c.Position.GetRoom(map))
                    .FirstOrDefault(r =>
                        r != null);

            if (room == null)
            {
                Log.Warning(
                    "[Li AI Chat] Ancient ruin caskets found, "
                    + "but no valid room found.");

                return;
            }

            List<IntVec3> validCells =
                room.Cells
                    .Where(cell =>
                        cell.Standable(map))
                    .Where(cell =>
                        cell.GetFirstItem(map) == null)
                    .Where(cell =>
                        cell.GetFirstBuilding(map) == null)
                    .Where(cell =>
                        caskets.All(casket =>
                            cell.DistanceTo(
                                casket.Position) >= 2f))
                    .ToList();

            if (validCells.Count == 0)
            {
                Log.Warning(
                    "[Li AI Chat] Ancient ruin room found, "
                    + "but no valid archive cell exists.");

                return;
            }

            IntVec3 targetCell =
                validCells.RandomElement();

            Thing archive =
                AncientArchiveFactory.Create(
                    ArchiveSourceType.AncientRuin,
                    "Recovered from an ancient ruin.");

            GenSpawn.Spawn(
                archive,
                targetCell,
                map);

            Log.Message(
                "[Li AI Chat] Ancient ruin archive "
                + "automatically spawned at "
                + targetCell);
        }
    }
}