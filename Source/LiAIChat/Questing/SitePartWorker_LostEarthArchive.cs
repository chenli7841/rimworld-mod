using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI.Group;

namespace LiAIChat.Questing
{
    public enum ArchiveRuinLayout
    {
        SignalRoom,
        CollapsedChamber,
        RelayStation
    }

    public enum ArchiveSiteThreatProfile
    {
        Safe,
        Guarded,
        Dangerous
    }
    public enum ArchiveMechDefensePattern
    {
        SingleScyther,
        TwoScythers,
        ScytherAndPikeman
    }

    public class SitePartWorker_LostEarthArchive : SitePartWorker
    {
        public override void PostMapGenerate(Map map)
        {
            base.PostMapGenerate(map);

            if (map == null)
            {
                Log.Error(
                    "[Li AI Chat] Lost Earth Archive site map is null.");

                return;
            }

            IntVec3 center =
                CellFinderLoose.RandomCellWith(
                    delegate (IntVec3 cell)
                    {
                        return cell.InBounds(map) &&
                               cell.Standable(map) &&
                               !cell.Fogged(map);
                    },
                    map);

            ArchiveSiteThreatProfile threatProfile =
                ChooseThreatProfile();

            GenerateArchiveRuin(
                map,
                center);

            ApplyThreatProfile(
                map,
                center,
                threatProfile);

            Log.Message(
                "[Li AI Chat] Lost Earth Archive site generated. " +
                "ThreatProfile=" +
                threatProfile +
                ".");
        }
        private ArchiveMechDefensePattern ChooseMechDefensePattern()
        {
            float roll = Rand.Value;

            if (roll < 0.45f)
            {
                return ArchiveMechDefensePattern.SingleScyther;
            }

            if (roll < 0.75f)
            {
                return ArchiveMechDefensePattern.TwoScythers;
            }

            return ArchiveMechDefensePattern.ScytherAndPikeman;
        }

