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
    public static class BlightfallFinalCoreUtility
    {
        private const int PerimeterTargets = 3;
        private const int CoreDefenseTargets = 2;
        private const int ReinforcementWaveCount = 3;
        private const int FirstReinforcementDelay = 2500;
        private const int ReinforcementInterval = 4500;
        private const int EnvironmentalPulseInterval = 6000;

        public static void Initialize(Map map)
        {
            Site site = map?.Parent as Site;
            WorldComponent_Blightfall campaign = WorldComponent_Blightfall.Current;
            BlightfallSiteState state = campaign?.GetSiteState(site);
            if (map == null || state == null || state.Type != BlightfallSiteType.BlightHeart ||
                state.Status == BlightfallSiteStatus.Completed || state.Status == BlightfallSiteStatus.Removed ||
                campaign.CampaignState != BlightfallCampaignState.CoreExposed)
                return;

            if (state.CoreBattleStage == BlightfallCoreBattleStage.NotStarted)
            {
                state.CoreBattleStage = BlightfallCoreBattleStage.PerimeterBreach;
                state.CorePerimeterTargetsRemaining = PerimeterTargets;
                state.CoreDefenseTargetsRemaining = CoreDefenseTargets;
                state.CoreReinforcementWavesSpawned = 0;
                state.CoreNextEnvironmentalPulseTick = CurrentTick() + EnvironmentalPulseInterval;
                state.LastUpdatedTick = CurrentTick();
                Log.Message("[LiAIChat Blightfall] Final core battle started at site " + site.ID + ".");
                AnnounceStage(site, state.CoreBattleStage);
            }

            EnsureHeart(map, site, state);
            EnsureStageTargets(map, site, state);
            if (state.CoreBattleStage == BlightfallCoreBattleStage.RepelReinforcements &&
                state.CoreNextReinforcementTick < 0)
                state.CoreNextReinforcementTick = CurrentTick() + FirstReinforcementDelay;
            campaign.UpdateSiteStatus(site, BlightfallSiteStatus.Visited);
        }

        public static void Tick(Map map)
        {
            Site site = map?.Parent as Site;
            WorldComponent_Blightfall campaign = WorldComponent_Blightfall.Current;
            BlightfallSiteState state = campaign?.GetSiteState(site);
            if (map == null || state == null || state.Type != BlightfallSiteType.BlightHeart ||
                state.CoreBattleStage == BlightfallCoreBattleStage.NotStarted ||
                state.CoreBattleStage == BlightfallCoreBattleStage.Completed ||
                campaign.CampaignState != BlightfallCampaignState.CoreExposed || CurrentTick() % 250 != 0)
                return;

            EnsureHeart(map, site, state);
            EnsureStageTargets(map, site, state);
            int now = CurrentTick();
            if (now >= state.CoreNextEnvironmentalPulseTick && state.CoreBattleStage < BlightfallCoreBattleStage.WeakenHeart)
            {
                AddCorePollutionPulse(map);
                state.CoreNextEnvironmentalPulseTick = now + EnvironmentalPulseInterval;
            }

            if (state.CoreBattleStage != BlightfallCoreBattleStage.RepelReinforcements)
                return;

            Faction insectFaction = GetInsectFaction();
            if (state.CoreReinforcementWavesSpawned < ReinforcementWaveCount &&
                now >= state.CoreNextReinforcementTick)
            {
                if (insectFaction != null && SpawnReinforcementWave(map, site, insectFaction))
                {
                    state.CoreReinforcementWavesSpawned++;
                    state.CoreNextReinforcementTick = now + ReinforcementInterval;
                    Log.Message("[LiAIChat Blightfall] Spawned core reinforcement wave " +
                        state.CoreReinforcementWavesSpawned + "/" + ReinforcementWaveCount + " at site " + site.ID + ".");
                }
                else
                {
                    state.CoreNextReinforcementTick = now + 1500;
                }
            }

            if (state.CoreReinforcementWavesSpawned >= ReinforcementWaveCount &&
                !HasLivingDefenders(map, insectFaction))
            {
                EnterStage(site, state, BlightfallCoreBattleStage.WeakenHeart);
                Log.Message("[LiAIChat Blightfall] Core reinforcements defeated at site " + site.ID + ".");
            }
        }

        public static void NotifyThingDestroyed(Site site, int thingId)
        {
            WorldComponent_Blightfall campaign = WorldComponent_Blightfall.Current;
            BlightfallSiteState state = campaign?.GetSiteState(site);
            if (state == null || state.Type != BlightfallSiteType.BlightHeart || thingId < 0)
                return;

            if (state.CoreBattleStage == BlightfallCoreBattleStage.PerimeterBreach &&
                state.CorePerimeterTargetThingIds.Remove(thingId))
            {
                state.CorePerimeterTargetsRemaining = Math.Max(0, state.CorePerimeterTargetsRemaining - 1);
                if (state.CorePerimeterTargetsRemaining == 0)
                {
                    state.CoreDefenseTargetsRemaining = CoreDefenseTargets;
                    EnterStage(site, state, BlightfallCoreBattleStage.DestroyCoreDefenses);
                    Log.Message("[LiAIChat Blightfall] Core perimeter breached at site " + site.ID + ".");
                }
                return;
            }

            if (state.CoreBattleStage == BlightfallCoreBattleStage.DestroyCoreDefenses &&
                state.CoreDefenseTargetThingIds.Remove(thingId))
            {
                state.CoreDefenseTargetsRemaining = Math.Max(0, state.CoreDefenseTargetsRemaining - 1);
                if (state.CoreDefenseTargetsRemaining == 0)
                {
                    state.CoreReinforcementWavesSpawned = 0;
                    state.CoreNextReinforcementTick = CurrentTick() + FirstReinforcementDelay;
                    EnterStage(site, state, BlightfallCoreBattleStage.RepelReinforcements);
                    Log.Message("[LiAIChat Blightfall] Core defenses destroyed at site " + site.ID + ".");
                }
                return;
            }

            if (thingId == state.CoreHeartThingId &&
                state.CoreBattleStage == BlightfallCoreBattleStage.FinalPurification)
            {
                Log.Warning("[LiAIChat Blightfall] The Heart was destroyed outside an ending choice; outcome was not committed.");
            }
        }

        public static bool ChooseEnding(Site site, BlightfallCampaignEnding ending)
        {
            WorldComponent_Blightfall campaign = WorldComponent_Blightfall.Current;
            BlightfallSiteState state = campaign?.GetSiteState(site);
            if (site == null || campaign == null || state == null ||
                state.CoreBattleStage != BlightfallCoreBattleStage.FinalPurification ||
                (ending == BlightfallCampaignEnding.PurifyHeart && !campaign.CanPurifyEnding()))
                return false;

            Building_BlightfallHeart heart = state.CoreHeartThingId < 0 ? null :
                Find.Maps.Where(map => map != null && map.Parent == site)
                    .SelectMany(map => map.listerThings.AllThings)
                    .FirstOrDefault(thing => thing.thingIDNumber == state.CoreHeartThingId) as Building_BlightfallHeart;
            if (!campaign.CompleteFinalCoreSite(site, ending))
                return false;
            if (heart != null && !heart.Destroyed)
                heart.Destroy(DestroyMode.KillFinalize);
            return true;
        }

        public static bool TryHarnessInstabilityIncursion()
        {
            Map map = Find.Maps.FirstOrDefault(candidate => candidate != null && candidate.IsPlayerHome &&
                candidate.mapPawns.FreeColonists.Any());
            FactionDef insectDef = DefDatabase<FactionDef>.GetNamedSilentFail("Insect");
            Faction faction = insectDef == null ? null : Find.FactionManager.FirstFactionOfDef(insectDef);
            if (map == null || faction == null)
                return false;

            string[] names = { "LiAIChat_BlightCrawler", "LiAIChat_SporeSpitter" };
            List<Pawn> spawned = new List<Pawn>();
            for (int i = 0; i < 4; i++)
            {
                PawnKindDef kind = DefDatabase<PawnKindDef>.GetNamedSilentFail(names[i % names.Length]);
                if (kind == null)
                    continue;
                Pawn pawn = PawnGenerator.GeneratePawn(kind, faction, new PlanetTile(map.Tile));
                if (pawn == null)
                    continue;
                IntVec3 edge = CellFinder.RandomEdgeCell(map);
                GenSpawn.Spawn(pawn, CellFinder.RandomSpawnCellForPawnNear(edge, map, 10), map);
                if (pawn.Spawned)
                    spawned.Add(pawn);
                else if (!pawn.Destroyed)
                    pawn.Destroy(DestroyMode.Vanish);
            }
            if (spawned.Count == 0)
                return false;
            LordMaker.MakeNewLord(faction,
                new LordJob_AssaultColony(faction, false, false, false, false, false, false, false), map, spawned);
            Find.LetterStack?.ReceiveLetter("LiAIChat_BlightfallHarnessIncursionTitle".Translate(),
                "LiAIChat_BlightfallHarnessIncursionText".Translate(), LetterDefOf.ThreatBig, map.Parent);
            return true;
        }

        public static bool BeginFinalPurification(Site site)
        {
            BlightfallSiteState state = WorldComponent_Blightfall.Current?.GetSiteState(site);
            if (state == null || state.Type != BlightfallSiteType.BlightHeart ||
                state.CoreBattleStage != BlightfallCoreBattleStage.WeakenHeart)
                return false;
            EnterStage(site, state, BlightfallCoreBattleStage.FinalPurification);
            Log.Message("[LiAIChat Blightfall] Final purification phase started at site " + site.ID + ".");
            return true;
        }

        public static void SaveHeartHealth(Map map, BlightfallSiteState state)
        {
            if (map == null || state == null || state.CoreHeartThingId < 0)
                return;
            Building_BlightfallHeart heart = map.listerThings.AllThings.FirstOrDefault(thing =>
                thing.thingIDNumber == state.CoreHeartThingId && thing.Spawned && !thing.Destroyed)
                as Building_BlightfallHeart;
            if (heart != null && heart.MaxHitPoints > 0)
                state.CoreHeartHealthPercent = Mathf.Clamp01((float)heart.HitPoints / heart.MaxHitPoints);
        }

        private static void EnsureHeart(Map map, Site site, BlightfallSiteState state)
        {
            Building_BlightfallHeart existing = state.CoreHeartThingId < 0 ? null :
                map.listerThings.AllThings.FirstOrDefault(thing => thing.thingIDNumber == state.CoreHeartThingId &&
                    thing.Spawned && !thing.Destroyed) as Building_BlightfallHeart;
            if (existing != null)
                return;

            ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail("LiAIChat_BlightfallHeart");
            if (def == null)
            {
                Log.ErrorOnce("[LiAIChat Blightfall] Blight Heart ThingDef is missing.",
                    "LiAIChat_BlightfallHeartDefMissing".GetHashCode());
                return;
            }
            Building_BlightfallHeart heart = ThingMaker.MakeThing(def) as Building_BlightfallHeart;
            if (heart == null)
                return;
            if (!GenPlace.TryPlaceThing(heart, map.Center, map, ThingPlaceMode.Near) || !heart.Spawned)
            {
                if (!heart.Destroyed)
                    heart.Destroy(DestroyMode.Vanish);
                return;
            }
            heart.HitPoints = Mathf.Clamp(Mathf.RoundToInt(heart.MaxHitPoints * state.CoreHeartHealthPercent),
                1, heart.MaxHitPoints);
            state.CoreHeartThingId = heart.thingIDNumber;
            Log.Message("[LiAIChat Blightfall] Spawned Blight Heart objective " + heart.thingIDNumber +
                " at site " + site.ID + ".");
        }

        private static void EnsureStageTargets(Map map, Site site, BlightfallSiteState state)
        {
            if (state.CoreBattleStage == BlightfallCoreBattleStage.PerimeterBreach &&
                state.CorePerimeterTargetThingIds.Count == 0 && state.CorePerimeterTargetsRemaining > 0)
            {
                state.CorePerimeterTargetThingIds = SpawnTargets(map, site, "LiAIChat_BlightfallCorePerimeterPylon",
                    state.CorePerimeterTargetsRemaining);
            }
            else if (state.CoreBattleStage == BlightfallCoreBattleStage.DestroyCoreDefenses &&
                state.CoreDefenseTargetThingIds.Count == 0 && state.CoreDefenseTargetsRemaining > 0)
            {
                state.CoreDefenseTargetThingIds = SpawnTargets(map, site, "LiAIChat_BlightfallCoreDefenseGenerator",
                    state.CoreDefenseTargetsRemaining);
            }
        }

        private static List<int> SpawnTargets(Map map, Site site, string defName, int count)
        {
            List<int> ids = new List<int>();
            ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
            if (def == null)
            {
                Log.Error("[LiAIChat Blightfall] Missing core battle target ThingDef " + defName + ".");
                return ids;
            }
            Faction insectFaction = GetInsectFaction();
            for (int i = 0; i < count; i++)
            {
                Thing target = ThingMaker.MakeThing(def);
                if (insectFaction != null)
                    target.SetFaction(insectFaction);
                IntVec3 root = CellFinder.RandomClosewalkCellNear(map.Center, map, 7 + i * 2);
                if (GenPlace.TryPlaceThing(target, root, map, ThingPlaceMode.Near) && target.Spawned)
                    ids.Add(target.thingIDNumber);
                else if (!target.Destroyed)
                    target.Destroy(DestroyMode.Vanish);
            }
            if (ids.Count < count)
                Log.Warning("[LiAIChat Blightfall] Placed " + ids.Count + "/" + count + " objectives for " +
                    defName + " at site " + site.ID + ".");
            return ids;
        }

        private static bool SpawnReinforcementWave(Map map, Site site, Faction faction)
        {
            List<PawnKindDef> kinds = new List<PawnKindDef>
            {
                DefDatabase<PawnKindDef>.GetNamedSilentFail("LiAIChat_BlightCrawler"),
                DefDatabase<PawnKindDef>.GetNamedSilentFail("LiAIChat_SporeSpitter"),
                DefDatabase<PawnKindDef>.GetNamedSilentFail("LiAIChat_BlightBehemoth")
            };
            kinds.RemoveAll(kind => kind == null);
            if (kinds.Count == 0)
                return false;

            int colonists = map.mapPawns.FreeColonists.Count;
            int count = Mathf.Clamp(10 + colonists / 2, 10, 22);
            List<Pawn> spawned = new List<Pawn>(count);
            for (int i = 0; i < count; i++)
            {
                string preferred = i % 8 == 0 ? "LiAIChat_BlightBehemoth" :
                    i % 3 == 0 ? "LiAIChat_SporeSpitter" : "LiAIChat_BlightCrawler";
                PawnKindDef kind = kinds.FirstOrDefault(candidate => candidate.defName == preferred) ?? kinds.RandomElement();
                Pawn pawn = PawnGenerator.GeneratePawn(kind, faction, new PlanetTile(site.Tile.tileId));
                if (pawn == null)
                    continue;
                IntVec3 edge = CellFinder.RandomEdgeCell(map);
                IntVec3 cell = CellFinder.RandomSpawnCellForPawnNear(edge, map, 10);
                GenSpawn.Spawn(pawn, cell, map);
                if (pawn.Spawned)
                    spawned.Add(pawn);
                else if (!pawn.Destroyed)
                    pawn.Destroy(DestroyMode.Vanish);
            }
            if (spawned.Count == 0)
                return false;
            LordMaker.MakeNewLord(faction,
                new LordJob_AssaultColony(faction, false, false, false, false, false, false, false), map, spawned);
            return true;
        }

        private static bool HasLivingDefenders(Map map, Faction faction)
        {
            return faction != null && map.mapPawns.AllPawnsSpawned.Any(pawn => pawn != null && !pawn.Dead &&
                pawn.Faction == faction && pawn.HostileTo(Faction.OfPlayer));
        }

        private static void AddCorePollutionPulse(Map map)
        {
            if (map.pollutionGrid == null)
                return;
            float current = (float)map.pollutionGrid.TotalPollution / Mathf.Max(1, map.cellIndices.NumGridCells);
            PollutionUtility.PolluteMapToPercent(map, Mathf.Clamp01(Mathf.Max(0.82f, current) + 0.012f),
                Rand.RangeInclusive(7, 10));
        }

        private static void EnterStage(Site site, BlightfallSiteState state, BlightfallCoreBattleStage stage)
        {
            state.CoreBattleStage = stage;
            state.CorePerimeterTargetThingIds.Clear();
            state.CoreDefenseTargetThingIds.Clear();
            state.LastUpdatedTick = CurrentTick();
            if (stage == BlightfallCoreBattleStage.RepelReinforcements)
            {
                state.CoreReinforcementWavesSpawned = 0;
                state.CoreNextReinforcementTick = CurrentTick() + FirstReinforcementDelay;
            }
            else if (stage >= BlightfallCoreBattleStage.WeakenHeart)
            {
                state.CoreNextReinforcementTick = -1;
                state.CoreNextEnvironmentalPulseTick = -1;
            }
            AnnounceStage(site, stage);
        }

        private static void AnnounceStage(Site site, BlightfallCoreBattleStage stage)
        {
            LetterDef letter = stage == BlightfallCoreBattleStage.WeakenHeart ||
                stage == BlightfallCoreBattleStage.FinalPurification
                ? LetterDefOf.PositiveEvent
                : LetterDefOf.ThreatBig;
            Find.LetterStack?.ReceiveLetter("LiAIChat_BlightfallCoreBattleStageTitle".Translate(),
                ("LiAIChat_BlightfallCoreStage_" + stage).Translate(), letter, site);
        }

        private static Faction GetInsectFaction()
        {
            FactionDef insectDef = DefDatabase<FactionDef>.GetNamedSilentFail("Insect");
            return insectDef == null ? null : Find.FactionManager.FirstFactionOfDef(insectDef);
        }

        private static int CurrentTick() => Find.TickManager?.TicksGame ?? 0;
    }

    public sealed class Building_BlightfallCoreDefense : Building
    {
        public override string GetInspectString()
        {
            string baseText = base.GetInspectString();
            string phase = "LiAIChat_BlightfallCoreDefenseInspect".Translate();
            return string.IsNullOrEmpty(baseText) ? phase : baseText + "\n" + phase;
        }
    }

    public sealed class Building_BlightfallHeart : Building
    {
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
                yield return gizmo;

            BlightfallSiteState state = WorldComponent_Blightfall.Current?.GetSiteState(Map?.Parent as Site);
            if (state == null || state.Type != BlightfallSiteType.BlightHeart)
                yield break;

            if (state.CoreBattleStage == BlightfallCoreBattleStage.WeakenHeart)
            {
                yield return new Command_Action
                {
                    defaultLabel = "LiAIChat_BlightfallWeakenHeart".Translate(),
                    defaultDesc = ("LiAIChat_BlightfallCoreStage_" + state.CoreBattleStage).Translate(),
                    icon = def.uiIcon,
                    action = () => BlightfallFinalCoreUtility.BeginFinalPurification(Map.Parent as Site)
                };
            }

            if (state.CoreBattleStage == BlightfallCoreBattleStage.FinalPurification)
            {
                yield return new Command_Action
                {
                    defaultLabel = "LiAIChat_BlightfallEndingDestroyLabel".Translate(),
                    defaultDesc = "LiAIChat_BlightfallEndingDestroyDesc".Translate(),
                    icon = def.uiIcon,
                    action = () => BlightfallFinalCoreUtility.ChooseEnding(Map.Parent as Site,
                        BlightfallCampaignEnding.DestroyHeart)
                };
                yield return new Command_Action
                {
                    defaultLabel = "LiAIChat_BlightfallEndingPurifyLabel".Translate(),
                    defaultDesc = "LiAIChat_BlightfallEndingPurifyDesc".Translate(),
                    icon = def.uiIcon,
                    Disabled = WorldComponent_Blightfall.Current == null ||
                        !WorldComponent_Blightfall.Current.CanPurifyEnding(),
                    disabledReason = "LiAIChat_BlightfallEndingPurifyLocked".Translate(),
                    action = () => BlightfallFinalCoreUtility.ChooseEnding(Map.Parent as Site,
                        BlightfallCampaignEnding.PurifyHeart)
                };
                yield return new Command_Action
                {
                    defaultLabel = "LiAIChat_BlightfallEndingHarnessLabel".Translate(),
                    defaultDesc = "LiAIChat_BlightfallEndingHarnessDesc".Translate(),
                    icon = def.uiIcon,
                    action = () => BlightfallFinalCoreUtility.ChooseEnding(Map.Parent as Site,
                        BlightfallCampaignEnding.HarnessHeart)
                };
            }
        }

        public override string GetInspectString()
        {
            string baseText = base.GetInspectString();
            BlightfallSiteState state = WorldComponent_Blightfall.Current?.GetSiteState(Map?.Parent as Site);
            string phase = state == null ? string.Empty : "LiAIChat_BlightfallCoreStage_" + state.CoreBattleStage;
            string translated = phase.Translate();
            translated += "\n" + "LiAIChat_BlightfallCoreRetreatRule".Translate();
            return string.IsNullOrEmpty(baseText) ? translated : baseText + "\n" + translated;
        }
    }

    [HarmonyPatch(typeof(Thing), nameof(Thing.TakeDamage))]
    public static class BlightfallHeartDamageGatePatch
    {
        public static void Prefix(Thing __instance, ref DamageInfo dinfo)
        {
            if (__instance is Building_BlightfallHeart)
            {
                BlightfallSiteState state = WorldComponent_Blightfall.Current?.GetSiteState(
                    __instance.Map?.Parent as Site);
                if (state != null)
                    dinfo.SetAmount(0f);
            }
        }

        public static void Postfix(Thing __instance)
        {
            Building_BlightfallHeart heart = __instance as Building_BlightfallHeart;
            BlightfallSiteState state = heart == null ? null : WorldComponent_Blightfall.Current?.GetSiteState(
                heart.Map?.Parent as Site);
            if (state != null && state.CoreBattleStage != BlightfallCoreBattleStage.Completed &&
                heart.MaxHitPoints > 0)
                state.CoreHeartHealthPercent = Mathf.Clamp01((float)heart.HitPoints / heart.MaxHitPoints);
        }
    }
}
