using RimWorld;
using Verse;

namespace LiAIChat.Archive
{
    public class SitePartWorker_AncientArchive
        : SitePartWorker
    {
        private const int ArchiveCount = 2;

        public override void PostMapGenerate(
            Map map)
        {
            base.PostMapGenerate(map);

            if (map == null)
                return;

            for (int i = 0; i < ArchiveCount; i++)
            {
                SpawnArchive(map);
            }
        }

        private void SpawnArchive(
            Map map)
        {
            Thing_AncientEarthArchiveFragment archive =
                AncientArchiveFactory.Create(
                    ArchiveSourceType.AncientRuin,
                    "Ancient Earth archive site");

            if (archive == null)
                return;

            IntVec3 cell =
                FindArchiveSpawnCell(map);

            if (!cell.IsValid)
            {
                archive.Destroy();
                return;
            }

            GenSpawn.Spawn(
                archive,
                cell,
                map);
        }

        private IntVec3 FindArchiveSpawnCell(
            Map map)
        {
            IntVec3 center =
                map.Center;

            for (int i = 0; i < 100; i++)
            {
                IntVec3 cell =
                    CellFinder.RandomClosewalkCellNear(
                        center,
                        map,
                        10);

                if (!cell.IsValid)
                    continue;

                if (!cell.Standable(map))
                    continue;

                return cell;
            }

            return IntVec3.Invalid;
        }
    }
}