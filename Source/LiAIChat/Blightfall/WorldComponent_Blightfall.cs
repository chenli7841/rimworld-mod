using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace LiAIChat.Blightfall
{
    public enum BlightfallCampaignState
    {
        Dormant,
        Active,
        Contained,
        CoreExposed,
        Completed
    }

    public enum BlightfallPollutionNodeStatus
    {
        Dormant,
        Active,
        Suppressed,
        Cleansed,
        Destroyed
    }

    public enum BlightfallPollutionNodeType
    {
        SporeTower,
        BroodNest,
        ResonanceSpire
    }

    public enum BlightfallSiteType
    {
        ContaminatedRuin,
        SporeForest,
        ToxicHive,
        AncientProcessingPlant,
        ResonanceDefense,
        BlightHeart,
        CarrionPlague
    }

    public enum BlightfallSiteStatus
    {
        Created,
        MapGenerated,
        Visited,
        Completed,
        Removed
    }

    public enum BlightfallCoreBattleStage
    {
        NotStarted,
        PerimeterBreach,
        DestroyCoreDefenses,
        RepelReinforcements,
        WeakenHeart,
        FinalPurification,
        Completed
    }

    public enum BlightfallCampaignEnding
    {
        None,
        DestroyHeart,
        PurifyHeart,
        HarnessHeart
    }

    public enum BlightfallTileStatus
    {
        Frontline,
        Interior,
        Core,
        Purified
    }

    public sealed class BlightfallTileState : IExposable
    {
        public int TileId = -1;
        public BlightfallTileStatus Status = BlightfallTileStatus.Interior;

        public void ExposeData()
        {
            Scribe_Values.Look(ref TileId, "tileId", -1);
            Scribe_Values.Look(ref Status, "status", BlightfallTileStatus.Interior);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (!Enum.IsDefined(typeof(BlightfallTileStatus), Status))
                    Status = BlightfallTileStatus.Interior;
                if (TileId < -1)
                    TileId = -1;
            }
        }
    }

    public sealed class BlightfallCarrionAnimalRecord : IExposable
    {
        public int PawnThingId = -1;
        public int CorpseThingId = -1;
        public bool Dead;
        public bool Burned;

        public void ExposeData()
        {
            Scribe_Values.Look(ref PawnThingId, "pawnThingId", -1);
            Scribe_Values.Look(ref CorpseThingId, "corpseThingId", -1);
            Scribe_Values.Look(ref Dead, "dead", false);
            Scribe_Values.Look(ref Burned, "burned", false);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (PawnThingId < 0) PawnThingId = -1;
                if (CorpseThingId < 0) CorpseThingId = -1;
                if (Burned) Dead = true;
            }
        }
    }

    public sealed class BlightfallSiteState : IExposable
    {
        public int SiteId = -1;
        public int TileId = -1;
        public BlightfallSiteType Type = BlightfallSiteType.ContaminatedRuin;
        public float Difficulty;
        public BlightfallSiteStatus Status = BlightfallSiteStatus.Created;
        public int LastUpdatedTick = -1;
        public int ObjectiveThingId = -1;
        public bool ObjectiveDestroyed;
        public bool DefenseSetupComplete;
        public int InitialPollutionCount = -1;
        public int NextEffectTick = -1;
        public bool AlliedExpeditionRequested;
        public bool AlliedExpeditionConsumed;
        public int AlliedExpeditionRequestTick = -1;
        public Faction AlliedExpeditionFaction;
        public BlightfallCoreBattleStage CoreBattleStage = BlightfallCoreBattleStage.NotStarted;
        public int CorePerimeterTargetsRemaining;
        public int CoreDefenseTargetsRemaining;
        public int CoreReinforcementWavesSpawned;
        public int CoreNextReinforcementTick = -1;
        public int CoreNextEnvironmentalPulseTick = -1;
        public float CoreHeartHealthPercent = 1f;
        public int CoreHeartThingId = -1;
        public List<int> CorePerimeterTargetThingIds = new List<int>();
        public List<int> CoreDefenseTargetThingIds = new List<int>();
        public int CarrionPlagueWaveDueTick = -1;
        public bool CarrionPlagueWaveSpawned;
        public bool CarrionPlagueWarningSent;
        public bool CarrionPlagueResolved;
        public List<BlightfallCarrionAnimalRecord> CarrionAnimals = new List<BlightfallCarrionAnimalRecord>();

        public void ExposeData()
        {
            Scribe_Values.Look(ref SiteId, "siteId", -1);
            Scribe_Values.Look(ref TileId, "tileId", -1);
            Scribe_Values.Look(ref Type, "type", BlightfallSiteType.ContaminatedRuin);
            Scribe_Values.Look(ref Difficulty, "difficulty", 0f);
            Scribe_Values.Look(ref Status, "status", BlightfallSiteStatus.Created);
            Scribe_Values.Look(ref LastUpdatedTick, "lastUpdatedTick", -1);
            Scribe_Values.Look(ref ObjectiveThingId, "objectiveThingId", -1);
            Scribe_Values.Look(ref ObjectiveDestroyed, "objectiveDestroyed", false);
            Scribe_Values.Look(ref DefenseSetupComplete, "defenseSetupComplete", false);
            Scribe_Values.Look(ref InitialPollutionCount, "initialPollutionCount", -1);
            Scribe_Values.Look(ref NextEffectTick, "nextEffectTick", -1);
            Scribe_Values.Look(ref AlliedExpeditionRequested, "alliedExpeditionRequested", false);
            Scribe_Values.Look(ref AlliedExpeditionConsumed, "alliedExpeditionConsumed", false);
            Scribe_Values.Look(ref AlliedExpeditionRequestTick, "alliedExpeditionRequestTick", -1);
            Scribe_References.Look(ref AlliedExpeditionFaction, "alliedExpeditionFaction");
            Scribe_Values.Look(ref CoreBattleStage, "coreBattleStage", BlightfallCoreBattleStage.NotStarted);
            Scribe_Values.Look(ref CorePerimeterTargetsRemaining, "corePerimeterTargetsRemaining", 0);
            Scribe_Values.Look(ref CoreDefenseTargetsRemaining, "coreDefenseTargetsRemaining", 0);
            Scribe_Values.Look(ref CoreReinforcementWavesSpawned, "coreReinforcementWavesSpawned", 0);
            Scribe_Values.Look(ref CoreNextReinforcementTick, "coreNextReinforcementTick", -1);
            Scribe_Values.Look(ref CoreNextEnvironmentalPulseTick, "coreNextEnvironmentalPulseTick", -1);
            Scribe_Values.Look(ref CoreHeartHealthPercent, "coreHeartHealthPercent", 1f);
            Scribe_Values.Look(ref CoreHeartThingId, "coreHeartThingId", -1);
            Scribe_Collections.Look(ref CorePerimeterTargetThingIds, "corePerimeterTargetThingIds", LookMode.Value);
            Scribe_Collections.Look(ref CoreDefenseTargetThingIds, "coreDefenseTargetThingIds", LookMode.Value);
            Scribe_Values.Look(ref CarrionPlagueWaveDueTick, "carrionPlagueWaveDueTick", -1);
            Scribe_Values.Look(ref CarrionPlagueWaveSpawned, "carrionPlagueWaveSpawned", false);
            Scribe_Values.Look(ref CarrionPlagueWarningSent, "carrionPlagueWarningSent", false);
            Scribe_Values.Look(ref CarrionPlagueResolved, "carrionPlagueResolved", false);
            Scribe_Collections.Look(ref CarrionAnimals, "carrionAnimals", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
                Normalize();
        }

        public void Normalize()
        {
            if (!Enum.IsDefined(typeof(BlightfallSiteType), Type))
                Type = BlightfallSiteType.ContaminatedRuin;
            if (!Enum.IsDefined(typeof(BlightfallSiteStatus), Status))
                Status = BlightfallSiteStatus.Created;
            Difficulty = BlightfallCampaignUtility.ClampPercent(Difficulty);
            if (SiteId < -1) SiteId = -1;
            if (TileId < -1) TileId = -1;
            if (LastUpdatedTick < -1) LastUpdatedTick = -1;
            if (ObjectiveThingId < -1) ObjectiveThingId = -1;
            if (InitialPollutionCount < -1) InitialPollutionCount = -1;
            if (NextEffectTick < -1) NextEffectTick = -1;
            if (AlliedExpeditionRequestTick < -1) AlliedExpeditionRequestTick = -1;
            if (!Enum.IsDefined(typeof(BlightfallCoreBattleStage), CoreBattleStage))
                CoreBattleStage = BlightfallCoreBattleStage.NotStarted;
            CorePerimeterTargetsRemaining = Math.Max(0, Math.Min(3, CorePerimeterTargetsRemaining));
            CoreDefenseTargetsRemaining = Math.Max(0, Math.Min(2, CoreDefenseTargetsRemaining));
            CoreReinforcementWavesSpawned = Math.Max(0, Math.Min(3, CoreReinforcementWavesSpawned));
            if (CoreNextReinforcementTick < -1) CoreNextReinforcementTick = -1;
            if (CoreNextEnvironmentalPulseTick < -1) CoreNextEnvironmentalPulseTick = -1;
            if (float.IsNaN(CoreHeartHealthPercent) || float.IsInfinity(CoreHeartHealthPercent))
                CoreHeartHealthPercent = 1f;
            CoreHeartHealthPercent = Math.Max(0.01f, Math.Min(1f, CoreHeartHealthPercent));
            if (CoreHeartThingId < -1) CoreHeartThingId = -1;
            CorePerimeterTargetThingIds = CorePerimeterTargetThingIds ?? new List<int>();
            CoreDefenseTargetThingIds = CoreDefenseTargetThingIds ?? new List<int>();
            CorePerimeterTargetThingIds.RemoveAll(id => id < 0);
            CoreDefenseTargetThingIds.RemoveAll(id => id < 0);
            if (CarrionPlagueWaveDueTick < -1) CarrionPlagueWaveDueTick = -1;
            CarrionAnimals = CarrionAnimals ?? new List<BlightfallCarrionAnimalRecord>();
            CarrionAnimals.RemoveAll(record => record == null || record.PawnThingId < 0);
        }
    }

    public sealed class BlightfallPollutionNodeState : IExposable
    {
        public int TileId = -1;
        public BlightfallPollutionNodeType Type = BlightfallPollutionNodeType.SporeTower;
        public BlightfallPollutionNodeStatus Status = BlightfallPollutionNodeStatus.Dormant;
        public float Activity;
        public float Suppression;
        public int LastUpdatedTick = -1;
        public int TargetThingId = -1;

        public void ExposeData()
        {
            Scribe_Values.Look(ref TileId, "tileId", -1);
            Scribe_Values.Look(ref Type, "type", BlightfallPollutionNodeType.SporeTower);
            Scribe_Values.Look(ref Status, "status", BlightfallPollutionNodeStatus.Dormant);
            Scribe_Values.Look(ref Activity, "activity", 0f);
            Scribe_Values.Look(ref Suppression, "suppression", 0f);
            Scribe_Values.Look(ref LastUpdatedTick, "lastUpdatedTick", -1);
            Scribe_Values.Look(ref TargetThingId, "targetThingId", -1);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
                Normalize();
        }

        public void Normalize()
        {
            if (!Enum.IsDefined(typeof(BlightfallPollutionNodeType), Type))
                Type = BlightfallPollutionNodeType.SporeTower;
            if (!Enum.IsDefined(typeof(BlightfallPollutionNodeStatus), Status))
                Status = BlightfallPollutionNodeStatus.Dormant;
            Activity = BlightfallCampaignUtility.ClampPercent(Activity);
            Suppression = BlightfallCampaignUtility.ClampPercent(Suppression);
            if (LastUpdatedTick < -1)
                LastUpdatedTick = -1;
            if (TargetThingId < -1)
                TargetThingId = -1;
        }
    }

    public static class BlightfallCampaignUtility
    {
        public static float ClampPercent(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
                return 0f;
            return Math.Max(0f, Math.Min(100f, value));
        }
    }

    /// <summary>
    /// Persistent world-level state for The Blightfall campaign.
    /// </summary>
    public sealed class WorldComponent_Blightfall : WorldComponent
    {
        private BlightfallCampaignState campaignState = BlightfallCampaignState.Dormant;
        private BlightfallCampaignEnding ending = BlightfallCampaignEnding.None;
        private int sourceCenterTileId = -1;
        private List<int> pollutedTileIds = new List<int>();
        private List<int> purifiedTileIds = new List<int>();
        private List<int> protectedTileIds = new List<int>();
        private List<BlightfallTileState> tileStates = new List<BlightfallTileState>();
        private List<BlightfallPollutionNodeState> pollutionNodes = new List<BlightfallPollutionNodeState>();
        private List<BlightfallSiteState> siteStates = new List<BlightfallSiteState>();
        private float sourceActivity;
        private float suppression;
        private int campaignStartTick = -1;
        private int lastWorldSimulationTick = -1;
        private int nextSpreadTick = -1;
        private int nextRaidTick = -1;
        private bool arrivalEventTriggered;
        private int nextEndingEffectTick = -1;
        private int harnessFailures;
        private Material pollutedTileMaterial;
        private Material sourceCenterMaterial;
        private Material frontlineTileMaterial;
        private Material interiorTileMaterial;
        private Material coreTileMaterial;
        private Material purifiedTileMaterial;

        public const int ArrivalAfterDays = 120;
        private const int TicksPerDay = 60000;
        public const int MaxPollutedTileCount = 80;
        public const int RequiredResonanceSpireCount = 3;
        private const int MinSpreadIntervalDays = 3;
        private const int MaxSpreadIntervalDays = 5;
        private const int MaxNewTilesPerSpread = 3;
        public const int MinimumRaidIntervalDays = 7;
        public const int MaximumRaidIntervalDays = 15;

        public static WorldComponent_Blightfall Current =>
            Find.World?.GetComponent<WorldComponent_Blightfall>();

        public BlightfallCampaignState CampaignState => campaignState;
        public BlightfallCampaignEnding Ending => ending;
        public int SourceCenterTileId => sourceCenterTileId;
        public IReadOnlyList<int> PollutedTileIds => pollutedTileIds;
        public IReadOnlyList<int> PurifiedTileIds => purifiedTileIds;
        public IReadOnlyList<int> ProtectedTileIds => protectedTileIds;
        public IReadOnlyList<BlightfallTileState> TileStates => tileStates;
        public IReadOnlyList<BlightfallPollutionNodeState> PollutionNodes => pollutionNodes;
        public IReadOnlyList<BlightfallSiteState> SiteStates => siteStates;
        public float SourceActivity => sourceActivity;
        public float Suppression => suppression;
        public int CampaignStartTick => campaignStartTick;
        public int LastWorldSimulationTick => lastWorldSimulationTick;
        public int NextSpreadTick => nextSpreadTick;
        public int NextRaidTick => nextRaidTick;
        public bool ArrivalEventTriggered => arrivalEventTriggered;
        public int ActiveBroodNestCount => pollutionNodes.Count(node => node != null &&
            node.Type == BlightfallPollutionNodeType.BroodNest && node.Status == BlightfallPollutionNodeStatus.Active);
        public int DestroyedResonanceSpireCount => pollutionNodes.Count(node => node != null &&
            node.Type == BlightfallPollutionNodeType.ResonanceSpire && node.TileId != sourceCenterTileId &&
            node.Status == BlightfallPollutionNodeStatus.Destroyed);
        public int RemainingResonanceSpireCount => Math.Max(0,
            RequiredResonanceSpireCount - DestroyedResonanceSpireCount);
        public float CoreProtectionStrength => RemainingResonanceSpireCount *
            (100f / RequiredResonanceSpireCount);
        public bool CoreProtected => RemainingResonanceSpireCount > 0;

        public WorldComponent_Blightfall(World world) : base(world)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref campaignState, "blightfallCampaignState", BlightfallCampaignState.Dormant);
            Scribe_Values.Look(ref ending, "blightfallEnding", BlightfallCampaignEnding.None);
            Scribe_Values.Look(ref sourceCenterTileId, "blightfallSourceCenterTileId", -1);
            Scribe_Collections.Look(ref pollutedTileIds, "blightfallPollutedTileIds", LookMode.Value);
            Scribe_Collections.Look(ref purifiedTileIds, "blightfallPurifiedTileIds", LookMode.Value);
            Scribe_Collections.Look(ref protectedTileIds, "blightfallProtectedTileIds", LookMode.Value);
            Scribe_Collections.Look(ref tileStates, "blightfallTileStates", LookMode.Deep);
            Scribe_Collections.Look(ref pollutionNodes, "blightfallPollutionNodes", LookMode.Deep);
            Scribe_Collections.Look(ref siteStates, "blightfallSiteStates", LookMode.Deep);
            Scribe_Values.Look(ref sourceActivity, "blightfallSourceActivity", 0f);
            Scribe_Values.Look(ref suppression, "blightfallSuppression", 0f);
            Scribe_Values.Look(ref campaignStartTick, "blightfallCampaignStartTick", -1);
            Scribe_Values.Look(ref lastWorldSimulationTick, "blightfallLastWorldSimulationTick", -1);
            Scribe_Values.Look(ref nextSpreadTick, "blightfallNextSpreadTick", -1);
            Scribe_Values.Look(ref nextRaidTick, "blightfallNextRaidTick", -1);
            Scribe_Values.Look(ref arrivalEventTriggered, "blightfallArrivalEventTriggered", false);
            Scribe_Values.Look(ref nextEndingEffectTick, "blightfallNextEndingEffectTick", -1);
            Scribe_Values.Look(ref harnessFailures, "blightfallHarnessFailures", 0);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
                NormalizeAfterLoad();
        }

        /// <summary>Starts a fresh campaign state without creating any world objects.</summary>
        public bool StartCampaign(int sourceTileId, int startTick = -1)
        {
            if (sourceTileId < 0)
                return false;

            int now = CurrentTick();
            campaignState = BlightfallCampaignState.Active;
            ending = BlightfallCampaignEnding.None;
            sourceCenterTileId = sourceTileId;
            pollutedTileIds.Clear();
            purifiedTileIds.Clear();
            protectedTileIds.Clear();
            tileStates.Clear();
            pollutionNodes.Clear();
            siteStates.Clear();
            sourceActivity = 0f;
            suppression = 0f;
            nextEndingEffectTick = -1;
            harnessFailures = 0;
            campaignStartTick = startTick >= 0 ? startTick : now;
            lastWorldSimulationTick = campaignStartTick;
            ScheduleNextSpread(now);
            ScheduleNextRaid(now);
            RefreshTileStatuses();
            return true;
        }

        public override void WorldComponentTick()
        {
            base.WorldComponentTick();
            int now = Find.TickManager?.TicksGame ?? 0;
            if (now >= ArrivalAfterDays * TicksPerDay && now % 2500 == 0)
                BlightfallArrivalEvent.TryTrigger(this, false);

            if (campaignState == BlightfallCampaignState.Active && nextSpreadTick >= 0 && now >= nextSpreadTick)
                RunSpreadCalculation(now);

            if (campaignState == BlightfallCampaignState.Active && nextRaidTick >= 0 && now >= nextRaidTick)
                BlightfallRaidIncident.TryLaunch(this, now);

            if (campaignState == BlightfallCampaignState.Completed && ending != BlightfallCampaignEnding.None &&
                nextEndingEffectTick >= 0 && now >= nextEndingEffectTick)
                TickEndingEffect(now);

            if (campaignState != BlightfallCampaignState.Dormant && now % 2500 == 0)
            {
                EnsureFunctionalNodes();
                BlightfallSiteUtility.EnsureSitesForPollutedTiles(this);
            }
        }

        public override void WorldComponentOnGUI()
        {
            base.WorldComponentOnGUI();
            if (!WorldRendererUtility.DrawingMap || campaignState == BlightfallCampaignState.Dormant)
                return;

            if (campaignState != BlightfallCampaignState.Completed)
            {
                GameFont previousFont = Text.Font;
                Text.Font = GameFont.Small;
                Widgets.Label(new Rect(12f, 42f, 520f, 28f),
                    "LiAIChat_BlightfallResonanceNetworkStatus".Translate(
                        RemainingResonanceSpireCount, RequiredResonanceSpireCount));
                Text.Font = previousFont;
            }

            EnsureOverlayMaterials();
            if (pollutedTileMaterial == null || sourceCenterMaterial == null || Find.WorldGrid == null)
                return;

            float tileOverlaySize = Find.WorldGrid.TileRadiusToAngle(Find.WorldGrid.AverageTileSize * 0.46f);
            RefreshTileStatuses();
            foreach (BlightfallTileState tileState in tileStates)
            {
                if (tileState == null || tileState.Status == BlightfallTileStatus.Purified)
                    continue;
                int tileId = tileState.TileId;
                if (tileId < 0 || tileId >= Find.WorldGrid.TilesCount)
                    continue;
                WorldRendererUtility.DrawQuadTangentialToPlanet(
                    Find.WorldGrid.GetTileCenter(new PlanetTile(tileId)), tileOverlaySize, 0.02f,
                    MaterialForStatus(tileState.Status), 0f, false, false, null);
            }

            foreach (int tileId in purifiedTileIds)
            {
                if (tileId < 0 || tileId >= Find.WorldGrid.TilesCount)
                    continue;
                WorldRendererUtility.DrawQuadTangentialToPlanet(
                    Find.WorldGrid.GetTileCenter(new PlanetTile(tileId)), tileOverlaySize, 0.012f,
                    purifiedTileMaterial ?? pollutedTileMaterial, 0f, false, false, null);
            }

            if (campaignState != BlightfallCampaignState.Completed &&
                sourceCenterTileId >= 0 && sourceCenterTileId < Find.WorldGrid.TilesCount)
            {
                float centerSize = tileOverlaySize * 0.72f;
                WorldRendererUtility.DrawQuadTangentialToPlanet(
                    Find.WorldGrid.GetTileCenter(new PlanetTile(sourceCenterTileId)), centerSize, 0.03f,
                    sourceCenterMaterial, 0f, false, false, null);
            }
        }

        public bool TryMarkArrivalEventTriggered()
        {
            if (arrivalEventTriggered)
                return false;
            arrivalEventTriggered = true;
            return true;
        }

        public void SetCampaignState(BlightfallCampaignState state)
        {
            campaignState = Enum.IsDefined(typeof(BlightfallCampaignState), state)
                ? state
                : BlightfallCampaignState.Dormant;
        }

        public bool SetSourceCenterTileId(int tileId)
        {
            if (tileId < 0)
                return false;
            sourceCenterTileId = tileId;
            return true;
        }

        public void SetSourceActivity(float value) => sourceActivity = BlightfallCampaignUtility.ClampPercent(value);

        public void SetSuppression(float value) => suppression = BlightfallCampaignUtility.ClampPercent(value);

        public bool SetTilePolluted(int tileId, bool polluted)
        {
            if (tileId < 0)
                return false;

            if (polluted)
                return AddContamination(tileId);
            else
            {
                pollutedTileIds.Remove(tileId);
                RefreshTileStatuses();
            }

            return true;
        }

        public bool MarkTilePurified(int tileId)
        {
            return PurifyTile(tileId);
        }

        public bool AddContamination(int tileId)
        {
            if (campaignState == BlightfallCampaignState.Completed)
                return false;
            if (pollutedTileIds.Contains(tileId))
                return true;
            // Only surface land without an existing world object can be contaminated.
            // This leaves settlements, sites, caravans, and other world objects untouched.
            if (campaignState == BlightfallCampaignState.Dormant ||
                pollutedTileIds.Count >= MaxPollutedTileCount ||
                !IsAvailableForContamination(tileId) || protectedTileIds.Contains(tileId) ||
                purifiedTileIds.Contains(tileId))
                return false;

            if (!pollutedTileIds.Contains(tileId))
                pollutedTileIds.Add(tileId);
            if (campaignState == BlightfallCampaignState.CoreExposed)
                campaignState = BlightfallCampaignState.Active;
            RefreshTileStatuses();
            return true;
        }

        public bool PurifyTile(int tileId)
        {
            if (!IsValidSurfaceTile(tileId))
                return false;

            if (!CanPurifyTile(tileId))
                return false;

            pollutedTileIds.Remove(tileId);
            if (!purifiedTileIds.Contains(tileId))
                purifiedTileIds.Add(tileId);
            RefreshTileStatuses();

            bool coreRemains = pollutedTileIds.Contains(sourceCenterTileId);
            if (!coreRemains)
                campaignState = BlightfallCampaignState.Completed;
            else
                UpdateCoreExposureState();
            return true;
        }

        public BlightfallTileStatus GetTileStatus(int tileId)
        {
            RefreshTileStatuses();
            BlightfallTileState state = tileStates.FirstOrDefault(item => item != null && item.TileId == tileId);
            return state?.Status ?? (purifiedTileIds.Contains(tileId)
                ? BlightfallTileStatus.Purified
                : BlightfallTileStatus.Interior);
        }

        public bool CanPurifyTile(int tileId)
        {
            if (!pollutedTileIds.Contains(tileId))
                return false;

            RefreshTileStatuses();
            if (tileId == sourceCenterTileId &&
                !(campaignState == BlightfallCampaignState.Completed && ending == BlightfallCampaignEnding.DestroyHeart))
                return false; // The exposed core can only be resolved through its final Site.
            if (tileId == sourceCenterTileId)
                return true; // The destroyed Heart leaves a cleanup site for its residual pollution.

            BlightfallTileState state = tileStates.FirstOrDefault(item => item != null && item.TileId == tileId);
            return state != null && state.Status == BlightfallTileStatus.Frontline;
        }

        public bool SetTileProtected(int tileId, bool isProtected = true)
        {
            if (!IsValidSurfaceTile(tileId))
                return false;

            // Protection blocks future contamination and prevents this tile spreading it;
            // it does not silently cleanse a tile that is already polluted.
            if (isProtected)
            {
                if (!protectedTileIds.Contains(tileId))
                    protectedTileIds.Add(tileId);
            }
            else
            {
                protectedTileIds.Remove(tileId);
            }
            return true;
        }

        public bool IsTilePolluted(int tileId) => pollutedTileIds.Contains(tileId);

        public bool IsTilePurified(int tileId) => purifiedTileIds.Contains(tileId);

        public bool IsTileProtected(int tileId) => protectedTileIds.Contains(tileId);

        public float AdjustActivity(float delta)
        {
            SetSourceActivity(sourceActivity + delta);
            return sourceActivity;
        }

        public float AdjustSuppression(float delta)
        {
            SetSuppression(suppression + delta);
            return suppression;
        }

        public void ScheduleNextSpread(int fromTick = -1)
        {
            int now = fromTick >= 0 ? fromTick : CurrentTick();
            nextSpreadTick = now + Rand.RangeInclusive(MinSpreadIntervalDays, MaxSpreadIntervalDays) * TicksPerDay;
        }

        public void ScheduleNextRaid(int fromTick = -1)
        {
            int now = fromTick >= 0 ? fromTick : CurrentTick();
            // High activity and surviving nests tighten the interval, while suppression slows it.
            float pressure = Mathf.Clamp01((sourceActivity / 100f) * 0.65f + ActiveBroodNestCount * 0.12f - suppression / 180f);
            int interval = Mathf.RoundToInt(Mathf.Lerp(MaximumRaidIntervalDays, MinimumRaidIntervalDays, pressure));
            nextRaidTick = now + interval * TicksPerDay;
        }

        public BlightfallPollutionNodeState GetNode(int tileId) =>
            pollutionNodes.FirstOrDefault(node => node != null && node.TileId == tileId);

        public IEnumerable<BlightfallPollutionNodeState> GetNodesAtTile(int tileId) =>
            pollutionNodes.Where(node => node != null && node.TileId == tileId);

        public BlightfallPollutionNodeState GetNode(BlightfallPollutionNodeType type, int tileId) =>
            pollutionNodes.FirstOrDefault(node => node != null && node.Type == type && node.TileId == tileId);

        public BlightfallPollutionNodeState GetOrCreateNode(int tileId)
        {
            if (tileId < 0)
                return null;

            BlightfallPollutionNodeState node = GetNode(tileId);
            if (node != null)
                return node;

            node = new BlightfallPollutionNodeState { TileId = tileId };
            pollutionNodes.Add(node);
            return node;
        }

        public BlightfallPollutionNodeState AddNode(BlightfallPollutionNodeType type, int tileId,
            float activity = 100f, float nodeSuppression = 0f)
        {
            if (!Enum.IsDefined(typeof(BlightfallPollutionNodeType), type) ||
                !pollutedTileIds.Contains(tileId) || !IsValidSurfaceTile(tileId))
                return null;

            BlightfallPollutionNodeState node = GetNode(type, tileId);
            if (node == null)
            {
                node = new BlightfallPollutionNodeState { TileId = tileId, Type = type };
                pollutionNodes.Add(node);
            }
            node.Status = BlightfallPollutionNodeStatus.Active;
            node.Activity = BlightfallCampaignUtility.ClampPercent(activity);
            node.Suppression = BlightfallCampaignUtility.ClampPercent(nodeSuppression);
            node.LastUpdatedTick = CurrentTick();
            return node;
        }

        public void EnsureFunctionalNodes()
        {
            if (campaignState == BlightfallCampaignState.Dormant || pollutedTileIds.Count == 0)
                return;

            // Older saves used one active spire on the core tile. Keep its historical
            // record but move protection to the perimeter network required by this stage.
            foreach (BlightfallPollutionNodeState coreSpire in pollutionNodes.Where(node => node != null &&
                node.Type == BlightfallPollutionNodeType.ResonanceSpire && node.TileId == sourceCenterTileId))
            {
                coreSpire.Status = BlightfallPollutionNodeStatus.Destroyed;
                coreSpire.Activity = 0f;
                coreSpire.Suppression = 0f;
                coreSpire.TargetThingId = -1;
            }

            int spireStateCount = pollutionNodes.Count(node => node != null &&
                node.Type == BlightfallPollutionNodeType.ResonanceSpire && node.TileId != sourceCenterTileId);
            while (spireStateCount < RequiredResonanceSpireCount)
            {
                int tileId = ChooseNodeTile(BlightfallPollutionNodeType.ResonanceSpire);
                if (tileId < 0 || AddNode(BlightfallPollutionNodeType.ResonanceSpire, tileId, 100f, 0f) == null)
                    break;
                spireStateCount++;
            }

            if (!pollutionNodes.Any(node => node != null && node.Type == BlightfallPollutionNodeType.SporeTower))
            {
                int tileId = ChooseNodeTile(BlightfallPollutionNodeType.SporeTower);
                if (tileId >= 0)
                    AddNode(BlightfallPollutionNodeType.SporeTower, tileId, 100f, 0f);
            }

            if (!pollutionNodes.Any(node => node != null && node.Type == BlightfallPollutionNodeType.BroodNest))
            {
                int tileId = ChooseNodeTile(BlightfallPollutionNodeType.BroodNest);
                if (tileId >= 0)
                    AddNode(BlightfallPollutionNodeType.BroodNest, tileId, 100f, 0f);
            }
            UpdateCoreExposureState();
        }

        private void UpdateCoreExposureState()
        {
            if (campaignState == BlightfallCampaignState.Dormant ||
                campaignState == BlightfallCampaignState.Completed ||
                !pollutedTileIds.Contains(sourceCenterTileId))
                return;

            int networkNodeCount = pollutionNodes.Count(node => node != null &&
                node.Type == BlightfallPollutionNodeType.ResonanceSpire && node.TileId != sourceCenterTileId);
            bool networkDestroyed = networkNodeCount >= RequiredResonanceSpireCount &&
                DestroyedResonanceSpireCount >= RequiredResonanceSpireCount;
            bool perimeterCleared = !pollutedTileIds.Any(tileId => tileId != sourceCenterTileId);
            if (networkDestroyed && perimeterCleared)
                campaignState = BlightfallCampaignState.CoreExposed;
        }

        private int ChooseNodeTile(BlightfallPollutionNodeType type)
        {
            List<int> candidates = pollutedTileIds.Where(tileId => tileId != sourceCenterTileId &&
                !pollutionNodes.Any(node => node != null && node.TileId == tileId)).ToList();
            if (candidates.Count == 0)
                candidates = pollutedTileIds.Where(tileId =>
                    tileId != sourceCenterTileId &&
                    !pollutionNodes.Any(node => node != null && node.Type == type && node.TileId == tileId)).ToList();
            return candidates.Count == 0 ? -1 : candidates.RandomElement();
        }

        public bool RegisterNodeTarget(int tileId, int targetThingId)
        {
            BlightfallPollutionNodeState node = pollutionNodes.FirstOrDefault(item => item != null &&
                item.TileId == tileId && item.Status == BlightfallPollutionNodeStatus.Active &&
                item.TargetThingId < 0);
            if (node == null || targetThingId < 0)
                return false;
            node.TargetThingId = targetThingId;
            return true;
        }

        public bool RegisterNodeTarget(BlightfallPollutionNodeType type, int tileId, int targetThingId)
        {
            BlightfallPollutionNodeState node = GetNodesAtTile(tileId).FirstOrDefault(item =>
                item.Type == type && item.Status == BlightfallPollutionNodeStatus.Active && item.TargetThingId < 0);
            if (node == null || targetThingId < 0)
                return false;
            node.TargetThingId = targetThingId;
            return true;
        }

        public void ResetNodeTargetBindings(int tileId)
        {
            foreach (BlightfallPollutionNodeState node in GetNodesAtTile(tileId))
            {
                if (node.Status == BlightfallPollutionNodeStatus.Active)
                    node.TargetThingId = -1;
            }
        }

        public bool NotifyNodeTargetDestroyed(int tileId, int targetThingId)
        {
            BlightfallPollutionNodeState node = pollutionNodes.FirstOrDefault(item => item != null &&
                item.TileId == tileId && item.TargetThingId == targetThingId &&
                item.Status == BlightfallPollutionNodeStatus.Active);
            if (node == null)
                return false;
            node.Status = BlightfallPollutionNodeStatus.Destroyed;
            node.Activity = 0f;
            node.Suppression = 0f;
            node.LastUpdatedTick = CurrentTick();
            if (node.Type == BlightfallPollutionNodeType.ResonanceSpire && node.TileId != sourceCenterTileId)
            {
                AdjustActivity(-4f);
                AdjustSuppression(4f);
                UpdateCoreExposureState();
            }
            Log.Message("[LiAIChat Blightfall] " + node.Type + " destroyed at tile " + tileId + ".");
            return true;
        }

        public bool NotifyNodeTargetDisabled(int tileId, int targetThingId)
        {
            BlightfallPollutionNodeState node = pollutionNodes.FirstOrDefault(item => item != null &&
                item.TileId == tileId && item.TargetThingId == targetThingId &&
                item.Status == BlightfallPollutionNodeStatus.Active);
            if (node == null)
                return false;
            node.Status = BlightfallPollutionNodeStatus.Suppressed;
            node.Activity = 0f;
            node.Suppression = 0f;
            node.LastUpdatedTick = CurrentTick();
            return true;
        }

        public int GetActiveNodeCount(BlightfallPollutionNodeType type) => pollutionNodes.Count(node =>
            node != null && node.Type == type && node.Status == BlightfallPollutionNodeStatus.Active);

        public float GetBroodNestGenerationMultiplier(int tileId)
        {
            float nearbyNestStrength = GetActiveNodeStrengthNear(
                BlightfallPollutionNodeType.BroodNest, tileId, true);
            return 1f + nearbyNestStrength * 0.5f;
        }

        private float GetActiveNodeStrengthNear(BlightfallPollutionNodeType type, int tileId, bool includeSameTile)
        {
            if (Find.WorldGrid == null || tileId < 0 || tileId >= Find.WorldGrid.TilesCount)
                return 0;
            var neighbors = new List<PlanetTile>();
            Find.WorldGrid.GetTileNeighbors(new PlanetTile(tileId), neighbors);
            return pollutionNodes.Where(node => node != null && node.Type == type &&
                    node.Status == BlightfallPollutionNodeStatus.Active &&
                    (node.TileId == tileId ? includeSameTile : neighbors.Any(tile => tile.tileId == node.TileId)))
                .Sum(node => node.Activity / 100f);
        }

        public bool UpdateNode(int tileId, BlightfallPollutionNodeStatus status,
            float activity, float nodeSuppression, int updatedAtTick = -1)
        {
            BlightfallPollutionNodeState node = GetOrCreateNode(tileId);
            if (node == null)
                return false;

            node.Status = Enum.IsDefined(typeof(BlightfallPollutionNodeStatus), status)
                ? status
                : BlightfallPollutionNodeStatus.Dormant;
            node.Activity = BlightfallCampaignUtility.ClampPercent(activity);
            node.Suppression = BlightfallCampaignUtility.ClampPercent(nodeSuppression);
            node.LastUpdatedTick = updatedAtTick >= 0 ? updatedAtTick : CurrentTick();
            return true;
        }

        public bool RemoveNode(int tileId)
        {
            return pollutionNodes.RemoveAll(node => node != null && node.TileId == tileId) > 0;
        }

        public BlightfallSiteState GetSiteState(Site site)
        {
            return site == null ? null : siteStates.FirstOrDefault(state =>
                state != null && state.SiteId == site.ID);
        }

        public bool RegisterSite(Site site, BlightfallSiteType type, float difficulty)
        {
            if (site == null || site.ID < 0 || !site.Tile.Valid)
                return false;

            BlightfallSiteState state = GetSiteState(site);
            if (state == null)
            {
                state = new BlightfallSiteState { SiteId = site.ID };
                siteStates.Add(state);
            }
            state.TileId = site.Tile.tileId;
            state.Type = Enum.IsDefined(typeof(BlightfallSiteType), type)
                ? type
                : BlightfallSiteType.ContaminatedRuin;
            state.Difficulty = BlightfallCampaignUtility.ClampPercent(difficulty);
            state.Status = BlightfallSiteStatus.Created;
            state.LastUpdatedTick = CurrentTick();
            return true;
        }

        public bool RegisterSpecialSiteObjective(Site site, int targetThingId, int initialPollutionCount)
        {
            BlightfallSiteState state = GetSiteState(site);
            if (state == null || state.Type == BlightfallSiteType.ContaminatedRuin || targetThingId < 0)
                return false;
            if (state.ObjectiveThingId < 0)
                state.ObjectiveThingId = targetThingId;
            if (state.InitialPollutionCount < 0)
                state.InitialPollutionCount = Math.Max(0, initialPollutionCount);
            if (state.NextEffectTick < 0)
                state.NextEffectTick = CurrentTick() + 1500;
            return state.ObjectiveThingId == targetThingId;
        }

        public bool NotifySpecialSiteObjectiveDestroyed(Site site, int targetThingId)
        {
            BlightfallSiteState state = GetSiteState(site);
            if (state == null || state.ObjectiveThingId != targetThingId || state.ObjectiveDestroyed)
                return false;
            state.ObjectiveDestroyed = true;
            state.LastUpdatedTick = CurrentTick();
            return true;
        }

        public bool CompleteSpecialSite(Site site)
        {
            BlightfallSiteState state = GetSiteState(site);
            if (state == null || state.Type == BlightfallSiteType.ContaminatedRuin ||
                state.Status == BlightfallSiteStatus.Completed || state.Status == BlightfallSiteStatus.Removed)
                return false;

            switch (state.Type)
            {
                case BlightfallSiteType.SporeForest:
                    AdjustActivity(-10f);
                    break;
                case BlightfallSiteType.ToxicHive:
                    AdjustActivity(-15f);
                    AdjustSuppression(8f);
                    break;
                case BlightfallSiteType.AncientProcessingPlant:
                    AdjustActivity(-12f);
                    AdjustSuppression(10f);
                    break;
                case BlightfallSiteType.ResonanceDefense:
                    // The node's shield contribution and network state were updated when
                    // the objective was destroyed; this completion is only the Site signal.
                    UpdateCoreExposureState();
                    break;
            }
            UpdateSiteStatus(site, BlightfallSiteStatus.Completed);
            Log.Message("[LiAIChat Blightfall] Special site completed: " + state.Type + " at tile " + state.TileId + ".");
            string completionText = state.Type == BlightfallSiteType.SporeForest
                ? "LiAIChat_BlightfallSporeForestComplete".Translate()
                : state.Type == BlightfallSiteType.ToxicHive
                    ? "LiAIChat_BlightfallToxicHiveComplete".Translate()
                    : state.Type == BlightfallSiteType.ResonanceDefense
                        ? "LiAIChat_BlightfallResonanceSpireComplete".Translate(
                            RemainingResonanceSpireCount, RequiredResonanceSpireCount)
                        : "LiAIChat_BlightfallProcessingPlantComplete".Translate();
            Find.LetterStack?.ReceiveLetter("LiAIChat_BlightfallSpecialSiteCompleteTitle".Translate(),
                completionText, LetterDefOf.PositiveEvent, site);
            return true;
        }

        public bool CompleteFinalCoreSite(Site site, BlightfallCampaignEnding selectedEnding)
        {
            BlightfallSiteState state = GetSiteState(site);
            if (state == null || state.Type != BlightfallSiteType.BlightHeart ||
                campaignState != BlightfallCampaignState.CoreExposed ||
                state.CoreBattleStage != BlightfallCoreBattleStage.FinalPurification ||
                (selectedEnding != BlightfallCampaignEnding.DestroyHeart &&
                 selectedEnding != BlightfallCampaignEnding.PurifyHeart &&
                 selectedEnding != BlightfallCampaignEnding.HarnessHeart) ||
                (selectedEnding == BlightfallCampaignEnding.PurifyHeart && !CanPurifyEnding()))
                return false;

            ending = selectedEnding;
            state.CoreBattleStage = BlightfallCoreBattleStage.Completed;
            state.CoreHeartHealthPercent = 0f;
            state.CoreHeartThingId = -1;
            state.CorePerimeterTargetThingIds.Clear();
            state.CoreDefenseTargetThingIds.Clear();
            state.LastUpdatedTick = CurrentTick();
            if (selectedEnding == BlightfallCampaignEnding.DestroyHeart)
            {
                state.Type = BlightfallSiteType.ContaminatedRuin;
                state.Status = BlightfallSiteStatus.Created;
                state.ObjectiveThingId = -1;
                state.ObjectiveDestroyed = false;
                site.customLabel = "LiAIChat_BlightfallDestroyedHeartSiteLabel".Translate();
                Map existingMap = Find.Maps.FirstOrDefault(map => map != null && map.Parent == site);
                if (existingMap != null)
                {
                    LiAIChat.Questing.QuestPart_AbandonedWasteDump cleanup =
                        LiAIChat.Questing.QuestPart_AbandonedWasteDump.TryStartForBlightfallSite(site);
                    cleanup?.InitializeFromMap(existingMap);
                }
            }
            else
            {
                state.Status = BlightfallSiteStatus.Completed;
                pollutedTileIds.Remove(sourceCenterTileId);
                if (!purifiedTileIds.Contains(sourceCenterTileId))
                    purifiedTileIds.Add(sourceCenterTileId);
                protectedTileIds.Add(sourceCenterTileId);
                site.customLabel = (selectedEnding == BlightfallCampaignEnding.PurifyHeart
                    ? "LiAIChat_BlightfallPurifiedHeartSiteLabel"
                    : "LiAIChat_BlightfallHarnessedHeartSiteLabel").Translate();
            }
            campaignState = BlightfallCampaignState.Completed;
            sourceActivity = 0f;
            suppression = 100f;
            nextSpreadTick = -1;
            nextRaidTick = -1;
            nextEndingEffectTick = selectedEnding == BlightfallCampaignEnding.DestroyHeart
                ? -1 : CurrentTick() + EndingEffectIntervalTicks;
            harnessFailures = 0;
            foreach (BlightfallPollutionNodeState node in pollutionNodes.Where(item => item != null))
            {
                if (node.Status == BlightfallPollutionNodeStatus.Active)
                    node.Status = BlightfallPollutionNodeStatus.Suppressed;
                node.Activity = 0f;
                node.Suppression = 100f;
                node.LastUpdatedTick = CurrentTick();
            }
            RefreshTileStatuses();
            string titleKey = selectedEnding == BlightfallCampaignEnding.DestroyHeart
                ? "LiAIChat_BlightfallEndingDestroyTitle"
                : selectedEnding == BlightfallCampaignEnding.PurifyHeart
                    ? "LiAIChat_BlightfallEndingPurifyTitle" : "LiAIChat_BlightfallEndingHarnessTitle";
            string textKey = selectedEnding == BlightfallCampaignEnding.DestroyHeart
                ? "LiAIChat_BlightfallEndingDestroyText"
                : selectedEnding == BlightfallCampaignEnding.PurifyHeart
                    ? "LiAIChat_BlightfallEndingPurifyText" : "LiAIChat_BlightfallEndingHarnessText";
            Find.LetterStack?.ReceiveLetter(titleKey.Translate(), textKey.Translate(),
                selectedEnding == BlightfallCampaignEnding.HarnessHeart
                    ? LetterDefOf.NeutralEvent : LetterDefOf.PositiveEvent, site);
            Log.Message("[LiAIChat Blightfall] Final core outcome committed: " + ending + ".");
            return true;
        }

        public bool CanPurifyEnding()
        {
            ResearchProjectDef required = DefDatabase<ResearchProjectDef>.GetNamedSilentFail("Fabrication");
            return required != null && required.IsFinished;
        }

        private const int EndingEffectIntervalTicks = 5 * TicksPerDay;
        private const int HarnessChemfuelCost = 50;
        private const int HarnessUraniumReward = 10;

        private void TickEndingEffect(int now)
        {
            // Persist the next deadline before applying the effect so a save cannot duplicate it.
            nextEndingEffectTick = now + EndingEffectIntervalTicks;
            if (ending == BlightfallCampaignEnding.PurifyHeart)
            {
                if (PurifyNextFrontlineTile())
                    Log.Message("[LiAIChat Blightfall] The purified Heart is cleansing another world tile.");
                return;
            }
            if (ending != BlightfallCampaignEnding.HarnessHeart)
                return;

            if (TryPayHarnessMaintenance())
            {
                harnessFailures = 0;
                DeliverHarnessReward();
                Find.LetterStack?.ReceiveLetter("LiAIChat_BlightfallHarnessMaintainedTitle".Translate(),
                    "LiAIChat_BlightfallHarnessMaintainedText".Translate(HarnessChemfuelCost, HarnessUraniumReward),
                    LetterDefOf.PositiveEvent);
                return;
            }

            harnessFailures++;
            Find.LetterStack?.ReceiveLetter("LiAIChat_BlightfallHarnessFailureTitle".Translate(),
                "LiAIChat_BlightfallHarnessFailureText".Translate(HarnessChemfuelCost, harnessFailures),
                LetterDefOf.ThreatBig);
            if (harnessFailures % 2 == 0)
                BlightfallFinalCoreUtility.TryHarnessInstabilityIncursion();
        }

        private bool PurifyNextFrontlineTile()
        {
            if (Find.WorldGrid == null || Find.WorldObjects == null)
                return false;
            RefreshTileStatuses();
            List<int> candidates = pollutedTileIds.Where(tileId => tileId != sourceCenterTileId &&
                    CanPurifyTile(tileId))
                .OrderBy(tileId => tileId).ToList();
            if (candidates.Count == 0)
                return false;

            int tileId = candidates[0];
            Site site = Find.WorldObjects.Sites.FirstOrDefault(candidate => candidate.Tile.tileId == tileId);
            if (site != null)
            {
                BlightfallSiteState siteState = GetSiteState(site);
                if (siteState != null)
                {
                    siteState.Status = BlightfallSiteStatus.Removed;
                    siteState.LastUpdatedTick = CurrentTick();
                }
                site.Destroy();
            }
            bool purified = PurifyTile(tileId);
            if (purified)
                Find.LetterStack?.ReceiveLetter("LiAIChat_BlightfallPurificationWaveTitle".Translate(),
                    "LiAIChat_BlightfallPurificationWaveText".Translate(), LetterDefOf.PositiveEvent);
            return purified;
        }

        private bool TryPayHarnessMaintenance()
        {
            ThingDef chemfuel = DefDatabase<ThingDef>.GetNamedSilentFail("Chemfuel");
            List<Map> homeMaps = Find.Maps.Where(map => map != null && map.IsPlayerHome &&
                map.mapPawns.FreeColonists.Any()).ToList();
            if (chemfuel == null || homeMaps.Count == 0)
                return false;
            List<Thing> stacks = homeMaps.SelectMany(map => map.listerThings.ThingsOfDef(chemfuel))
                .Where(thing => thing != null && thing.Spawned && !thing.Destroyed && !thing.IsForbidden(Faction.OfPlayer))
                .ToList();
            if (stacks.Sum(thing => thing.stackCount) < HarnessChemfuelCost)
                return false;

            int remaining = HarnessChemfuelCost;
            foreach (Thing stack in stacks)
            {
                int used = Math.Min(remaining, stack.stackCount);
                stack.stackCount -= used;
                remaining -= used;
                if (stack.stackCount <= 0 && !stack.Destroyed)
                    stack.Destroy(DestroyMode.Vanish);
                if (remaining <= 0)
                    break;
            }
            return remaining == 0;
        }

        private void DeliverHarnessReward()
        {
            ThingDef uranium = DefDatabase<ThingDef>.GetNamedSilentFail("Uranium");
            Map map = Find.Maps.FirstOrDefault(candidate => candidate != null && candidate.IsPlayerHome &&
                candidate.mapPawns.FreeColonists.Any());
            if (uranium == null || map == null)
                return;
            Thing reward = ThingMaker.MakeThing(uranium);
            reward.stackCount = HarnessUraniumReward;
            if (!GenPlace.TryPlaceThing(reward, map.Center, map, ThingPlaceMode.Near) && !reward.Destroyed)
                reward.Destroy(DestroyMode.Vanish);
        }

        public bool UpdateSiteStatus(Site site, BlightfallSiteStatus status)
        {
            if (site == null || !Enum.IsDefined(typeof(BlightfallSiteStatus), status))
                return false;

            BlightfallSiteState state = GetSiteState(site);
            if (state == null)
            {
                if (status == BlightfallSiteStatus.Completed)
                    return false;
                if (!RegisterSite(site, BlightfallSiteType.ContaminatedRuin, sourceActivity))
                    return false;
                state = GetSiteState(site);
            }

            // Site lifecycle signals can arrive more than once; terminal outcomes stay terminal.
            if (state.Status == BlightfallSiteStatus.Completed || state.Status == BlightfallSiteStatus.Removed)
                return true;
            if ((int)status < (int)state.Status && status != BlightfallSiteStatus.Completed)
                return true;
            state.Status = status;
            state.LastUpdatedTick = CurrentTick();
            return true;
        }

        /// <summary>Records the latest tick processed by a future world simulation task.</summary>
        public void RecordWorldSimulationTime(int tick = -1)
        {
            lastWorldSimulationTick = tick >= 0 ? tick : CurrentTick();
        }

        public void ResetToDormant()
        {
            campaignState = BlightfallCampaignState.Dormant;
            sourceCenterTileId = -1;
            pollutedTileIds.Clear();
            purifiedTileIds.Clear();
            protectedTileIds.Clear();
            tileStates.Clear();
            pollutionNodes.Clear();
            siteStates.Clear();
            sourceActivity = 0f;
            suppression = 0f;
            campaignStartTick = -1;
            lastWorldSimulationTick = -1;
            nextSpreadTick = -1;
            nextRaidTick = -1;
            ending = BlightfallCampaignEnding.None;
            nextEndingEffectTick = -1;
            harnessFailures = 0;
            arrivalEventTriggered = false;
        }

        private void NormalizeAfterLoad()
        {
            if (!Enum.IsDefined(typeof(BlightfallCampaignState), campaignState))
                campaignState = BlightfallCampaignState.Dormant;
            if (!Enum.IsDefined(typeof(BlightfallCampaignEnding), ending))
                ending = BlightfallCampaignEnding.None;
            if (sourceCenterTileId < -1)
                sourceCenterTileId = -1;
            if (campaignState != BlightfallCampaignState.Dormant || sourceCenterTileId >= 0)
                arrivalEventTriggered = true;
            if (campaignStartTick < -1)
                campaignStartTick = -1;
            if (lastWorldSimulationTick < -1)
                lastWorldSimulationTick = -1;
            if (nextSpreadTick < -1)
                nextSpreadTick = -1;
            if (nextRaidTick < -1)
                nextRaidTick = -1;
            if (nextEndingEffectTick < -1)
                nextEndingEffectTick = -1;
            harnessFailures = Math.Max(0, harnessFailures);

            sourceActivity = BlightfallCampaignUtility.ClampPercent(sourceActivity);
            suppression = BlightfallCampaignUtility.ClampPercent(suppression);
            pollutedTileIds = NormalizeTileIds(pollutedTileIds);
            purifiedTileIds = NormalizeTileIds(purifiedTileIds);
            protectedTileIds = NormalizeTileIds(protectedTileIds);
            if (tileStates == null)
                tileStates = new List<BlightfallTileState>();

            HashSet<int> polluted = new HashSet<int>(pollutedTileIds);
            purifiedTileIds.RemoveAll(tileId => polluted.Contains(tileId));
            if (pollutedTileIds.Count > MaxPollutedTileCount)
                pollutedTileIds.RemoveRange(MaxPollutedTileCount, pollutedTileIds.Count - MaxPollutedTileCount);

            if (campaignState == BlightfallCampaignState.Active && nextSpreadTick < 0)
                nextSpreadTick = CurrentTick() + 4 * TicksPerDay;
            if (campaignState == BlightfallCampaignState.Active && nextRaidTick < 0)
                ScheduleNextRaid(CurrentTick());

            if (pollutionNodes == null)
                pollutionNodes = new List<BlightfallPollutionNodeState>();
            var seenNodes = new HashSet<string>();
            pollutionNodes.RemoveAll(node =>
            {
                if (node == null || node.TileId < 0)
                    return true;
                node.Normalize();
                // Older saves stored one untyped active node at the source tile; migrate it to
                // the core's Resonance Spire before adding the other functional nodes.
                if (node.TileId == sourceCenterTileId && node.Type == BlightfallPollutionNodeType.SporeTower)
                    node.Type = BlightfallPollutionNodeType.ResonanceSpire;
                return !seenNodes.Add(node.TileId + ":" + node.Type);
            });

            if (siteStates == null)
                siteStates = new List<BlightfallSiteState>();
            HashSet<int> seenSiteIds = new HashSet<int>();
            siteStates.RemoveAll(state =>
            {
                if (state == null || state.SiteId < 0 || !seenSiteIds.Add(state.SiteId))
                    return true;
                state.Normalize();
                return false;
            });

            var seenProgressTiles = new HashSet<int>();
            tileStates.RemoveAll(state => state == null || state.TileId < 0 || !seenProgressTiles.Add(state.TileId));
            RefreshTileStatuses();
            if (campaignState != BlightfallCampaignState.Dormant)
                EnsureFunctionalNodes();
        }

        private static List<int> NormalizeTileIds(List<int> tileIds)
        {
            if (tileIds == null)
                return new List<int>();

            HashSet<int> seen = new HashSet<int>();
            tileIds.RemoveAll(tileId => tileId < 0 || !seen.Add(tileId));
            return tileIds;
        }

        private static int CurrentTick() => Find.TickManager?.TicksGame ?? 0;

        private bool IsValidSurfaceTile(int tileId)
        {
            if (tileId < 0 || Find.WorldGrid == null || tileId >= Find.WorldGrid.TilesCount)
                return false;
            return Find.WorldGrid[new PlanetTile(tileId)].OnSurface &&
                   !Find.WorldGrid[new PlanetTile(tileId)].WaterCovered;
        }

        private bool IsAvailableForContamination(int tileId)
        {
            return IsValidSurfaceTile(tileId) && Find.WorldObjects != null &&
                   !Find.WorldObjects.AnyWorldObjectAt(new PlanetTile(tileId));
        }

        private void RunSpreadCalculation(int now)
        {
            // Advance the saved deadline before doing any work, so a save at this tick cannot repeat it.
            ScheduleNextSpread(now);
            lastWorldSimulationTick = now;

            if (sourceActivity <= 0f || pollutedTileIds.Count >= MaxPollutedTileCount ||
                Find.WorldGrid == null || Find.WorldObjects == null)
                return;

            var candidates = new HashSet<int>();
            foreach (int sourceTileId in new List<int>(pollutedTileIds))
            {
                if (protectedTileIds.Contains(sourceTileId) || !IsValidSurfaceTile(sourceTileId))
                    continue;

                var neighbors = new List<PlanetTile>();
                Find.WorldGrid.GetTileNeighbors(new PlanetTile(sourceTileId), neighbors);
                foreach (PlanetTile neighbor in neighbors)
                {
                    int neighborId = neighbor.tileId;
                    if (candidates.Contains(neighborId) || pollutedTileIds.Contains(neighborId) ||
                        protectedTileIds.Contains(neighborId) || purifiedTileIds.Contains(neighborId) ||
                        !IsAvailableForContamination(neighborId))
                        continue;
                    candidates.Add(neighborId);
                }
            }

            float spreadChance = (0.08f + 0.32f * sourceActivity / 100f) *
                                 (1f - 0.9f * suppression / 100f);
            int added = 0;
            foreach (int candidate in candidates.InRandomOrder())
            {
                int purifiedNeighbors = CountAdjacentPurifiedTiles(candidate);
                float barrierFactor = 1f - 0.18f * Math.Min(3, purifiedNeighbors);
                float nearbySporeTowers = GetActiveNodeStrengthNear(
                    BlightfallPollutionNodeType.SporeTower, candidate, true);
                float nodeBonus = nearbySporeTowers * 0.12f;
                if (Rand.Value > Mathf.Clamp01(spreadChance + nodeBonus) * barrierFactor)
                    continue;
                if (AddContamination(candidate))
                    added++;
                if (added >= MaxNewTilesPerSpread || pollutedTileIds.Count >= MaxPollutedTileCount)
                    break;
            }
            RefreshTileStatuses();
            BlightfallSiteUtility.EnsureSitesForPollutedTiles(this);
        }

        private void EnsureOverlayMaterials()
        {
            if (pollutedTileMaterial == null && ShaderDatabase.WorldOverlayTransparentLitPollution != null)
            {
                pollutedTileMaterial = new Material(ShaderDatabase.WorldOverlayTransparentLitPollution);
                pollutedTileMaterial.color = new Color(0.62f, 0.16f, 0.08f, 0.52f);
            }
            if (sourceCenterMaterial == null && ShaderDatabase.WorldOverlayAdditive != null)
            {
                sourceCenterMaterial = new Material(ShaderDatabase.WorldOverlayAdditive);
                sourceCenterMaterial.color = new Color(1f, 0.2f, 0.06f, 0.9f);
            }
            Shader worldShader = ShaderDatabase.WorldOverlayTransparentLitPollution;
            if (worldShader != null)
            {
                if (frontlineTileMaterial == null)
                    frontlineTileMaterial = MakeOverlayMaterial(worldShader, new Color(0.92f, 0.55f, 0.08f, 0.66f));
                if (interiorTileMaterial == null)
                    interiorTileMaterial = MakeOverlayMaterial(worldShader, new Color(0.52f, 0.12f, 0.08f, 0.62f));
                if (coreTileMaterial == null)
                    coreTileMaterial = MakeOverlayMaterial(worldShader, new Color(0.83f, 0.2f, 0.72f, 0.8f));
                if (purifiedTileMaterial == null)
                    purifiedTileMaterial = MakeOverlayMaterial(worldShader, new Color(0.18f, 0.78f, 0.33f, 0.56f));
            }
        }

        private static Material MakeOverlayMaterial(Shader shader, Color color)
        {
            return new Material(shader) { color = color };
        }

        private Material MaterialForStatus(BlightfallTileStatus status)
        {
            switch (status)
            {
                case BlightfallTileStatus.Frontline: return frontlineTileMaterial ?? pollutedTileMaterial;
                case BlightfallTileStatus.Core: return coreTileMaterial ?? sourceCenterMaterial;
                default: return interiorTileMaterial ?? pollutedTileMaterial;
            }
        }

        private void RefreshTileStatuses()
        {
            if (tileStates == null)
                tileStates = new List<BlightfallTileState>();

            var statuses = new Dictionary<int, BlightfallTileStatus>();
            foreach (int tileId in pollutedTileIds)
            {
                if (tileId == sourceCenterTileId)
                    statuses[tileId] = BlightfallTileStatus.Core;
                else
                    statuses[tileId] = IsFrontlineTile(tileId)
                        ? BlightfallTileStatus.Frontline
                        : BlightfallTileStatus.Interior;
            }
            foreach (int tileId in purifiedTileIds)
                statuses[tileId] = BlightfallTileStatus.Purified;

            var existing = tileStates.Where(state => state != null)
                .ToDictionary(state => state.TileId, state => state);
            tileStates.Clear();
            foreach (KeyValuePair<int, BlightfallTileStatus> entry in statuses)
            {
                BlightfallTileState state;
                if (!existing.TryGetValue(entry.Key, out state))
                    state = new BlightfallTileState { TileId = entry.Key };
                state.Status = entry.Value;
                tileStates.Add(state);
            }
        }

        private bool IsFrontlineTile(int tileId)
        {
            if (Find.WorldGrid == null || !IsValidSurfaceTile(tileId))
                return false;

            var neighbors = new List<PlanetTile>();
            Find.WorldGrid.GetTileNeighbors(new PlanetTile(tileId), neighbors);
            return neighbors.Any(neighbor => !pollutedTileIds.Contains(neighbor.tileId) &&
                (purifiedTileIds.Contains(neighbor.tileId) || IsValidSurfaceTile(neighbor.tileId)));
        }

        private int CountAdjacentPurifiedTiles(int tileId)
        {
            if (Find.WorldGrid == null)
                return 0;
            var neighbors = new List<PlanetTile>();
            Find.WorldGrid.GetTileNeighbors(new PlanetTile(tileId), neighbors);
            return neighbors.Count(neighbor => purifiedTileIds.Contains(neighbor.tileId));
        }
    }
}
