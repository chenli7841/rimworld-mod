using System.Collections.Generic;
using RimWorld;
using Verse;

namespace LiAIChat.Archive
{
    public class AncientArchiveVaultResult
    {
        public CellRect Rect;

        public List<IntVec3> ArchiveCells =
            new List<IntVec3>();
    }

    public static class AncientArchiveVaultGenerator
    {
        private const int Width = 11;
        private const int Height = 9;

        public static AncientArchiveVaultResult Generate(
            Map map)
        {
            if (map == null)
                return null;

            CellRect rect =
                CellRect.CenteredOn(
                    map.Center,
                    Width,
                    Height);

            if (!RectFitsMap(
                rect,
                map))
            {
                return null;
            }

            AncientArchiveVaultResult result =
                new AncientArchiveVaultResult
                {
                    Rect = rect
                };

            BuildVault(
                map,
                rect);

            AddArchiveCells(
                result,
                rect);

            return result;
        }

        private static bool RectFitsMap(
            CellRect rect,
            Map map)
        {
            foreach (IntVec3 cell in rect)
            {
                if (!cell.InBounds(map))
                    return false;
            }

            return true;
        }

        private static void BuildVault(
            Map map,
            CellRect rect)
        {
            IntVec3 doorCell =
                new IntVec3(
                    rect.CenterCell.x,
                    0,
                    rect.minZ);

            ThingDef wallStuff =
                GenStuff.DefaultStuffFor(
                    ThingDefOf.Wall);

            ThingDef doorStuff =
                GenStuff.DefaultStuffFor(
                    ThingDefOf.Door);

            foreach (IntVec3 cell
                in rect.EdgeCells)
            {
                if (cell == doorCell)
                {
                    SpawnBuilding(
                        ThingDefOf.Door,
                        doorStuff,
                        cell,
                        map);

                    continue;
                }

                SpawnBuilding(
                    ThingDefOf.Wall,
                    wallStuff,
                    cell,
                    map);
            }
        }

        private static void SpawnBuilding(
            ThingDef buildingDef,
            ThingDef stuff,
            IntVec3 cell,
            Map map)
        {
            Thing thing =
                ThingMaker.MakeThing(
                    buildingDef,
                    stuff);

            GenSpawn.Spawn(
                thing,
                cell,
                map);
        }

        private static void AddArchiveCells(
            AncientArchiveVaultResult result,
            CellRect rect)
        {
            IntVec3 center =
                rect.CenterCell;

            result.ArchiveCells.Add(
                center + IntVec3.West);

            result.ArchiveCells.Add(
                center + IntVec3.East);
        }
    }
}