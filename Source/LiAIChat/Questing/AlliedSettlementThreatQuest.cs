using System;
using System.Collections.Generic;
using System.Linq;
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
                else if (settlement != null) yield return new GlobalTargetInfo(settlement);
            }
        }

        public override void QuestPartTick()
        {
            base.QuestPartTick();
            if (quest.State != QuestState.Ongoing || site == null || site.Map == null ||
                Find.TickManager.TicksGame % 250 != 0)
                return;

            foreach (Pawn pawn in site.Map.mapPawns.AllPawnsSpawned)
                if (pawn != null && pawn.Faction == faction && !defenders.Contains(pawn))
                    defenders.Add(pawn);

            // A generated but empty map is not a victory; only observed defenders can satisfy this objective.
            if (defenders.Count == 0 || defenders.Any(pawn => pawn != null && !pawn.Dead && !pawn.Downed))
                return;

            AlliedSettlementWorldComponent.Current?.CompleteCrisis(
                settlement, AlliedSettlementCrisisKind.HostileThreat);
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
        private const float ThreatPoints = 500f;

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
}
