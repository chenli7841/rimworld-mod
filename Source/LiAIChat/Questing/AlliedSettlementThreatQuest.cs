using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using LiAIChat.AlliedSettlementSurvival;
using RimWorld;
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
        private List<Pawn> defenders = new List<Pawn>();
        private List<Pawn> retreatedDefenders = new List<Pawn>();
        internal bool HasSpawnedLivingDefender => defenders.Any(pawn => pawn != null && pawn.Spawned && !pawn.Dead);

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
        public static void Postfix(Site site, ref int __result)
        {
            QuestPart_AlliedSettlementThreat part = FindThreatPart(site);
            if (part == null) return;

            if (part.defenderCount <= 0)
                part.defenderCount = AlliedCaravanReinforcementPolicy.AlliedThreatDefenderCount(
                    PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive
                        .Count(pawn => pawn != null && pawn.IsColonistPlayerControlled));
            __result = part.defenderCount;
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
                !threatPart.HasSpawnedLivingDefender &&
                !threatPart.mechanoidFallbackAttempted)
                SpawnMissingMechanoidDefenders(threatPart, __instance.Map);
        }

        private static void SpawnMissingMechanoidDefenders(QuestPart_AlliedSettlementThreat threatPart, Map map)
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

                IntVec3 edge;
                if (!CellFinder.TryFindRandomEdgeCellWith(cell => cell.Standable(map), map, Rot4.North, 0f, out edge))
                    edge = CellFinder.RandomEdgeCell(Rot4.North, map);
                foreach (Pawn pawn in generated)
                {
                    IntVec3 cell = CellFinder.RandomSpawnCellForPawnNear(edge, map, 10);
                    GenSpawn.Spawn(pawn, cell, map);
                }
                LordMaker.MakeNewLord(threatPart.faction,
                    new LordJob_AssaultColony(threatPart.faction, false, false, false, false, false, false, false),
                    map, generated);
                threatPart.ObserveDefenders(map);
                Log.Message("[LiAIChat] Spawned " + generated.Count + " mechanoid defenders for allied threat site " +
                    threatPart.site?.Label + " (target=" + targetCount + ").");
            }
            catch (Exception error)
            {
                foreach (Pawn pawn in generated)
                    if (pawn != null && !pawn.Spawned && !pawn.Destroyed)
                        pawn.Destroy(DestroyMode.Vanish);
                Log.Error("[LiAIChat] Could not spawn mechanoid defenders for allied threat site: " + error);
            }
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
