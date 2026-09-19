using Verse;

namespace LiAIChat.Archive
{
    public class GenStep_AncientArchiveSite
        : GenStep
    {
        public override int SeedPart =>
            78412631;

        public override void Generate(
            Map map,
            GenStepParams parms)
        {
            if (map == null)
                return;

            AncientArchiveVaultResult vault =
                AncientArchiveVaultGenerator.Generate(
                    map);

            if (vault == null)
                return;

            foreach (IntVec3 cell
                in vault.ArchiveCells)
            {
                SpawnArchive(
                    map,
                    cell);
            }
        }

        private void SpawnArchive(
            Map map,
            IntVec3 cell)
        {
            Thing_AncientEarthArchiveFragment archive =
                AncientArchiveFactory.Create(
                    ArchiveSourceType.AncientRuin,
                    "Ancient Earth archive site");

            if (archive == null)
                return;

            if (!cell.IsValid ||
                !cell.InBounds(map))
            {
                archive.Destroy();
                return;
            }

            GenSpawn.Spawn(
                archive,
                cell,
                map);
        }
    }
}