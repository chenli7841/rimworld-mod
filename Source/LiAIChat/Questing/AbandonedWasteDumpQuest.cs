using System;
using System.Linq;
using LiAIChat.Blightfall;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using UnityEngine;
using Verse;

namespace LiAIChat.Questing
{
    public class QuestNode_AbandonedWasteDumpUnique : QuestNode
    {
        protected override bool TestRunInt(Slate slate)
        {
            Site targetSite;
            bool boundToBlightfallSite = slate.TryGet("blightfallSite", out targetSite, false) && targetSite != null;
            if (Find.QuestManager == null)
                return true;

            return !Find.QuestManager.QuestsListForReading
                .Where(quest => quest != null && quest.State == QuestState.Ongoing &&
                    quest.root?.defName == "LiAIChat_AbandonedWasteDump")
                .SelectMany(quest => quest.PartsListForReading.OfType<QuestPart_AbandonedWasteDump>())
                .Any(part => !boundToBlightfallSite || part.site == targetSite);
        }

        protected override void RunInt()
        {
        }
    }

    public class QuestNode_CreateAbandonedWasteDump : QuestNode
    {
        protected override bool TestRunInt(Slate slate)
        {
            PlanetTile tile = slate.Get<PlanetTile>("siteTile");
            return tile.Valid &&
                DefDatabase<ThingDef>.GetNamedSilentFail("Wastepack") != null &&
                DefDatabase<SitePartDef>.GetNamedSilentFail("LiAIChat_AbandonedWasteDump") != null;
        }

        protected override void RunInt()
        {
            PlanetTile tile = QuestGen.slate.Get<PlanetTile>("siteTile");
            SitePartDef sitePart = DefDatabase<SitePartDef>.GetNamed("LiAIChat_AbandonedWasteDump");
            Site site;
            bool useExistingSite = QuestGen.slate.TryGet("blightfallSite", out site, false) && site != null;
            if (!useExistingSite)
                site = SiteMaker.MakeSite(sitePart, tile, null, false, (float?)null, null);
            if (site == null)
                return;

            float difficultyFactor = Mathf.Clamp01(QuestGen.quest.points / 4000f);
            float pollutionPercent = Rand.Range(0.16f + 0.05f * difficultyFactor,
                0.24f + 0.08f * difficultyFactor);
            int minimumWastepacks = 14 + Mathf.RoundToInt(8f * difficultyFactor);
            int maximumWastepacks = 26 + Mathf.RoundToInt(14f * difficultyFactor);
            int wastepackCount = Rand.RangeInclusive(minimumWastepacks, maximumWastepacks);
            int expectedPollution = Mathf.RoundToInt(
                site.PreferredMapSize.x * site.PreferredMapSize.z * pollutionPercent);
            float rewardValue = Math.Max(500,
                Mathf.RoundToInt(expectedPollution * 0.12f + wastepackCount * 30f));
            string cleanedSignal = QuestGen.GenerateNewSignal("AbandonedWasteDumpCleaned", true);

            QuestGen.quest.AddPart(new QuestPart_AbandonedWasteDump
            {
                site = site,
                pollutionPercent = pollutionPercent,
                wastepackCount = wastepackCount,
                cleanedSignal = cleanedSignal
            });

            QuestGen.slate.Set("wasteDumpCleanedSignal", cleanedSignal);
            QuestGen.slate.Set("rewardValue", rewardValue);
            QuestGen.slate.Set("site", site);
        }
    }

    public class QuestNode_SpawnAbandonedWasteDumpSite : QuestNode
    {
        protected override bool TestRunInt(Slate slate)
        {
            return slate.Get<Site>("site", null, false) != null && Find.WorldObjects != null;
        }

        protected override void RunInt()
        {
            Site site = QuestGen.slate.Get<Site>("site", null, false);
            if (site == null || Find.WorldObjects == null)
                return;

            if (!Find.WorldObjects.AllWorldObjects.Contains(site))
                Find.WorldObjects.Add(site);

            if (site.MainSitePartDef?.defName == "LiAIChat_BlightfallContaminatedRuin")
            {
                WorldComponent_Blightfall component = WorldComponent_Blightfall.Current;
                if (component != null && component.GetSiteState(site) == null)
                    component.RegisterSite(site, BlightfallSiteType.ContaminatedRuin, component.SourceActivity);
            }
        }
    }

    public class SitePartWorker_AbandonedWasteDump : SitePartWorker
    {
        public override void PostMapGenerate(Map map)
        {
            base.PostMapGenerate(map);
            Site site = map?.Parent as Site;
            QuestPart_AbandonedWasteDump cleanupPart = QuestPart_AbandonedWasteDump.FindForSite(site);
            if (cleanupPart == null && WorldComponent_Blightfall.Current?.GetSiteState(site) != null)
                cleanupPart = QuestPart_AbandonedWasteDump.TryStartForBlightfallSite(site);
            cleanupPart?.InitializeFromMap(map);
        }
    }

