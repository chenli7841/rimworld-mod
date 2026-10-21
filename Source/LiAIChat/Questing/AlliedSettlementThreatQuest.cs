using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using LiAIChat.AlliedSettlementSurvival;
using RimWorld;
using RimWorld.BaseGen;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse;
using Verse.AI.Group;

namespace LiAIChat.Questing
{
    public class QuestNode_CreateAlliedSettlementThreat : QuestNode
    {
        protected override bool TestRunInt(Slate slate) =>
            slate.Get<Settlement>("alliedSettlement") != null && slate.Get<Site>("threatSite") != null;

        protected override void RunInt()
        {
            QuestGen.quest.AddPart(new QuestPart_AlliedSettlementThreat
            {
                settlement = QuestGen.slate.Get<Settlement>("alliedSettlement"),
                site = QuestGen.slate.Get<Site>("threatSite"),
                faction = QuestGen.slate.Get<Faction>("threatFaction"),
                defenderCount = QuestGen.slate.Get("threatDefenderCount", 1)
            });
        }
    }

    public class QuestPart_AlliedSettlementThreat : QuestPartActivable
    {
        public Settlement settlement;
        public Site site;
        public Faction faction;
        public int defenderCount;
        public bool mechanoidFallbackAttempted;
        public bool mechanoidArrivalDeploymentComplete;
        public bool mechanoidDeploymentWaitLogged;
        public bool mechanoidCenterSpawnAwaitingLanding;
        private List<Pawn> defenders = new List<Pawn>();
        private List<Pawn> retreatedDefenders = new List<Pawn>();
        internal IEnumerable<Pawn> TrackedDefenders => defenders;

        public override string DescriptionPart
        {
            get
            {
                if (quest == null || quest.State != QuestState.Ongoing) return null;
                int standing = defenders.Count(pawn => pawn != null && !pawn.Dead && !pawn.Downed &&
                    !retreatedDefenders.Contains(pawn));
                return defenders.Count == 0
                    ? "LiASS_ThreatAwaitingDefenders".Translate()
                    : "LiASS_ThreatDefenderProgress".Translate(standing);
            }
        }

        public override IEnumerable<GlobalTargetInfo> QuestLookTargets
        {
            get
            {
                if (site != null) yield return new GlobalTargetInfo(site);
                if (settlement != null) yield return new GlobalTargetInfo(settlement);
            }
        }

        public override void QuestPartTick()
        {
            base.QuestPartTick();
            if (quest.State != QuestState.Ongoing || Find.TickManager.TicksGame % 250 != 0)
                return;

            // Site.PostMapGenerate runs before GravshipUtility finishes placing the
            // player's ship. Poll the loaded site map so the fallback deployment is
            // retried after the landing engine actually exists, even if a Harmony
            // hook on the placement method is skipped by a game/modded call path.
            if (site?.Map is Map siteMap && faction == Faction.OfMechanoids &&
                (!mechanoidArrivalDeploymentComplete || mechanoidCenterSpawnAwaitingLanding))
            {
                Building_GravEngine playerEngine = siteMap.listerThings.AllThings
                    .OfType<Building_GravEngine>()
                    .FirstOrDefault(engine => engine.Spawned && engine.Faction == Faction.OfPlayer);
                if (playerEngine != null)
                {
                    AlliedSettlementThreatDefenderCapturePatch
                        .DeployMechanoidDefendersAfterGravshipLanding(siteMap, playerEngine);
                    mechanoidDeploymentWaitLogged = false;
                }
                else if (!mechanoidDeploymentWaitLogged)
                {
                    mechanoidDeploymentWaitLogged = true;
                    Log.Message("[LiAIChat] Mechanoid defenders are waiting for the player gravship to land at " +
                        site.Label + "; quest tick will retry deployment.");
                }
            }

            // Keep checking the recorded defenders after the player leaves the camp.
            // The site map is unloaded at that point, but the cached pawn references
            // still tell us whether the defenders were defeated.
            if (site != null && site.Map != null)
                ObserveDefenders(site.Map);

            TryCompleteCrisisAfterDefendersDefeated();
        }