        private ArchiveSiteThreatProfile ChooseThreatProfile()
        {
            float roll =
                Rand.Value;

            if (roll < 0.50f)
            {
                return ArchiveSiteThreatProfile.Safe;
            }

            if (roll < 0.85f)
            {
                return ArchiveSiteThreatProfile.Guarded;
            }

            return ArchiveSiteThreatProfile.Dangerous;
        }
        private void ApplyThreatProfile(
    Map map,
    IntVec3 center,
    ArchiveSiteThreatProfile threatProfile)
        {
            switch (threatProfile)
            {
                case ArchiveSiteThreatProfile.Safe:

                    ApplySafeProfile(
                        map,
                        center);

                    break;

                case ArchiveSiteThreatProfile.Guarded:

                    ApplyGuardedProfile(
                        map,
                        center);

                    break;

                case ArchiveSiteThreatProfile.Dangerous:

                    ApplyDangerousProfile(
                        map,
                        center);

                    break;
            }
        }
        private void ApplySafeProfile(
    Map map,
    IntVec3 center)
        {
            Log.Message(
                "[Li AI Chat] Archive site is SAFE.");
        }
        private void ApplyGuardedProfile(
    Map map,
    IntVec3 center)
        {
            int defenderCount =
                Rand.RangeInclusive(
                    1,
                    3);

            List<Pawn> defenders =
                new List<Pawn>();

            for (int i = 0;
                 i < defenderCount;
                 i++)
            {
                Pawn defender =
                    TrySpawnAncientSoldier(
                        map,
                        center);

                if (defender != null)
                {
                    defenders.Add(
                        defender);
                }
            }

            if (defenders.Count > 0)
            {
                CreateDefenderLord(
                    map,
                    center,
                    defenders);
            }

            Log.Message(
                "[Li AI Chat] Archive site is GUARDED. " +
                "Ancient soldiers spawned=" +
                defenders.Count +
                ".");
        }
        private Pawn TrySpawnAncientSoldier(
    Map map,
    IntVec3 ruinCenter)
        {
            IntVec3 spawnCell;

            if (!TryFindDefenderSpawnCell(
                map,
                ruinCenter,
                out spawnCell))
            {
                Log.Warning(
                    "[Li AI Chat] Could not find spawn cell " +
                    "for Ancient Soldier.");

                return null;
            }

            Faction faction =
                Faction.OfAncientsHostile;

            if (faction == null)
            {
                Log.Warning(
                    "[Li AI Chat] Faction.OfAncientsHostile is null.");

                return null;
            }

            Pawn pawn =
                PawnGenerator.GeneratePawn(
                    PawnKindDefOf.AncientSoldier,
                    faction);

            if (pawn == null)
            {
                Log.Warning(
                    "[Li AI Chat] Failed to generate Ancient Soldier.");

                return null;
            }

            GenSpawn.Spawn(
                pawn,
                spawnCell,
                map);

            Log.Message(
                "[Li AI Chat] Ancient Soldier spawned at " +
                spawnCell +
                ".");

            return pawn;
        }
        private void CreateDefenderLord(
    Map map,
    IntVec3 defendPoint,
    List<Pawn> defenders)
        {
            if (map == null ||
                defenders == null ||
                defenders.Count == 0)
            {
                return;
            }

            Faction faction =
                Faction.OfAncientsHostile;

            if (faction == null)
            {
                return;
            }

            LordJob lordJob =
                new LordJob_DefendPoint(
                    defendPoint);

            Lord lord =
                LordMaker.MakeNewLord(
                    faction,
                    lordJob,
                    map,
                    defenders);

            Log.Message(
                "[Li AI Chat] Archive defender Lord created. " +
                "PawnCount=" +
                defenders.Count +
                ", DefendPoint=" +
                defendPoint +
                ".");
        }
        private bool TryFindDefenderSpawnCell(
    Map map,
    IntVec3 center,
    out IntVec3 result)
        {
            const int minRadius = 6;
            const int maxRadius = 10;
            const int maxAttempts = 40;

            for (int attempt = 0;
                 attempt < maxAttempts;
                 attempt++)
            {
                int x =
                    Rand.RangeInclusive(
                        -maxRadius,
                        maxRadius);

                int z =
                    Rand.RangeInclusive(
                        -maxRadius,
                        maxRadius);

                IntVec3 cell =
                    center +
                    new IntVec3(
                        x,
                        0,
                        z);

                if (!cell.InBounds(map))
                {
                    continue;
                }

                float distanceSquared =
                    cell.DistanceToSquared(
                        center);

                if (distanceSquared <
                    minRadius * minRadius)
                {
                    continue;
                }

                if (distanceSquared >
                    maxRadius * maxRadius)
                {
                    continue;
                }

                if (!cell.Standable(map))
                {
                    continue;
                }

                if (cell.GetFirstPawn(map) != null)
                {
                    continue;
                }

                result =
                    cell;

                return true;
            }

            result =
                IntVec3.Invalid;

            return false;
        }
        private void ApplyDangerousProfile(
    Map map,
    IntVec3 center)
        {
            ArchiveMechDefensePattern pattern =
                ChooseMechDefensePattern();

            int spawned =
                SpawnDormantMechDefense(
                    map,
                    center,
                    pattern);

            Log.Message(
                "[Li AI Chat] Dangerous archive site generated. " +
                "Defense pattern = " +
                pattern +
                ", mechanoids = " +
                spawned);
        }
        private int SpawnDormantMechDefense(
    Map map,
    IntVec3 center,
    ArchiveMechDefensePattern pattern)
        {
            int spawned = 0;

            switch (pattern)
            {
                case ArchiveMechDefensePattern.SingleScyther:

                    if (TrySpawnDormantMechanoid(
                        PawnKindDefOf.Mech_Scyther,
                        map,
                        center) != null)
                    {
                        spawned++;
                    }

                    break;

                case ArchiveMechDefensePattern.TwoScythers:

                    for (int i = 0; i < 2; i++)
                    {
                        if (TrySpawnDormantMechanoid(
                            PawnKindDefOf.Mech_Scyther,
                            map,
                            center) != null)
                        {
                            spawned++;
                        }
                    }

                    break;

                case ArchiveMechDefensePattern.ScytherAndPikeman:

                    if (TrySpawnDormantMechanoid(
                        PawnKindDefOf.Mech_Scyther,
                        map,
                        center) != null)
                    {
                        spawned++;
                    }

                    if (TrySpawnDormantMechanoid(
                        PawnKindDefOf.Mech_Pikeman,
                        map,
                        center) != null)
                    {
                        spawned++;
                    }

                    break;
            }

            return spawned;
        }
        private Pawn TrySpawnDormantMechanoid(
    PawnKindDef kindDef,
    Map map,
    IntVec3 center)
        {
            if (kindDef == null || map == null)
            {
                return null;
            }

            IntVec3 spawnCell;

            if (!TryFindMechSpawnCell(
                map,
                center,
                out spawnCell))
            {
                Log.Warning(
                    "[Li AI Chat] Could not find " +
                    "a valid mechanoid spawn cell.");

                return null;
            }

            Faction faction =
                Faction.OfMechanoids;

            if (faction == null)
            {
                return null;
            }

            Pawn mech =
                PawnGenerator.GeneratePawn(
                    kindDef,
                    faction);

            if (mech == null)
            {
                return null;
            }

            GenSpawn.Spawn(
                mech,
                spawnCell,
                map);

            CompCanBeDormant dormant =
                mech.TryGetComp<CompCanBeDormant>();

            if (dormant == null)
            {
                Log.Warning(
                    "[Li AI Chat] Mechanoid " +
                    mech.LabelShort +
                    " has no CompCanBeDormant.");

                return mech;
            }

            dormant.ToSleep();

            ArchiveSiteDefenseComponent
    defenseComponent =
        map.GetComponent<
            ArchiveSiteDefenseComponent>();

            if (defenseComponent != null)
            {
                defenseComponent
                    .RegisterDefender(
                        mech);
            }

            Log.Message(
                "[Li AI Chat] Dormant mechanoid spawned: " +
                mech.LabelShort +
                ", Awake = " +
                dormant.Awake);

            return mech;
        }
        private bool TryFindMechSpawnCell(
    Map map,
    IntVec3 center,
    out IntVec3 result)
        {
            for (int i = 0; i < 60; i++)
            {
                IntVec3 cell =
                    CellFinder.RandomClosewalkCellNear(
                        center,
                        map,
                        8);

                float distance =
                    cell.DistanceTo(center);

                if (distance < 4f)
                {
                    continue;
                }

                if (!cell.InBounds(map))
                {
                    continue;
                }

                if (!cell.Standable(map))
                {
                    continue;
                }

                if (cell.GetFirstPawn(map) != null)
                {
                    continue;
                }

                if (cell.GetFirstBuilding(map) != null)
                {
                    continue;
                }

                result = cell;
                return true;
            }

            result = IntVec3.Invalid;
            return false;
        }