    public class QuestPart_AbandonedWasteDump : QuestPartActivable
    {
        private const int PollInterval = 250;

        public Site site;
        public float pollutionPercent;
        public int wastepackCount;
        public int initialPollution;
        public int initialWastepacks;
        public int lastKnownPollution;
        public int lastKnownWastepacks;
        public string cleanedSignal;
        public bool initialized;
        public bool cleanedSignalSent;

        public override string DescriptionPart
        {
            get
            {
                if (quest == null || quest.State != QuestState.Ongoing)
                    return null;
                if (!initialized)
                    return "LiAIChat_AbandonedWasteDumpAwaitingMap".Translate();

                int cleanedPollution = Math.Max(0, initialPollution - lastKnownPollution);
                int progress = initialPollution <= 0
                    ? 0
                    : Mathf.Clamp(Mathf.RoundToInt(cleanedPollution * 100f / initialPollution), 0, 100);
                string description = "LiAIChat_AbandonedWasteDumpProgress".Translate(
                    progress, cleanedPollution, initialPollution, lastKnownWastepacks).ToString();
                BlightfallSiteState trackedSite = WorldComponent_Blightfall.Current?.GetSiteState(site);
                WorldComponent_Blightfall component = WorldComponent_Blightfall.Current;
                if (trackedSite != null && component != null && !component.CanPurifyTile(trackedSite.TileId))
                {
                    bool isCore = trackedSite.TileId == component.SourceCenterTileId;
                    bool outerPollutionRemains = component.PollutedTileIds.Any(tileId =>
                        tileId != component.SourceCenterTileId);
                    string lockedReason = !isCore
                        ? "LiAIChat_BlightfallInteriorLocked"
                        : component.CoreProtected && outerPollutionRemains
                            ? "LiAIChat_BlightfallCoreProtectedAndSealed"
                            : component.CoreProtected
                                ? "LiAIChat_BlightfallCoreProtected"
                                : "LiAIChat_BlightfallCoreLocked";
                    description += "\n" + lockedReason.Translate();
                }
                return description;
            }
        }

        public static QuestPart_AbandonedWasteDump FindForSite(Site site)
        {
            if (site == null || Find.QuestManager == null)
                return null;

            return Find.QuestManager.QuestsListForReading
                .Where(candidate => candidate != null && candidate.State == QuestState.Ongoing)
                .SelectMany(candidate => candidate.PartsListForReading.OfType<QuestPart_AbandonedWasteDump>())
                .FirstOrDefault(part => part.site == site);
        }

        public static QuestPart_AbandonedWasteDump TryStartForBlightfallSite(Site site)
        {
            BlightfallSiteState blightfallSite = WorldComponent_Blightfall.Current?.GetSiteState(site);
            if (site == null || blightfallSite == null ||
                blightfallSite.Status == BlightfallSiteStatus.Completed ||
                blightfallSite.Status == BlightfallSiteStatus.Removed || Find.QuestManager == null)
                return null;

            QuestPart_AbandonedWasteDump existing = FindForSite(site);
            if (existing != null)
                return existing;

            QuestScriptDef questDef = DefDatabase<QuestScriptDef>.GetNamedSilentFail("LiAIChat_AbandonedWasteDump");
            if (questDef == null)
                return null;

            var slate = new Slate();
            slate.Set("blightfallSite", site);
            Quest generatedQuest = QuestUtility.GenerateQuestAndMakeAvailable(questDef, slate);
            QuestPart_AbandonedWasteDump cleanupPart = FindForSite(site);
            if (cleanupPart == null)
                Log.Warning("[LiAIChat Blightfall] Could not attach the existing Abandoned Waste Dump quest to site " +
                    site.Label + (generatedQuest == null ? ". Quest generation returned null." : ". Quest was generated without an active cleanup part."));
            return cleanupPart;
        }

        public void InitializeFromMap(Map map)
        {
            if (initialized || map?.pollutionGrid == null)
                return;

            initialPollution = map.pollutionGrid.TotalPollution;
            initialWastepacks = CountWastepacks(map);
            lastKnownPollution = initialPollution;
            lastKnownWastepacks = initialWastepacks;
            initialized = true;
            Log.Message("[LiAIChat] Abandoned waste dump baseline recorded for " + site?.Label +
                ": pollutedCells=" + initialPollution + ", wastepacks=" + initialWastepacks + ".");
        }