        public void ObserveDefenders(Map map)
        {
            if (map?.mapPawns == null || defenders == null) return;
            foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
                if (pawn != null && pawn.Faction == faction && !defenders.Contains(pawn))
                    defenders.Add(pawn);
        }

        public void NotifyDefenderKilled(Pawn pawn)
        {
            if (pawn == null || defenders == null || !defenders.Contains(pawn)) return;
            TryCompleteCrisisAfterDefendersDefeated();
        }

        private void TryCompleteCrisisAfterDefendersDefeated()
        {
            if (quest == null || quest.State != QuestState.Ongoing) return;
            if (site != null && site.Map != null)
                ObserveDefenders(site.Map);

            // Pawns killed by explosions or other mods can be destroyed without
            // retaining Dead/Downed state. Treat destroyed pawns as defeated too.
            bool campDestroyed = site != null && (site.Destroyed ||
                AlliedSettlementThreatCompletionPatch.WasClearedByVanilla(site));
            bool defendersDefeated = defenders.Count > 0 &&
                !defenders.Any(pawn => pawn != null && !pawn.Destroyed && !pawn.Dead && !pawn.Downed &&
                    !retreatedDefenders.Contains(pawn));
            if (!campDestroyed && !defendersDefeated) return;

            bool completed = AlliedSettlementWorldComponent.Current?.CompleteCrisis(
                settlement, AlliedSettlementCrisisKind.HostileThreat) ?? false;
            if (!completed)
            {
                AlliedSettlementState state = AlliedSettlementWorldComponent.Current?.Get(settlement);
                Log.Warning("[LiAIChat] Allied threat site was cleared but its crisis could not complete. Site=" +
                    site?.Label + "; quest=" + quest.State + "; crisisActive=" + state?.crisisActive +
                    "; crisisKind=" + state?.crisisKind + "; defenders=" + defenders.Count + ".");
            }
        }

