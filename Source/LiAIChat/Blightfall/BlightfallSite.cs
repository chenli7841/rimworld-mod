using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace LiAIChat.Blightfall
{
    public static class BlightfallSiteUtility
    {
        public static void EnsureSitesForPollutedTiles(WorldComponent_Blightfall component)
        {
            if (component == null || Find.WorldObjects == null)
                return;

            IEnumerable<int> targetTiles = component.PollutedTileIds.Concat(
                component.PollutionNodes.Where(node => node != null && node.Status == BlightfallPollutionNodeStatus.Active)
                    .Select(node => node.TileId)).Distinct().ToList();
            foreach (int tileId in targetTiles)
            {
                PlanetTile tile = new PlanetTile(tileId);
                if (tileId == component.SourceCenterTileId &&
                    component.CampaignState == BlightfallCampaignState.Completed &&
                    component.Ending != BlightfallCampaignEnding.DestroyHeart)
                    continue;
                if (tileId == component.SourceCenterTileId &&
                    component.CampaignState != BlightfallCampaignState.CoreExposed &&
                    component.CampaignState != BlightfallCampaignState.Completed)
                {
                    RemoveLockedCoreSites(component, tileId);
                    continue;
                }
                if (tileId == component.SourceCenterTileId &&
                    component.CampaignState == BlightfallCampaignState.CoreExposed)
                    UpgradeSiteForBlightHeart(tileId);

                BlightfallPollutionNodeState resonanceSpire = component.GetNode(
                    BlightfallPollutionNodeType.ResonanceSpire, tileId);
                if (!tile.Valid || Find.WorldObjects.AnyWorldObjectAt(tile))
                {
                    if (resonanceSpire != null && resonanceSpire.Status == BlightfallPollutionNodeStatus.Active)
                        UpgradeSiteForResonanceSpire(tileId, resonanceSpire);
                    continue;
                }

                bool hasLiveSiteRecord = component.SiteStates.Any(state => state != null &&
                    state.TileId == tileId && state.Status != BlightfallSiteStatus.Removed &&
                    state.Status != BlightfallSiteStatus.Completed);
                bool completedSpecialSite = component.SiteStates.Any(state => state != null &&
                    state.TileId == tileId && state.Status == BlightfallSiteStatus.Completed &&
                    state.Type != BlightfallSiteType.ContaminatedRuin);
                if (completedSpecialSite)
                    continue;
                if (!hasLiveSiteRecord)
                    TryCreateInitialSite(component, tile);
            }
        }

        public static Site TryCreateInitialSite(WorldComponent_Blightfall component, PlanetTile tile)
        {
            if (component == null || !tile.Valid || Find.WorldObjects == null ||
                Find.WorldObjects.AnyWorldObjectAt(tile))
                return null;

            BlightfallSiteType siteType = ChooseSiteType(component, tile.tileId);
            SitePartDef partDef = DefDatabase<SitePartDef>.GetNamedSilentFail(SitePartDefFor(siteType));
            if (partDef == null)
                return null;

            try
            {
                float difficulty = siteType == BlightfallSiteType.ResonanceDefense ||
                    siteType == BlightfallSiteType.BlightHeart
                    ? 100f : component.SourceActivity;
                Site site = SiteMaker.MakeSite(partDef, tile, null, false, (float?)null, null);
                if (site == null)
                    return null;

                site.customLabel = SiteLabelFor(siteType).Translate();
                Find.WorldObjects.Add(site);
                component.ResetNodeTargetBindings(tile.tileId);
                if (!component.RegisterSite(site, siteType, difficulty))
                {
                    site.Destroy();
                    return null;
                }

                Log.Message("[LiAIChat Blightfall] Created " + siteType + " site at tile " + tile.tileId +
                    " (siteId=" + site.ID + ", difficulty=" + component.SourceActivity + ").");
                return site;
            }
            catch (Exception exception)
            {
                Log.Error("[LiAIChat Blightfall] Failed to create contaminated site at tile " +
                    tile.tileId + ": " + exception);
                return null;
            }
        }

        public static BlightfallSiteState StateFor(Site site)
        {
            return WorldComponent_Blightfall.Current?.GetSiteState(site);
        }

        private static BlightfallSiteType ChooseSiteType(WorldComponent_Blightfall component, int tileId)
        {
            BlightfallPollutionNodeState node = component.GetNodesAtTile(tileId).FirstOrDefault(item =>
                item.Status == BlightfallPollutionNodeStatus.Active);
            if (node != null)
            {
                if (node.Type == BlightfallPollutionNodeType.SporeTower) return BlightfallSiteType.SporeForest;
                if (node.Type == BlightfallPollutionNodeType.BroodNest) return BlightfallSiteType.ToxicHive;
                if (node.Type == BlightfallPollutionNodeType.ResonanceSpire &&
                    tileId != component.SourceCenterTileId) return BlightfallSiteType.ResonanceDefense;
            }
            if (tileId == component.SourceCenterTileId)
                return component.CampaignState == BlightfallCampaignState.CoreExposed
                    ? BlightfallSiteType.BlightHeart
                    : BlightfallSiteType.ContaminatedRuin;
            switch ((tileId % 4 + 4) % 4)
            {
                case 0: return BlightfallSiteType.SporeForest;
                case 1: return BlightfallSiteType.ToxicHive;
                case 2: return BlightfallSiteType.AncientProcessingPlant;
                default: return BlightfallSiteType.ContaminatedRuin;
            }
        }

        public static string SitePartDefFor(BlightfallSiteType type)
        {
            switch (type)
            {
                case BlightfallSiteType.SporeForest: return "LiAIChat_BlightfallSporeForest";
                case BlightfallSiteType.ToxicHive: return "LiAIChat_BlightfallToxicHive";
                case BlightfallSiteType.AncientProcessingPlant: return "LiAIChat_BlightfallProcessingPlant";
                case BlightfallSiteType.ResonanceDefense: return "LiAIChat_BlightfallResonanceDefense";
                case BlightfallSiteType.BlightHeart: return "LiAIChat_BlightfallBlightHeart";
                default: return "LiAIChat_BlightfallContaminatedRuin";
            }
        }

        private static string SiteLabelFor(BlightfallSiteType type)
        {
            switch (type)
            {
                case BlightfallSiteType.SporeForest: return "LiAIChat_BlightfallSporeForestSiteLabel";
                case BlightfallSiteType.ToxicHive: return "LiAIChat_BlightfallToxicHiveSiteLabel";
                case BlightfallSiteType.AncientProcessingPlant: return "LiAIChat_BlightfallProcessingPlantSiteLabel";
                case BlightfallSiteType.ResonanceDefense: return "LiAIChat_BlightfallResonanceSiteLabel";
                case BlightfallSiteType.BlightHeart: return "LiAIChat_BlightfallBlightHeartSiteLabel";
                default: return "LiAIChat_BlightfallSiteLabel";
            }
        }

        private static void UpgradeSiteForResonanceSpire(int tileId, BlightfallPollutionNodeState node)
        {
            Site site = Find.WorldObjects.Sites.FirstOrDefault(candidate => candidate != null &&
                candidate.Tile.tileId == tileId);
            if (site == null)
                return;
            WorldComponent_Blightfall component = WorldComponent_Blightfall.Current;
            BlightfallSiteState state = component?.GetSiteState(site);
            if (state == null)
            {
                if (!component.RegisterSite(site, BlightfallSiteType.ResonanceDefense, 100f))
                    return;
                state = component.GetSiteState(site);
            }
            else if (state.Status != BlightfallSiteStatus.Completed && state.Status != BlightfallSiteStatus.Removed)
            {
                state.Type = BlightfallSiteType.ResonanceDefense;
                state.Difficulty = 100f;
            }
            if (state != null && state.Status != BlightfallSiteStatus.Completed &&
                state.Status != BlightfallSiteStatus.Removed)
            {
                site.customLabel = "LiAIChat_BlightfallResonanceSiteLabel".Translate();
                node.Activity = Math.Max(node.Activity, 100f);
            }
        }

        private static void UpgradeSiteForBlightHeart(int tileId)
        {
            Site site = Find.WorldObjects.Sites.FirstOrDefault(candidate => candidate != null &&
                candidate.Tile.tileId == tileId);
            if (site == null)
                return;
            WorldComponent_Blightfall component = WorldComponent_Blightfall.Current;
            BlightfallSiteState state = component?.GetSiteState(site);
            if (state == null)
            {
                if (!component.RegisterSite(site, BlightfallSiteType.BlightHeart, 100f))
                    return;
                state = component.GetSiteState(site);
            }
            if (state == null || state.CoreBattleStage == BlightfallCoreBattleStage.Completed)
                return;
            state.Type = BlightfallSiteType.BlightHeart;
            state.Difficulty = 100f;
            if (state.Status == BlightfallSiteStatus.Completed || state.Status == BlightfallSiteStatus.Removed)
                state.Status = BlightfallSiteStatus.Created;
            site.customLabel = "LiAIChat_BlightfallBlightHeartSiteLabel".Translate();
        }

        private static void RemoveLockedCoreSites(WorldComponent_Blightfall component, int tileId)
        {
            foreach (Site site in Find.WorldObjects.Sites.Where(candidate => candidate != null &&
                candidate.Tile.tileId == tileId).ToList())
            {
                if (site.HasMap || Find.Maps.Any(map => map != null && map.Parent == site))
                    continue;
                bool questUsesSite = Find.QuestManager != null && Find.QuestManager.QuestsListForReading.Any(quest =>
                    quest != null && quest.State == QuestState.Ongoing &&
                    quest.PartsListForReading.Any(part => part.QuestLookTargets != null &&
                        part.QuestLookTargets.Any(target => target.HasWorldObject && target.WorldObject == site)));
                if (questUsesSite)
                    continue;
                BlightfallSiteState state = component.GetSiteState(site);
                if (state != null)
                    state.Status = BlightfallSiteStatus.Removed;
                site.Destroy();
            }
        }
    }

    public class SitePartWorker_BlightfallContaminatedRuin : SitePartWorker
    {
        public override void PostMapGenerate(Map map)
        {
            base.PostMapGenerate(map);
            Site site = map?.Parent as Site;
            WorldComponent_Blightfall component = WorldComponent_Blightfall.Current;
            component?.UpdateSiteStatus(site, BlightfallSiteStatus.MapGenerated);

            BlightfallSiteState siteState = component?.GetSiteState(site);
            if (siteState != null && siteState.Type == BlightfallSiteType.ContaminatedRuin &&
                component.IsTilePolluted(siteState.TileId) &&
                siteState.Status != BlightfallSiteStatus.Completed &&
                siteState.Status != BlightfallSiteStatus.Removed)
            {
                LiAIChat.Questing.QuestPart_AbandonedWasteDump cleanupPart =
                    LiAIChat.Questing.QuestPart_AbandonedWasteDump.FindForSite(site) ??
                    LiAIChat.Questing.QuestPart_AbandonedWasteDump.TryStartForBlightfallSite(site);
                cleanupPart?.InitializeFromMap(map);
            }
            BlightfallNodeTargetUtility.SpawnMissingTargets(map);
            BlightfallSpecialSiteUtility.Initialize(map);
            BlightfallResonanceDefenseUtility.Initialize(map);
            BlightfallFinalCoreUtility.Initialize(map);
        }

        public override void Notify_SiteMapAboutToBeRemoved(SitePart sitePart)
        {
            base.Notify_SiteMapAboutToBeRemoved(sitePart);
            Site site = sitePart?.site;
            LiAIChat.Questing.QuestPart_AbandonedWasteDump.FindForSite(site)?.NotifyMapLeaving(site?.Map);
            WorldComponent_Blightfall component = WorldComponent_Blightfall.Current;
            BlightfallSiteState state = component?.GetSiteState(site);
            component?.UpdateSiteStatus(site, BlightfallSiteStatus.Visited);
            if (state != null && state.Type == BlightfallSiteType.ResonanceDefense &&
                state.Status != BlightfallSiteStatus.Completed && state.Status != BlightfallSiteStatus.Removed)
            {
                state.DefenseSetupComplete = false;
                state.ObjectiveThingId = -1;
                state.ObjectiveDestroyed = false;
                component.ResetNodeTargetBindings(state.TileId);
            }
            if (state != null && state.Type == BlightfallSiteType.BlightHeart &&
                state.CoreBattleStage != BlightfallCoreBattleStage.Completed)
            {
                BlightfallFinalCoreUtility.SaveHeartHealth(Find.Maps.FirstOrDefault(candidate =>
                    candidate != null && candidate.Parent == site), state);
                state.CorePerimeterTargetThingIds.Clear();
                state.CoreDefenseTargetThingIds.Clear();
                state.CoreHeartThingId = -1;
            }
        }
    }

    public class GenStep_BlightfallContaminatedRuin : GenStep
    {
        private const int Seed = 10358429;
        private const float MinimumPollutionPercent = 0.14f;
        private const float MaximumPollutionPercent = 0.28f;
        private const int MinimumWastepacks = 14;
        private const int MaximumWastepacks = 32;

        public override int SeedPart => Seed;

        public override void Generate(Map map, GenStepParams parms)
        {
            if (map?.pollutionGrid == null)
                return;

            WorldComponent_Blightfall component = WorldComponent_Blightfall.Current;
            BlightfallSiteState siteState = BlightfallSiteUtility.StateFor(map.Parent as Site);
            if (siteState?.Type == BlightfallSiteType.BlightHeart &&
                siteState.Status == BlightfallSiteStatus.Completed)
                return;
            if (siteState != null && component != null && !component.IsTilePolluted(siteState.TileId))
                return;
            float difficulty = siteState?.Difficulty ?? 50f;
            float difficultyFactor = Mathf.Clamp01(difficulty / 100f);
            float minimumPollution = siteState?.Type == BlightfallSiteType.BlightHeart ? 0.72f :
                siteState?.Type == BlightfallSiteType.ResonanceDefense ? 0.40f :
                siteState?.Type == BlightfallSiteType.SporeForest ? 0.30f :
                siteState?.Type == BlightfallSiteType.ToxicHive ? 0.27f : MinimumPollutionPercent;
            float maximumPollution = siteState?.Type == BlightfallSiteType.BlightHeart ? 0.82f :
                siteState?.Type == BlightfallSiteType.ResonanceDefense ? 0.55f :
                siteState?.Type == BlightfallSiteType.SporeForest ? 0.42f :
                siteState?.Type == BlightfallSiteType.ToxicHive ? 0.40f : MaximumPollutionPercent;
            int minimumWastepacks = siteState?.Type == BlightfallSiteType.BlightHeart ? 80 :
                siteState?.Type == BlightfallSiteType.ResonanceDefense ? 38 : MinimumWastepacks;
            int maximumWastepacks = siteState?.Type == BlightfallSiteType.BlightHeart ? 120 :
                siteState?.Type == BlightfallSiteType.ResonanceDefense ? 60 : MaximumWastepacks;
            float pollutionPercent = Mathf.Lerp(minimumPollution, maximumPollution,
                difficultyFactor);
            int wastepackCount = Mathf.RoundToInt(Mathf.Lerp(minimumWastepacks, maximumWastepacks,
                difficultyFactor));

            // Use RimWorld's pollution grid so Biotech and vanilla pollution visuals stay in sync.
            PollutionUtility.PolluteMapToPercent(map, pollutionPercent, Rand.RangeInclusive(5, 9));
            int spawnedWastepacks = SpawnWastepacks(map, wastepackCount);
            Log.Message("[LiAIChat Blightfall] Generated contaminated ruin: siteId=" +
                (map.Parent as Site)?.ID + ", difficulty=" + difficulty + ", pollutedCells=" +
                map.pollutionGrid.TotalPollution + ", wastepacks=" + spawnedWastepacks + ".");
        }

        private static int SpawnWastepacks(Map map, int targetCount)
        {
            ThingDef wastepackDef = DefDatabase<ThingDef>.GetNamedSilentFail("Wastepack");
            if (wastepackDef == null)
                return 0;

            var candidates = new List<IntVec3>();
            foreach (IntVec3 cell in map.AllCells)
            {
                if (!cell.Standable(map) || cell.Roofed(map) || !map.pollutionGrid.IsPolluted(cell))
                    continue;
                candidates.Add(cell);
            }

            int spawned = 0;
            while (spawned < targetCount && candidates.Count > 0)
            {
                int index = Rand.Range(0, candidates.Count);
                IntVec3 cell = candidates[index];
                candidates.RemoveAt(index);
                Thing pack = ThingMaker.MakeThing(wastepackDef);
                pack.stackCount = Math.Min(targetCount - spawned, Rand.RangeInclusive(1, 5));
                if (GenPlace.TryPlaceThing(pack, cell, map, ThingPlaceMode.Near))
                    spawned += pack.stackCount;
                else if (!pack.Destroyed)
                    pack.Destroy(DestroyMode.Vanish);
            }
            return spawned;
        }
    }

    public class GenStep_BlightfallNodeTargets : GenStep
    {
        private const int Seed = 73918421;

        public override int SeedPart => Seed;

        public override void Generate(Map map, GenStepParams parms)
        {
            BlightfallNodeTargetUtility.SpawnMissingTargets(map);
        }
    }

    public static class BlightfallNodeTargetUtility
    {
        public static void SpawnMissingTargets(Map map)
        {
            Site site = map?.Parent as Site;
            WorldComponent_Blightfall component = WorldComponent_Blightfall.Current;
            BlightfallSiteState siteState = component?.GetSiteState(site);
            if (map == null || siteState == null)
                return;

            foreach (BlightfallPollutionNodeState node in component.GetNodesAtTile(siteState.TileId)
                         .Where(item => item.Status == BlightfallPollutionNodeStatus.Active && item.TargetThingId < 0)
                         .ToList())
            {
                ThingDef targetDef = DefDatabase<ThingDef>.GetNamedSilentFail(TargetDefName(node.Type, siteState.Type));
                if (targetDef == null)
                {
                    Log.Error("[LiAIChat Blightfall] Missing target ThingDef for node " + node.Type + ".");
                    continue;
                }

                Thing target = ThingMaker.MakeThing(targetDef);
                if (GenPlace.TryPlaceThing(target, map.Center, map, ThingPlaceMode.Near) && target.Spawned &&
                    component.RegisterNodeTarget(node.Type, siteState.TileId, target.thingIDNumber))
                {
                    Log.Message("[LiAIChat Blightfall] Spawned " + node.Type + " target " + target.thingIDNumber +
                        " at site " + site.ID + ".");
                }
                else
                {
                    if (!target.Destroyed)
                        target.Destroy(DestroyMode.Vanish);
                    Log.Warning("[LiAIChat Blightfall] Could not place target for " + node.Type +
                        " at site " + site.ID + ".");
                }
            }
        }

        private static string TargetDefName(BlightfallPollutionNodeType type, BlightfallSiteType siteType)
        {
            if (siteType == BlightfallSiteType.SporeForest && type == BlightfallPollutionNodeType.SporeTower)
                return "LiAIChat_BlightfallSporeMotherTree";
            if (siteType == BlightfallSiteType.ToxicHive && type == BlightfallPollutionNodeType.BroodNest)
                return "LiAIChat_BlightfallToxicHiveObjective";
            if (siteType == BlightfallSiteType.AncientProcessingPlant && type == BlightfallPollutionNodeType.ResonanceSpire)
                return "LiAIChat_BlightfallProcessingPlantObjective";
            if (siteType == BlightfallSiteType.ResonanceDefense && type == BlightfallPollutionNodeType.ResonanceSpire)
                return "LiAIChat_BlightfallResonanceSpire";
            switch (type)
            {
                case BlightfallPollutionNodeType.SporeTower: return "LiAIChat_BlightfallSporeTower";
                case BlightfallPollutionNodeType.BroodNest: return "LiAIChat_BlightfallBroodNest";
                case BlightfallPollutionNodeType.ResonanceSpire: return "LiAIChat_BlightfallResonanceSpire";
                default: return null;
            }
        }
    }

    public sealed class Building_BlightfallResonanceSpire : Building
    {
        public override string GetInspectString()
        {
            string baseText = base.GetInspectString();
            string shield = "LiAIChat_BlightfallResonanceShieldActive".Translate();
            return string.IsNullOrEmpty(baseText) ? shield : baseText + "\n" + shield;
        }
    }

    public static class BlightfallResonanceDefenseUtility
    {
        private const int ShieldRadius = 15;

        public static void Initialize(Map map)
        {
            Site site = map?.Parent as Site;
            WorldComponent_Blightfall campaign = WorldComponent_Blightfall.Current;
            BlightfallSiteState state = campaign?.GetSiteState(site);
            if (map == null || state == null || state.Type != BlightfallSiteType.ResonanceDefense ||
                state.Status == BlightfallSiteStatus.Completed || state.Status == BlightfallSiteStatus.Removed ||
                state.DefenseSetupComplete)
                return;

            Thing spire = state.ObjectiveThingId < 0 ? null : map.listerThings.AllThings.FirstOrDefault(thing =>
                thing.thingIDNumber == state.ObjectiveThingId && thing.Spawned && !thing.Destroyed);
            if (spire == null)
            {
                Log.Warning("[LiAIChat Blightfall] Could not initialize resonance defenses; the spire target is missing at site " +
                    site.ID + ".");
                return;
            }

            FactionDef insectDef = DefDatabase<FactionDef>.GetNamedSilentFail("Insect");
            Faction insectFaction = insectDef == null ? null : Find.FactionManager.FirstFactionOfDef(insectDef);
            if (insectFaction == null)
            {
                Log.ErrorOnce("[LiAIChat Blightfall] The Insect faction is missing; resonance defenses cannot generate defenders.",
                    "LiAIChat_Blightfall_ResonanceInsectFactionMissing".GetHashCode());
            }
            else
            {
                SpawnDefenders(map, site, state, insectFaction);
                SpawnDefenseBuildings(map, spire.Position, insectFaction);
            }

            state.DefenseSetupComplete = true;
            Log.Message("[LiAIChat Blightfall] Initialized resonance defense site " + site.ID +
                " at tile " + state.TileId + ".");
        }

        private static void SpawnDefenders(Map map, Site site, BlightfallSiteState state, Faction faction)
        {
            List<PawnKindDef> kinds = new List<PawnKindDef>
            {
                DefDatabase<PawnKindDef>.GetNamedSilentFail("LiAIChat_BlightCrawler"),
                DefDatabase<PawnKindDef>.GetNamedSilentFail("LiAIChat_SporeSpitter"),
                DefDatabase<PawnKindDef>.GetNamedSilentFail("LiAIChat_BlightBehemoth")
            };
            kinds.RemoveAll(kind => kind == null);
            if (kinds.Count == 0)
            {
                Log.ErrorOnce("[LiAIChat Blightfall] No Blightfall defender PawnKinds are available.",
                    "LiAIChat_Blightfall_ResonanceKindsMissing".GetHashCode());
                return;
            }

            int colonists = PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive
                .Count(pawn => pawn != null && pawn.IsColonistPlayerControlled);
            int count = Mathf.Clamp(10 + Mathf.RoundToInt(state.Difficulty / 12f) + colonists / 2, 12, 32);
            List<Pawn> defenders = new List<Pawn>(count);
            for (int i = 0; i < count; i++)
            {
                string preferred = i % 7 == 0 ? "LiAIChat_BlightBehemoth" :
                    i % 3 == 0 ? "LiAIChat_SporeSpitter" : "LiAIChat_BlightCrawler";
                PawnKindDef kind = kinds.FirstOrDefault(candidate => candidate.defName == preferred) ?? kinds.RandomElement();
                Pawn pawn = PawnGenerator.GeneratePawn(kind, faction, new PlanetTile(site.Tile.tileId));
                if (pawn == null)
                    continue;
                IntVec3 root = CellFinder.RandomEdgeCell(map);
                IntVec3 cell = CellFinder.RandomSpawnCellForPawnNear(root, map, 10);
                GenSpawn.Spawn(pawn, cell, map);
                if (pawn.Spawned)
                    defenders.Add(pawn);
                else if (!pawn.Destroyed)
                    pawn.Destroy(DestroyMode.Vanish);
            }
            if (defenders.Count > 0)
            {
                LordMaker.MakeNewLord(faction,
                    new LordJob_AssaultColony(faction, false, false, false, false, false, false, false), map, defenders);
                Log.Message("[LiAIChat Blightfall] Spawned " + defenders.Count +
                    " defenders for resonance site " + site.ID + ".");
            }
        }

        private static void SpawnDefenseBuildings(Map map, IntVec3 center, Faction faction)
        {
            ThingDef turretDef = DefDatabase<ThingDef>.GetNamedSilentFail("Turret_MiniTurret");
            if (turretDef != null)
            {
                foreach (int radius in new[] { 9, 12 })
                {
                    Thing turret = ThingMaker.MakeThing(turretDef);
                    turret.SetFaction(faction);
                    IntVec3 cell = CellFinder.RandomClosewalkCellNear(center, map, radius);
                    if (!GenPlace.TryPlaceThing(turret, cell, map, ThingPlaceMode.Near) || !turret.Spawned)
                    {
                        if (!turret.Destroyed)
                            turret.Destroy(DestroyMode.Vanish);
                    }
                }
            }

            ThingDef sandbagDef = DefDatabase<ThingDef>.GetNamedSilentFail("Sandbags");
            if (sandbagDef == null)
                return;
            int placed = 0;
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, 8f, false).InRandomOrder())
            {
                if (placed >= 12)
                    break;
                if (!cell.InBounds(map) || !cell.Standable(map) || cell.GetEdifice(map) != null)
                    continue;
                Thing sandbag = ThingMaker.MakeThing(sandbagDef);
                sandbag.SetFaction(faction);
                if (GenPlace.TryPlaceThing(sandbag, cell, map, ThingPlaceMode.Direct) && sandbag.Spawned)
                    placed++;
                else if (!sandbag.Destroyed)
                    sandbag.Destroy(DestroyMode.Vanish);
            }
        }

        public static bool IsShieldActiveFor(Thing thing, out Thing spire)
        {
            spire = null;
            Site site = thing?.Map?.Parent as Site;
            WorldComponent_Blightfall campaign = WorldComponent_Blightfall.Current;
            BlightfallSiteState state = campaign?.GetSiteState(site);
            BlightfallPollutionNodeState node = state == null ? null : campaign.GetNode(
                BlightfallPollutionNodeType.ResonanceSpire, state.TileId);
            if (thing == null || state?.Type != BlightfallSiteType.ResonanceDefense ||
                node?.Status != BlightfallPollutionNodeStatus.Active || state.ObjectiveThingId < 0)
                return false;
            spire = thing.Map.listerThings.AllThings.FirstOrDefault(candidate =>
                candidate.thingIDNumber == state.ObjectiveThingId && candidate.Spawned && !candidate.Destroyed);
            if (spire == null)
                return false;
            int dx = thing.Position.x - spire.Position.x;
            int dz = thing.Position.z - spire.Position.z;
            return dx * dx + dz * dz <= ShieldRadius * ShieldRadius;
        }

        public static bool IsPoweredDefenseTurret(Building_TurretGun turret)
        {
            if (turret?.Faction?.def?.defName != "Insect")
                return false;
            Thing ignored;
            return IsShieldActiveFor(turret, out ignored);
        }
    }

    [HarmonyPatch(typeof(Thing), nameof(Thing.TakeDamage))]
    public static class BlightfallResonanceShieldDamagePatch
    {
        public static void Prefix(Thing __instance, ref DamageInfo dinfo)
        {
            Thing attacker = dinfo.Instigator;
            if (attacker?.Faction == null ||
                (attacker.Faction != Faction.OfPlayer &&
                 attacker.Faction.RelationKindWith(Faction.OfPlayer) != FactionRelationKind.Ally))
                return;
            Thing spire;
            if (BlightfallResonanceDefenseUtility.IsShieldActiveFor(__instance, out spire))
                dinfo.SetAmount(dinfo.Amount * 0.45f);
        }
    }

    [HarmonyPatch(typeof(CompPowerTrader), "get_PowerOn")]
    public static class BlightfallResonanceTurretPowerPatch
    {
        public static bool Prefix(CompPowerTrader __instance, ref bool __result)
        {
            Building_TurretGun turret = __instance?.parent as Building_TurretGun;
            if (!BlightfallResonanceDefenseUtility.IsPoweredDefenseTurret(turret))
                return true;
            __result = true;
            return false;
        }
    }

    public sealed class Building_BlightfallProcessor : Building
    {
        private bool shutdown;

        public bool Shutdown => shutdown;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref shutdown, "blightfallProcessorShutdown", false);
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
                yield return gizmo;

            if (!shutdown)
            {
                yield return new Command_Action
                {
                    defaultLabel = "LiAIChat_BlightfallProcessorShutdown".Translate(),
                    defaultDesc = "LiAIChat_BlightfallProcessorToggleDesc".Translate(),
                    action = () =>
                    {
                        shutdown = true;
                        BlightfallSpecialSiteUtility.NotifyProcessorShutdown(Map);
                    }
                };
                }
        }

        public override string GetInspectString()
        {
            string status = shutdown ? "LiAIChat_BlightfallProcessorStatusOff".Translate() : "LiAIChat_BlightfallProcessorStatusOn".Translate();
            string baseText = base.GetInspectString();
            return string.IsNullOrEmpty(baseText) ? status : baseText + "\n" + status;
        }
    }

    public static class BlightfallSpecialSiteUtility
    {
        private const int TicksPerPulse = 6000;
        private const int HiveSpawnInterval = 9000;

        public static void Initialize(Map map)
        {
            Site site = map?.Parent as Site;
            WorldComponent_Blightfall world = WorldComponent_Blightfall.Current;
            BlightfallSiteState state = world?.GetSiteState(site);
            if (map == null || state == null || state.Type == BlightfallSiteType.ContaminatedRuin ||
                state.Type == BlightfallSiteType.BlightHeart ||
                state.Status == BlightfallSiteStatus.Completed || state.Status == BlightfallSiteStatus.Removed)
                return;

            if (state.ObjectiveThingId < 0)
            {
                BlightfallPollutionNodeType expectedNode = NodeTypeFor(state.Type);
                BlightfallPollutionNodeState node = expectedNode == (BlightfallPollutionNodeType)(-1) ? null :
                    world.GetNodesAtTile(state.TileId).FirstOrDefault(item => item.Type == expectedNode &&
                        item.Status == BlightfallPollutionNodeStatus.Active && item.TargetThingId >= 0);
                Thing existingTarget = node == null ? null : map.listerThings.AllThings.FirstOrDefault(thing =>
                    thing.thingIDNumber == node.TargetThingId && thing.Spawned && !thing.Destroyed);
                if (existingTarget != null)
                {
                    world.RegisterSpecialSiteObjective(site, existingTarget.thingIDNumber,
                        map.pollutionGrid?.TotalPollution ?? 0);
                }
                else
                {
                    string defName = ObjectiveDefFor(state.Type);
                    ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
                    if (def == null)
                    {
                        Log.Error("[LiAIChat Blightfall] Missing special-site objective " + defName + ".");
                        return;
                    }
                    Thing objective = ThingMaker.MakeThing(def);
                    if (!GenPlace.TryPlaceThing(objective, map.Center, map, ThingPlaceMode.Near) || !objective.Spawned ||
                        !world.RegisterSpecialSiteObjective(site, objective.thingIDNumber,
                            map.pollutionGrid?.TotalPollution ?? 0))
                    {
                        if (!objective.Destroyed)
                            objective.Destroy(DestroyMode.Vanish);
                        Log.Error("[LiAIChat Blightfall] Could not place objective for " + state.Type + " at site " + site.ID + ".");
                        return;
                    }
                    Log.Message("[LiAIChat Blightfall] Spawned " + state.Type + " objective " + objective.thingIDNumber +
                        " at site " + site.ID + ".");
                }
            }
            else if (state.InitialPollutionCount < 0)
            {
                state.InitialPollutionCount = map.pollutionGrid?.TotalPollution ?? 0;
            }
        }

        public static void NotifyProcessorShutdown(Map map)
        {
            Site site = map?.Parent as Site;
            WorldComponent_Blightfall world = WorldComponent_Blightfall.Current;
            BlightfallSiteState state = world?.GetSiteState(site);
            if (state?.Type == BlightfallSiteType.AncientProcessingPlant)
            {
                world.NotifyNodeTargetDisabled(state.TileId, state.ObjectiveThingId);
                world.CompleteSpecialSite(site);
            }
        }

        public static void NotifyObjectiveDestroyed(Site site, int thingId)
        {
            WorldComponent_Blightfall world = WorldComponent_Blightfall.Current;
            BlightfallSiteState state = world?.GetSiteState(site);
            if (state == null || !world.NotifySpecialSiteObjectiveDestroyed(site, thingId))
                return;
            if (state.Type == BlightfallSiteType.SporeForest ||
                state.Type == BlightfallSiteType.AncientProcessingPlant ||
                state.Type == BlightfallSiteType.ResonanceDefense)
                world.CompleteSpecialSite(site);
        }

        public static void Tick(Map map)
        {
            if (map == null || map.IsPocketMap || Find.TickManager == null || Find.TickManager.TicksGame % 250 != 0)
                return;
            Site site = map.Parent as Site;
            WorldComponent_Blightfall world = WorldComponent_Blightfall.Current;
            BlightfallSiteState state = world?.GetSiteState(site);
            if (state == null || world.CampaignState == BlightfallCampaignState.Completed ||
                state.Type == BlightfallSiteType.ContaminatedRuin ||
                state.Status == BlightfallSiteStatus.Completed || state.Status == BlightfallSiteStatus.Removed)
                return;

            int now = Find.TickManager.TicksGame;
            Thing objective = state.ObjectiveThingId < 0 ? null : map.listerThings.AllThings.FirstOrDefault(
                thing => thing.thingIDNumber == state.ObjectiveThingId && thing.Spawned && !thing.Destroyed);
            bool processorOff = (objective as Building_BlightfallProcessor)?.Shutdown ?? false;
            if (state.ObjectiveDestroyed || processorOff)
            {
                if (state.Type != BlightfallSiteType.ToxicHive || HiveCleanupComplete(map, state))
                    world.CompleteSpecialSite(site);
                return;
            }

            if (objective == null || now < state.NextEffectTick)
                return;

            switch (state.Type)
            {
                case BlightfallSiteType.SporeForest:
                    AddPollution(map, 0.005f);
                    state.NextEffectTick = now + TicksPerPulse;
                    break;
                case BlightfallSiteType.ToxicHive:
                    SpawnHiveDefender(map);
                    state.NextEffectTick = now + HiveSpawnInterval;
                    break;
                case BlightfallSiteType.AncientProcessingPlant:
                    AddPollution(map, 0.003f);
                    state.NextEffectTick = now + TicksPerPulse;
                    break;
            }
            state.LastUpdatedTick = now;
        }

        private static bool HiveCleanupComplete(Map map, BlightfallSiteState state)
        {
            int initial = Math.Max(0, state.InitialPollutionCount);
            int remaining = map.pollutionGrid?.TotalPollution ?? 0;
            return initial == 0 || remaining <= Mathf.FloorToInt(initial * 0.2f);
        }

        private static void AddPollution(Map map, float amount)
        {
            if (map?.pollutionGrid == null)
                return;
            float current = (float)map.pollutionGrid.TotalPollution / Mathf.Max(1, map.cellIndices.NumGridCells);
            PollutionUtility.PolluteMapToPercent(map, Mathf.Clamp01(current + amount), Rand.RangeInclusive(5, 8));
        }

        private static void SpawnHiveDefender(Map map)
        {
            FactionDef insectDef = DefDatabase<FactionDef>.GetNamedSilentFail("Insect");
            Faction faction = insectDef == null ? null : Find.FactionManager.FirstFactionOfDef(insectDef);
            if (faction == null || map.mapPawns.AllPawnsSpawned.Count(pawn => pawn.Faction == faction && !pawn.Dead) >= 8)
                return;
            PawnKindDef kind = DefDatabase<PawnKindDef>.GetNamedSilentFail("LiAIChat_BlightCrawler") ??
                               DefDatabase<PawnKindDef>.GetNamedSilentFail("Megascarab");
            if (kind == null)
                return;
            Pawn pawn = PawnGenerator.GeneratePawn(kind, faction);
            if (pawn == null)
                return;
            IntVec3 cell = CellFinder.RandomSpawnCellForPawnNear(map.Center, map, 12);
            GenSpawn.Spawn(pawn, cell, map);
            LordMaker.MakeNewLord(faction,
                new LordJob_AssaultColony(faction, false, false, false, false, false, false, false), map,
                new List<Pawn> { pawn });
        }

        private static string ObjectiveDefFor(BlightfallSiteType type)
        {
            switch (type)
            {
                case BlightfallSiteType.SporeForest: return "LiAIChat_BlightfallSporeMotherTree";
                case BlightfallSiteType.ToxicHive: return "LiAIChat_BlightfallToxicHiveObjective";
                case BlightfallSiteType.AncientProcessingPlant: return "LiAIChat_BlightfallProcessingPlantObjective";
                case BlightfallSiteType.ResonanceDefense: return "LiAIChat_BlightfallResonanceSpire";
                default: return null;
            }
        }

        private static BlightfallPollutionNodeType NodeTypeFor(BlightfallSiteType type)
        {
            switch (type)
            {
                case BlightfallSiteType.SporeForest: return BlightfallPollutionNodeType.SporeTower;
                case BlightfallSiteType.ToxicHive: return BlightfallPollutionNodeType.BroodNest;
                case BlightfallSiteType.AncientProcessingPlant: return BlightfallPollutionNodeType.ResonanceSpire;
                case BlightfallSiteType.ResonanceDefense: return BlightfallPollutionNodeType.ResonanceSpire;
                default: return (BlightfallPollutionNodeType)(-1);
            }
        }
    }

    public sealed class MapComponent_BlightfallSpecialSite : MapComponent
    {
        public MapComponent_BlightfallSpecialSite(Map map) : base(map) { }
        public override void MapComponentTick()
        {
            BlightfallSpecialSiteUtility.Tick(map);
            BlightfallFinalCoreUtility.Tick(map);
        }
    }

    [HarmonyPatch(typeof(Thing), nameof(Thing.Destroy), new Type[] { typeof(DestroyMode) })]
    internal static class BlightfallNodeTargetDestroyedPatch
    {
        private static void Prefix(Thing __instance)
        {
            if (__instance == null || !__instance.Spawned)
                return;

            Site site = __instance.Map?.Parent as Site;
            if (site == null)
                return;

            WorldComponent_Blightfall component = WorldComponent_Blightfall.Current;
            BlightfallSiteState siteState = component?.GetSiteState(site);
            if (siteState != null)
            {
                if (siteState.Type == BlightfallSiteType.BlightHeart)
                {
                    BlightfallFinalCoreUtility.NotifyThingDestroyed(site, __instance.thingIDNumber);
                    return;
                }
                component.NotifyNodeTargetDestroyed(siteState.TileId, __instance.thingIDNumber);
                BlightfallSpecialSiteUtility.NotifyObjectiveDestroyed(site, __instance.thingIDNumber);
            }
        }
    }

    [HarmonyPatch(typeof(Site), nameof(Site.Destroy))]
    internal static class BlightfallSiteCompletionPatch
    {
        private static void Prefix(Site __instance)
        {
            WorldComponent_Blightfall component = WorldComponent_Blightfall.Current;
            BlightfallSiteState state = component?.GetSiteState(__instance);
            if (state != null && state.Status != BlightfallSiteStatus.Completed)
                component.UpdateSiteStatus(__instance, BlightfallSiteStatus.Removed);
        }
    }
}