        public override void QuestPartTick()
        {
            base.QuestPartTick();
            if (quest == null || quest.State != QuestState.Ongoing || cleanedSignalSent ||
                Find.TickManager.TicksGame % PollInterval != 0)
                return;

            Map map = site?.Map;
            if (map?.pollutionGrid == null)
                return;
            if (!initialized)
                InitializeFromMap(map);

            lastKnownPollution = map.pollutionGrid.TotalPollution;
            lastKnownWastepacks = CountWastepacks(map);
            int removedPollution = Math.Max(0, initialPollution - lastKnownPollution);
            bool pollutionCleaned = initialPollution > 0 && removedPollution * 100L >= initialPollution * 80L;
            if (!pollutionCleaned || lastKnownWastepacks > 0)
                return;

            BlightfallSiteState blightfallSite = WorldComponent_Blightfall.Current?.GetSiteState(site);
            WorldComponent_Blightfall component = WorldComponent_Blightfall.Current;
            if (blightfallSite != null && (component == null ||
                !component.CanPurifyTile(blightfallSite.TileId) ||
                !component.PurifyTile(blightfallSite.TileId)))
                return;

            // Set the saved guard before sending the signal because signal delivery
            // can end and clean up this quest immediately.
            cleanedSignalSent = true;
            if (blightfallSite != null)
                component.UpdateSiteStatus(site, BlightfallSiteStatus.Completed);
            Find.SignalManager.SendSignal(new Signal(cleanedSignal));
            Log.Message("[LiAIChat] Abandoned waste dump cleaned: " + site?.Label +
                "; initialPollution=" + initialPollution + "; remainingPollution=" +
                map.pollutionGrid.TotalPollution + "; remainingWastepacks=0.");
        }

        private static int CountWastepacks(Map map)
        {
            ThingDef wastepackDef = DefDatabase<ThingDef>.GetNamedSilentFail("Wastepack");
            if (map == null || wastepackDef == null)
                return 0;

            return map.listerThings.ThingsOfDef(wastepackDef)
                .Where(thing => thing != null && thing.Spawned && thing.Map == map && !thing.Destroyed)
                .Sum(thing => thing.stackCount);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref site, "site");
            Scribe_Values.Look(ref pollutionPercent, "pollutionPercent");
            Scribe_Values.Look(ref wastepackCount, "wastepackCount");
            Scribe_Values.Look(ref initialPollution, "initialPollution");
            Scribe_Values.Look(ref initialWastepacks, "initialWastepacks");
            Scribe_Values.Look(ref lastKnownPollution, "lastKnownPollution");
            Scribe_Values.Look(ref lastKnownWastepacks, "lastKnownWastepacks");
            Scribe_Values.Look(ref initialized, "initialized");
            Scribe_Values.Look(ref cleanedSignalSent, "cleanedSignalSent");
            Scribe_Values.Look(ref cleanedSignal, "cleanedSignal");
        }
    }

    public class GenStep_AbandonedWasteDump : GenStep
    {
        private const int Seed = 48173921;

        public override int SeedPart => Seed;

        public override void Generate(Map map, GenStepParams parms)
        {
            ThingDef wastepackDef = DefDatabase<ThingDef>.GetNamedSilentFail("Wastepack");
            if (map?.pollutionGrid == null || wastepackDef == null)
                return;

            // Biotech's normal Pollution gen step runs at order 890. Raise this
            // quest map to a reliably visible cleanup scale while keeping its
            // contamination inside the vanilla pollution system.
            QuestPart_AbandonedWasteDump questPart = QuestPart_AbandonedWasteDump.FindForSite(map.Parent as Site);
            float pollutionPercent = questPart?.pollutionPercent ?? Rand.Range(0.16f, 0.30f);
            int targetWastepacks = questPart?.wastepackCount ?? Rand.RangeInclusive(14, 32);
            PollutionUtility.PolluteMapToPercent(map, pollutionPercent, Rand.RangeInclusive(5, 9));

            ListSpawnablePollutedCells(map, out var candidates);
            if (candidates.Count == 0)
            {
                Log.Warning("[LiAIChat] Abandoned waste dump generated without usable polluted cells.");
                return;
            }

            int remainingWastepacks = targetWastepacks;
            int spawnedWastepacks = 0;
            while (remainingWastepacks > 0 && candidates.Count > 0)
            {
                int index = Rand.Range(0, candidates.Count);
                IntVec3 cell = candidates[index];
                candidates.RemoveAt(index);

                Thing pack = ThingMaker.MakeThing(wastepackDef);
                pack.stackCount = System.Math.Min(remainingWastepacks, Rand.RangeInclusive(1, 5));
                if (GenPlace.TryPlaceThing(pack, cell, map, ThingPlaceMode.Near))
                {
                    spawnedWastepacks += pack.stackCount;
                    remainingWastepacks -= pack.stackCount;
                }
                else if (!pack.Destroyed)
                {
                    pack.Destroy(DestroyMode.Vanish);
                }
            }

            Log.Message("[LiAIChat] Abandoned waste dump generated: pollutedCells=" +
                map.pollutionGrid.TotalPollution + ", wastepacks=" + spawnedWastepacks + ".");
        }

        private static void ListSpawnablePollutedCells(Map map, out System.Collections.Generic.List<IntVec3> cells)
        {
            cells = new System.Collections.Generic.List<IntVec3>();
            foreach (IntVec3 cell in map.AllCells)
            {
                if (cell.DistanceToSquared(map.Center) < 30 * 30 ||
                    !cell.Standable(map) || cell.Roofed(map) ||
                    !map.pollutionGrid.IsPolluted(cell))
                    continue;

                cells.Add(cell);
            }
        }
    }
}