        public void NotifyDefenderRetreated(Pawn pawn, Map map)
        {
            if (pawn == null || map == null || site?.Map != map || pawn.Faction != faction)
                return;

            ObserveDefenders(map);
            if (!defenders.Contains(pawn))
                defenders.Add(pawn);
            if (retreatedDefenders.Contains(pawn))
                return;

            retreatedDefenders.Add(pawn);
            Log.Message("[LiAIChat] Enemy defender retreated from allied threat site: " + pawn.LabelShort +
                "; site=" + site.Label + "; remaining defenders=" + defenders.Count(candidate =>
                    candidate != null && !candidate.Destroyed && !candidate.Dead && !candidate.Downed &&
                    !retreatedDefenders.Contains(candidate)));
            TryCompleteCrisisAfterDefendersDefeated();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref settlement, "settlement");
            Scribe_References.Look(ref site, "site");
            Scribe_References.Look(ref faction, "faction");
            Scribe_Values.Look(ref defenderCount, "defenderCount");
            Scribe_Values.Look(ref mechanoidFallbackAttempted, "mechanoidFallbackAttempted");
            Scribe_Values.Look(ref mechanoidArrivalDeploymentComplete, "mechanoidArrivalDeploymentComplete");
            Scribe_Values.Look(ref mechanoidDeploymentWaitLogged, "mechanoidDeploymentWaitLogged");
            Scribe_Values.Look(ref mechanoidCenterSpawnAwaitingLanding, "mechanoidCenterSpawnAwaitingLanding");
            Scribe_Collections.Look(ref defenders, "defenders", LookMode.Reference);
            Scribe_Collections.Look(ref retreatedDefenders, "retreatedDefenders", LookMode.Reference);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (defenders == null) defenders = new List<Pawn>();
                if (retreatedDefenders == null) retreatedDefenders = new List<Pawn>();
            }
        }
    }

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.Kill))]
    public static class AlliedSettlementThreatDefenderDeathPatch
    {
        public static void Postfix(Pawn __instance)
        {
            if (__instance == null || Find.QuestManager == null) return;
            foreach (QuestPart_AlliedSettlementThreat threatPart in Find.QuestManager.QuestsListForReading
                .Where(quest => quest != null && quest.State == QuestState.Ongoing)
                .SelectMany(quest => quest.PartsListForReading.OfType<QuestPart_AlliedSettlementThreat>())
                .ToList())
                threatPart.NotifyDefenderKilled(__instance);
        }
    }

    public static class AlliedSettlementThreatQuestUtility
    {
        private static float ThreatPoints => AlliedSettlementSurvivalMod.Current.threatPoints;

        public static bool TryCreate(AlliedSettlementState state, out Quest quest)
        {
            quest = null;
            if (state?.settlement == null || Find.QuestManager == null)
                return false;

            QuestScriptDef def = DefDatabase<QuestScriptDef>.GetNamedSilentFail("LiAIChat_AlliedSettlementThreat");
            SitePartDef part = DefDatabase<SitePartDef>.GetNamedSilentFail("BanditCamp");
            if (def == null || part == null)
            {
                Log.ErrorOnce("[LiAIChat] Allied settlement threat quest or BanditCamp site part is missing.",
                    "LiAIChat_AlliedSettlementThreat".GetHashCode());
                return false;
            }

            Faction faction = CandidateFactions().Where(candidate => candidate != null &&
                candidate.HostileTo(Faction.OfPlayer) && candidate.HostileTo(state.settlement.Faction) &&
                PawnGroupMakerUtility.CanGenerateAnyNormalGroup(candidate, ThreatPoints)).RandomElementWithFallback();
            if (faction == null)
                return false;

            int colonistCount = PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive
                .Count(pawn => pawn != null && pawn.IsColonistPlayerControlled);
            int defenderCount = AlliedCaravanReinforcementPolicy.AlliedThreatDefenderCount(colonistCount);

            PlanetTile tile;
            if (!TileFinder.TryFindTileWithDistance(state.settlement.Tile, 2, 7, out tile,
                    null, TileFinderMode.Near, true))
                return false;

            Site site = null;
            try
            {
                site = SiteMaker.MakeSite(part, tile, faction, true, ThreatPoints, null);
                if (site == null) return false;
                SitePart outpostPart = site.parts.FirstOrDefault(sitePart => sitePart?.def?.workerClass == typeof(SitePartWorker_Outpost));
                if (outpostPart != null)
                    outpostPart.expectedEnemyCount = defenderCount;
                Find.WorldObjects.Add(site);

                var slate = new Slate();
                slate.Set("alliedSettlement", state.settlement);
                slate.Set("threatSite", site);
                slate.Set("threatFaction", faction);
                slate.Set("threatDefenderCount", defenderCount);
                quest = QuestGen.Generate(def, slate);
                if (quest == null)
                {
                    Find.WorldObjects.Remove(site);
                    site = null;
                    return false;
                }

                quest.SetInitiallyAccepted();
                Find.QuestManager.Add(quest);
                state.threatQuest = quest;
                return true;
            }
            catch (Exception error)
            {
                if (site != null && !site.Destroyed)
                    Find.WorldObjects.Remove(site);
                quest = null;
                Log.Error("[LiAIChat] Could not create allied settlement threat quest: " + error);
                return false;
            }
        }

        private static IEnumerable<Faction> CandidateFactions()
        {
            if (Faction.OfMechanoids != null) yield return Faction.OfMechanoids;
            if (Faction.OfInsects != null) yield return Faction.OfInsects;
            foreach (Faction faction in Find.FactionManager.AllFactionsListForReading)
                if (faction != null && faction.def.humanlikeFaction && !faction.def.hidden && !faction.defeated)
                    yield return faction;
        }
    }

    // BanditCamp uses the vanilla outpost worker and combat group generation.
    // Override only the defender count for sites attached to this quest, leaving
    // ordinary outposts and all of their vanilla generation untouched.
    [HarmonyPatch(typeof(SitePartWorker_Outpost), "GetEnemiesCount")]
    public static class AlliedSettlementThreatDefenderCountPatch
    {
        public static bool Prefix(Site site, ref int __result)
        {
            QuestPart_AlliedSettlementThreat part = FindThreatPart(site);
            if (part?.faction != Faction.OfMechanoids)
                return true;

            // GetPostProcessedThreatLabel calls GetEnemiesCount while the site is
            // selected on the world map. The vanilla count enumerates Settlement
            // pawn kinds and logs an error for Mechanoid, which has no such maker.
            // This site already has its stable, saved defender count, so skip that
            // enumeration entirely for our mechanoid threat sites.
            __result = GetDefenderCount(part);
            return false;
        }

        public static void Postfix(Site site, ref int __result)
        {
            QuestPart_AlliedSettlementThreat part = FindThreatPart(site);
            if (part == null) return;

            __result = GetDefenderCount(part);
        }

        private static int GetDefenderCount(QuestPart_AlliedSettlementThreat part)
        {
            if (part.defenderCount <= 0)
                part.defenderCount = AlliedCaravanReinforcementPolicy.AlliedThreatDefenderCount(
                    PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive
                        .Count(pawn => pawn != null && pawn.IsColonistPlayerControlled));
            return part.defenderCount;
        }

        private static QuestPart_AlliedSettlementThreat FindThreatPart(Site site)
        {
            if (site == null || Find.QuestManager == null) return null;
            return Find.QuestManager.QuestsListForReading
                .Where(quest => quest != null &&
                    quest.State == QuestState.Ongoing &&
                    quest.PartsListForReading != null)
                .SelectMany(quest => quest.PartsListForReading.OfType<QuestPart_AlliedSettlementThreat>())
                .FirstOrDefault(part => part.site == site);
        }
    }

    // The vanilla outpost resolver requests the Settlement pawn-group kind. The
    // Mechanoid faction has no maker for that kind, so suppress only that step for
    // these quest sites; the post-map hook creates a normal Mechanoid Combat group.
    [HarmonyPatch(typeof(SymbolResolver_Settlement), nameof(SymbolResolver_Settlement.Resolve))]
    public static class AlliedMechanoidThreatSettlementPawnGenerationPatch
    {
        public static void Prefix(ref ResolveParams rp)
        {
            Site site = rp.sitePart?.site;
            QuestPart_AlliedSettlementThreat threatPart =
                AlliedSettlementThreatDefenderCapturePatch.FindThreatPart(site);
            if (threatPart?.faction == Faction.OfMechanoids)
                rp.settlementDontGeneratePawns = true;
        }
    }

    // Capture all camp defenders as soon as world-site map generation finishes.
    // This closes the fast-clear/fast-exit window before the first periodic quest tick.
    [HarmonyPatch(typeof(Site), "PostMapGenerate")]
    public static class AlliedSettlementThreatDefenderCapturePatch
    {
        public static void Postfix(Site __instance)
        {
            if (__instance == null || __instance.Map == null || Find.QuestManager == null) return;

            QuestPart_AlliedSettlementThreat threatPart = FindThreatPart(__instance);
            if (threatPart == null) return;

            threatPart.ObserveDefenders(__instance.Map);
            if (threatPart.faction == Faction.OfMechanoids &&
                !threatPart.mechanoidArrivalDeploymentComplete)
            {
                // This map-generation hook is known to run for gravship arrivals.
                // Spawn at map center immediately so defenders cannot be lost when
                // a later gravship-placement or quest-tick hook is skipped.
                DeployMechanoidDefendersAfterGravshipLanding(__instance.Map, null);
            }
        }

        internal static void DeployMechanoidDefendersAfterGravshipLanding(Map map, Building_GravEngine engine)
        {
            if (map?.Parent is not Site site) return;
            QuestPart_AlliedSettlementThreat threatPart = FindThreatPart(site);
            if (threatPart == null || threatPart.faction != Faction.OfMechanoids ||
                (threatPart.mechanoidArrivalDeploymentComplete &&
                 !threatPart.mechanoidCenterSpawnAwaitingLanding))
                return;

            threatPart.ObserveDefenders(map);
            IntVec3 landingAnchor = engine != null && engine.Spawned && engine.Map == map
                ? engine.Position
                : map.Center;
            List<Pawn> activeDefenders = threatPart.TrackedDefenders
                .Where(pawn => pawn != null && !pawn.Destroyed && !pawn.Dead && !pawn.Downed)
                .ToList();

            if (activeDefenders.Count > 0)
            {
                // Older builds spawned the generated group at the map edge before the
                // gravship landed. Redeploy those pawns near the landing zone so the
                // player can actually see and engage them through the site's fog.
                foreach (Pawn pawn in activeDefenders)
                {
                    if (pawn.Spawned && pawn.Map == map)
                        pawn.DeSpawn(DestroyMode.Vanish);
                    SpawnNearLanding(pawn, map, landingAnchor);
                }
                activeDefenders = activeDefenders.Where(pawn => pawn.Spawned && pawn.Map == map).ToList();
                if (activeDefenders.Count > 0)
                {
                    LordMaker.MakeNewLord(threatPart.faction,
                        new LordJob_AssaultColony(threatPart.faction, false, false, false, false, false, false, false),
                        map, activeDefenders);
                    threatPart.ObserveDefenders(map);
                    threatPart.mechanoidArrivalDeploymentComplete = true;
                    threatPart.mechanoidCenterSpawnAwaitingLanding = false;
                    Log.Message("[LiAIChat] Deployed " + activeDefenders.Count + " existing mechanoid defenders near " +
                        "the gravship landing at " + site.Label + "; landing=" + landingAnchor + ".");
                }
                return;
            }

            Pawn previouslyTracked = threatPart.TrackedDefenders.FirstOrDefault(pawn => pawn != null);
            if (previouslyTracked != null)
            {
                // Do not create a replacement wave if the previous group was already
                // defeated or downed before the player returned to the site.
                threatPart.NotifyDefenderKilled(previouslyTracked);
                return;
            }

            SpawnMissingMechanoidDefenders(threatPart, map, landingAnchor, engine == null);
        }

        private static void SpawnMissingMechanoidDefenders(
            QuestPart_AlliedSettlementThreat threatPart, Map map, IntVec3 landingAnchor,
            bool awaitGravshipLanding)
        {
            threatPart.mechanoidFallbackAttempted = true;
            int targetCount = Math.Max(1, threatPart.defenderCount);
            var generated = new List<Pawn>();
            float pointsPerAttempt = Math.Max(AlliedSettlementSurvivalMod.Current.threatPoints, targetCount * 100f);

            try
            {
                // Outpost settlement generation does not reliably create a Combat group for mechanoid factions.
                // Use the faction's normal Combat pawn-group maker so its pawn kinds and tech remain vanilla.
                for (int attempt = 0; attempt < 4 && generated.Count < targetCount; attempt++)
                {
                    var parms = new PawnGroupMakerParms
                    {
                        faction = threatPart.faction,
                        groupKind = PawnGroupKindDefOf.Combat,
                        points = pointsPerAttempt,
                        tile = map.Tile,
                        generateFightersOnly = true
                    };
                    generated.AddRange(PawnGroupMakerUtility.GeneratePawns(parms, true));
                }

                if (generated.Count > targetCount)
                {
                    foreach (Pawn excess in generated.Skip(targetCount).ToList())
                        if (excess != null && !excess.Destroyed)
                            excess.Destroy(DestroyMode.Vanish);
                    generated.RemoveRange(targetCount, generated.Count - targetCount);
                }

                if (generated.Count == 0)
                {
                    Log.Error("[LiAIChat] No mechanoid Combat pawns could be generated for allied threat site " +
                        threatPart.site?.Label + ".");
                    return;
                }

                var spawned = new List<Pawn>();
                foreach (Pawn pawn in generated)
                {
                    if (SpawnNearLanding(pawn, map, landingAnchor))
                        spawned.Add(pawn);
                }
                if (spawned.Count == 0)
                {
                    Log.Error("[LiAIChat] Generated mechanoid defenders but none spawned on the map at " +
                        threatPart.site?.Label + ".");
                    return;
                }

                LordMaker.MakeNewLord(threatPart.faction,
                    new LordJob_AssaultColony(threatPart.faction, false, false, false, false, false, false, false),
                    map, spawned);
                threatPart.ObserveDefenders(map);
                threatPart.mechanoidArrivalDeploymentComplete = true;
                threatPart.mechanoidCenterSpawnAwaitingLanding = awaitGravshipLanding;
                Log.Message("[LiAIChat] Spawned " + spawned.Count + " mechanoid defenders for allied threat site " +
                    threatPart.site?.Label + " (target=" + targetCount + ").");
                Log.Message("[LiAIChat] Mechanoid defender landing=" + landingAnchor + "; positions=" +
                    string.Join(", ", spawned.Select(pawn => pawn.Position.ToString())) + ".");
            }
            catch (Exception error)
            {
                foreach (Pawn pawn in generated)
                    if (pawn != null && !pawn.Spawned && !pawn.Destroyed)
                        pawn.Destroy(DestroyMode.Vanish);
                Log.Error("[LiAIChat] Could not spawn mechanoid defenders for allied threat site: " + error);
            }
        }

        private static bool SpawnNearLanding(Pawn pawn, Map map, IntVec3 landingAnchor)
        {
            if (pawn == null || map == null) return false;
            IntVec3 cell = CellFinder.RandomSpawnCellForPawnNear(landingAnchor, map, 18);
            GenSpawn.Spawn(pawn, cell, map);
            return pawn.Spawned && pawn.Map == map;
        }

        internal static QuestPart_AlliedSettlementThreat FindThreatPart(Site site)
        {
            if (site == null || Find.QuestManager == null) return null;
            return Find.QuestManager.QuestsListForReading
                .Where(quest => quest != null && quest.State == QuestState.Ongoing)
                .SelectMany(quest => quest.PartsListForReading.OfType<QuestPart_AlliedSettlementThreat>())
                .FirstOrDefault(part => part.site == site);
        }
    }

    [HarmonyPatch(typeof(WorldComponent_GravshipController), "PlaceGravship")]
    public static class AlliedSettlementMechanoidLandingDeploymentPatch
    {
        public static void Postfix(Building_GravEngine __result, Map map)
        {
            AlliedSettlementThreatDefenderCapturePatch.DeployMechanoidDefendersAfterGravshipLanding(map, __result);
        }
    }

    // The vanilla Site records this state when all hostiles are dead or downed.
    // It survives map unloads, so the crisis can still resolve after caravan exit.
    [HarmonyPatch(typeof(Site), "CheckAllEnemiesDefeated")]
    public static class AlliedSettlementThreatCompletionPatch
    {
        private static readonly FieldInfo AllEnemiesDefeatedField =
            AccessTools.Field(typeof(Site), "allEnemiesDefeatedSignalSent");

        public static bool WasClearedByVanilla(Site site)
        {
            return site != null && AllEnemiesDefeatedField != null &&
                (bool)AllEnemiesDefeatedField.GetValue(site);
        }

        public static void Postfix(Site __instance)
        {
            if (!WasClearedByVanilla(__instance)) return;
            QuestPart_AlliedSettlementThreat threatPart =
                AlliedSettlementThreatDefenderCapturePatch.FindThreatPart(__instance);
            if (threatPart != null)
                AlliedSettlementWorldComponent.Current?.CompleteCrisis(
                    threatPart.settlement, AlliedSettlementCrisisKind.HostileThreat);
        }
    }

    // Mark tracked hostile pawns that leave the threat-site map as having retreated.
    // This covers enemy lords that evacuate rather than fighting to the last pawn.
    [HarmonyPatch(typeof(Pawn), "ExitMap")]
    public static class AlliedSettlementThreatDefenderRetreatPatch
    {
        public static void Prefix(Pawn __instance, out Map __state)
        {
            __state = __instance?.Map;
        }

        public static void Postfix(Pawn __instance, Map __state)
        {
            if (__instance == null || __state == null || Find.QuestManager == null)
                return;

            foreach (QuestPart_AlliedSettlementThreat threatPart in Find.QuestManager.QuestsListForReading
                .Where(quest => quest != null && quest.State == QuestState.Ongoing)
                .SelectMany(quest => quest.PartsListForReading.OfType<QuestPart_AlliedSettlementThreat>())
                .ToList())
            {
                threatPart.NotifyDefenderRetreated(__instance, __state);
            }
        }
    }
}
