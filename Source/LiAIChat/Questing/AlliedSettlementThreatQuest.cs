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
                faction = QuestGen.slate.Get<Faction>("threatFaction")
            });
        }
    }

    public class QuestPart_AlliedSettlementThreat : QuestPartActivable
    {
        public Settlement settlement;
        public Site site;
        public Faction faction;
        private List<Pawn> defenders = new List<Pawn>();

        public override string DescriptionPart
        {
            get
            {
                if (quest == null || quest.State != QuestState.Ongoing) return null;
                int standing = defenders.Count(pawn => pawn != null && !pawn.Dead && !pawn.Downed);
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

            // Destroying the camp itself is an explicit victory, even if no defenders
            // were observed before the site was removed. Otherwise, require that we
            // observed defenders and that none remain standing. An empty generated
            // map alone is not enough to resolve the crisis.
            bool campDestroyed = site != null && (site.Destroyed ||
                AlliedSettlementThreatCompletionPatch.WasClearedByVanilla(site));
            bool defendersDefeated = defenders.Count > 0 &&
                !defenders.Any(pawn => pawn != null && !pawn.Dead && !pawn.Downed);
            if (!campDestroyed && !defendersDefeated)
                return;

            AlliedSettlementWorldComponent.Current?.CompleteCrisis(
                settlement, AlliedSettlementCrisisKind.HostileThreat);
        }

        public void ObserveDefenders(Map map)
        {
            if (map?.mapPawns == null || defenders == null) return;
            foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
                if (pawn != null && pawn.Faction == faction && !defenders.Contains(pawn))
                    defenders.Add(pawn);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref settlement, "settlement");
            Scribe_References.Look(ref site, "site");
            Scribe_References.Look(ref faction, "faction");
            Scribe_Collections.Look(ref defenders, "defenders", LookMode.Reference);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && defenders == null)
                defenders = new List<Pawn>();
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

            PlanetTile tile;
            if (!TileFinder.TryFindTileWithDistance(state.settlement.Tile, 2, 7, out tile,
                    null, TileFinderMode.Near, true))
                return false;

            Site site = null;
            try
            {
                site = SiteMaker.MakeSite(part, tile, faction, true, ThreatPoints, null);
                if (site == null) return false;
                Find.WorldObjects.Add(site);

                var slate = new Slate();
                slate.Set("alliedSettlement", state.settlement);
                slate.Set("threatSite", site);
                slate.Set("threatFaction", faction);
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
            if (!IsAlliedThreatSite(site)) return;

            int colonists = PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive
                .Count(pawn => pawn != null && pawn.IsColonistPlayerControlled);
            __result = AlliedCaravanReinforcementPolicy.AlliedThreatDefenderCount(colonists);
        }

        private static bool IsAlliedThreatSite(Site site)
        {
            return site != null && Find.QuestManager != null &&
                Find.QuestManager.QuestsListForReading.Any(quest => quest != null &&
                    quest.State == QuestState.Ongoing &&
                    quest.PartsListForReading.OfType<QuestPart_AlliedSettlementThreat>()
                        .Any(part => part.site == site));
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
            threatPart?.ObserveDefenders(__instance.Map);
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
}