        private void GenerateArchiveRuin(
    Map map,
    IntVec3 center)
        {
            ArchiveRuinLayout layout =
                ChooseLayout();

            switch (layout)
            {
                case ArchiveRuinLayout.SignalRoom:

                    GenerateSignalRoom(
                        map,
                        center);

                    break;

                case ArchiveRuinLayout.CollapsedChamber:

                    GenerateCollapsedChamber(
                        map,
                        center);

                    break;

                case ArchiveRuinLayout.RelayStation:

                    GenerateRelayStation(
                        map,
                        center);

                    break;
            }

            Log.Message(
                "[Li AI Chat] Lost Earth Archive ruin generated. " +
                "Layout=" +
                layout +
                ", Center=" +
                center +
                ".");
        }
        private ArchiveRuinLayout ChooseLayout()
        {
            int value =
                Rand.RangeInclusive(
                    0,
                    2);

            switch (value)
            {
                case 0:
                    return ArchiveRuinLayout.SignalRoom;

                case 1:
                    return ArchiveRuinLayout.CollapsedChamber;

                default:
                    return ArchiveRuinLayout.RelayStation;
            }
        }
        private void GenerateSignalRoom(
    Map map,
    IntVec3 center)
        {
            SpawnFloorRectangle(
                map,
                center,
                4,
                4);

            SpawnRectangleWalls(
                map,
                center,
                4,
                4,
                0.20f);

            SpawnDebris(
                map,
                center,
                3,
                6,
                3);

            SpawnTerminal(
                map,
                center);
            TrySpawnThing(
    ThingDefOf.SleepingSpot,
    center + new IntVec3(-2, 0, 1),
    map);

            TrySpawnThing(
                ThingDefOf.StandingLamp,
                center + new IntVec3(2, 0, 2),
                map);
        }
        private void DamageAncientThing(
    Thing thing,
    float minPercent,
    float maxPercent)
        {
            if (thing == null)
            {
                return;
            }

            if (thing.MaxHitPoints <= 1)
            {
                return;
            }

            float percent =
                Rand.Range(
                    minPercent,
                    maxPercent);

            thing.HitPoints =
                (int)(
                    thing.MaxHitPoints *
                    percent);
        }
        private void GenerateCollapsedChamber(
    Map map,
    IntVec3 center)
        {
            SpawnFloorRectangle(
                map,
                center,
                5,
                4);

            SpawnRectangleWalls(
                map,
                center,
                5,
                4,
                0.45f);

            SpawnDebris(
                map,
                center,
                5,
                9,
                4);

            SpawnTerminal(
                map,
                center);
            TrySpawnThing(
    ThingDefOf.SimpleResearchBench,
    center + new IntVec3(-2, 0, 1),
    map);
        }
        private Thing TrySpawnThing(
    ThingDef def,
    IntVec3 cell,
    Map map)
        {
            if (def == null)
            {
                return null;
            }

            if (!cell.InBounds(map))
            {
                return null;
            }

            if (cell.GetFirstBuilding(map) != null)
            {
                return null;
            }

            Thing thing =
                ThingMaker.MakeThing(def);

            DamageAncientThing(
                thing,
                0.30f,
                0.75f);

            GenSpawn.Spawn(
                thing,
                cell,
                map);

            return thing;
        }
        private void GenerateRelayStation(
    Map map,
    IntVec3 center)
        {
            SpawnFloorRectangle(
                map,
                center,
                6,
                3);

            SpawnRectangleWalls(
                map,
                center,
                6,
                3,
                0.15f);

            SpawnDebris(
                map,
                center,
                3,
                5,
                5);

            IntVec3 terminalCell =
                center +
                new IntVec3(
                    3,
                    0,
                    0);

            SpawnTerminal(
                map,
                terminalCell);
        }
        private void SpawnDebris(
    Map map,
    IntVec3 center,
    int minCount,
    int maxCount,
    int radius)
        {
            int debrisCount =
                Rand.RangeInclusive(
                    minCount,
                    maxCount);

            for (int i = 0;
                 i < debrisCount;
                 i++)
            {
                IntVec3 cell;

                if (!TryFindDebrisCell(
                    map,
                    center,
                    radius,
                    out cell))
                {
                    continue;
                }

                Thing debris =
                    ThingMaker.MakeThing(
                        ThingDefOf.ChunkSlagSteel);

                GenSpawn.Spawn(
                    debris,
                    cell,
                    map);
            }
        }
        private bool TryFindDebrisCell(
    Map map,
    IntVec3 center,
    int radius,
    out IntVec3 result)
        {
            const int maxAttempts = 20;

            for (int attempt = 0;
                 attempt < maxAttempts;
                 attempt++)
            {
                int x =
                    Rand.RangeInclusive(
                        -radius,
                        radius);

                int z =
                    Rand.RangeInclusive(
                        -radius,
                        radius);

                IntVec3 cell =
                    center +
                    new IntVec3(
                        x,
                        0,
                        z);

                if (!cell.InBounds(map))
                {
                    continue;
                }

                // Terminal 中央位置必须保持空闲。
                if (cell == center)
                {
                    continue;
                }

                // Terminal 周围一圈也不要堆残骸。
                if (cell.DistanceToSquared(center) <= 2f)
                {
                    continue;
                }

                if (!cell.Standable(map))
                {
                    continue;
                }

                if (cell.GetFirstBuilding(map) != null)
                {
                    continue;
                }

                result = cell;

                return true;
            }

            result = IntVec3.Invalid;

            return false;
        }
        private void SpawnFloorRectangle(
    Map map,
    IntVec3 center,
    int radiusX,
    int radiusZ)
        {
            for (int x = -radiusX;
                 x <= radiusX;
                 x++)
            {
                for (int z = -radiusZ;
                     z <= radiusZ;
                     z++)
                {
                    IntVec3 cell =
                        center +
                        new IntVec3(
                            x,
                            0,
                            z);

                    if (!cell.InBounds(map))
                    {
                        continue;
                    }

                    map.terrainGrid.SetTerrain(
                        cell,
                        TerrainDefOf.Concrete);
                }
            }
        }
        private void SpawnRectangleWalls(
    Map map,
    IntVec3 center,
    int radiusX,
    int radiusZ,
    float missingChance)
        {
            for (int x = -radiusX;
                 x <= radiusX;
                 x++)
            {
                TrySpawnWall(
                    map,
                    center +
                    new IntVec3(
                        x,
                        0,
                        radiusZ),
                    missingChance);

                // 南面中央留下入口。
                if (x < -1 || x > 0)
                {
                    TrySpawnWall(
                        map,
                        center +
                        new IntVec3(
                            x,
                            0,
                            -radiusZ),
                        missingChance);
                }
            }

            for (int z = -radiusZ + 1;
                 z <= radiusZ - 1;
                 z++)
            {
                TrySpawnWall(
                    map,
                    center +
                    new IntVec3(
                        -radiusX,
                        0,
                        z),
                    missingChance);

                TrySpawnWall(
                    map,
                    center +
                    new IntVec3(
                        radiusX,
                        0,
                        z),
                    missingChance);
            }
        }
        private void TrySpawnWall(
    Map map,
    IntVec3 cell,
    float missingChance)
        {
            if (!cell.InBounds(map))
            {
                return;
            }

            if (Rand.Chance(
                missingChance))
            {
                return;
            }

            Thing wall =
                ThingMaker.MakeThing(
                    ThingDefOf.Wall,
                    ThingDefOf.Steel);

            GenSpawn.Spawn(
                wall,
                cell,
                map);
        }
        private void SpawnTerminal(
    Map map,
    IntVec3 center)
        {
            Thing terminal =
                ThingMaker.MakeThing(
                    LiAIChatThingDefOf
                        .LiAIChat_AncientArchiveSignalTerminal);

            float healthPercent =
    Rand.Range(
        0.55f,
        0.85f);

            terminal.HitPoints =
                (int)(
                    terminal.MaxHitPoints *
                    healthPercent);

            GenSpawn.Spawn(
                terminal,
                center,
                map);

            Log.Message(
                "[Li AI Chat] Ancient Archive Signal Terminal spawned.");
        }
    }
}