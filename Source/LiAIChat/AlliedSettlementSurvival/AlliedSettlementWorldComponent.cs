using System.Collections.Generic;
using System.Linq;
using LiAIChat.Questing;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace LiAIChat.AlliedSettlementSurvival
{
    public enum AlliedSettlementCrisisKind
    {
        None,
        FoodShortage,
        DiseaseOutbreak,
        HostileThreat
    }

    public sealed class AlliedSettlementState : IExposable
    {
        public Settlement settlement;
        public float strength = SettlementStrengthPolicy.InitialStrength;
        public int registeredTick;
        public int lastTick;
        public bool active;
        public bool crisisActive;
        public int nextCrisisTick;
        public int crisisDeadlineTick;
        public int lastCrisisTick;
        public int crisisCount;
        public AlliedSettlementCrisisKind crisisKind;
        public Quest aidQuest;
        public Quest threatQuest;

        public const int ProtectionTicks = 15 * SettlementStrengthPolicy.TicksPerDay;
        public const int MinimumCrisisIntervalTicks = 20 * SettlementStrengthPolicy.TicksPerDay;
        public const int MaximumCrisisIntervalTicks = 35 * SettlementStrengthPolicy.TicksPerDay;
        public const int CrisisDeadlineTicks = 10 * SettlementStrengthPolicy.TicksPerDay;
        public const int MaximumConcurrentCrises = 2;
        public const float FailedCrisisStrengthLoss = 20f;

        public void ExposeData()
        {
            Scribe_References.Look(ref settlement, "settlement");
            Scribe_Values.Look(ref strength, "strength", SettlementStrengthPolicy.InitialStrength);
            Scribe_Values.Look(ref registeredTick, "registeredTick");
            Scribe_Values.Look(ref lastTick, "lastTick");
            Scribe_Values.Look(ref active, "active");
            Scribe_Values.Look(ref crisisActive, "crisisActive");
            Scribe_Values.Look(ref nextCrisisTick, "nextCrisisTick");
            Scribe_Values.Look(ref crisisDeadlineTick, "crisisDeadlineTick");
            Scribe_Values.Look(ref lastCrisisTick, "lastCrisisTick");
            Scribe_Values.Look(ref crisisCount, "crisisCount");
            Scribe_Values.Look(ref crisisKind, "crisisKind", AlliedSettlementCrisisKind.None);
            Scribe_References.Look(ref aidQuest, "aidQuest");
            Scribe_References.Look(ref threatQuest, "threatQuest");
            if (Scribe.mode == LoadSaveMode.PostLoadInit) strength = SettlementStrengthPolicy.Clamp(strength);
        }
    }

    // Separate save namespace: no changes to archive, knowledge or Pawn AI persistence.
    public sealed class AlliedSettlementWorldComponent : WorldComponent
    {
        private List<AlliedSettlementState> settlements = new List<AlliedSettlementState>();
        private List<string> migratedPawnIds = new List<string>();
        public bool HasMigrated(Pawn pawn) => migratedPawnIds.Contains(pawn.GetUniqueLoadID());
        public void RecordMigration(Pawn pawn) { if (!HasMigrated(pawn)) migratedPawnIds.Add(pawn.GetUniqueLoadID()); }
        public IEnumerable<AlliedSettlementState> States => settlements;
        public static AlliedSettlementWorldComponent Current => Find.World?.GetComponent<AlliedSettlementWorldComponent>();

        public AlliedSettlementWorldComponent(World world) : base(world) { }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref settlements, "liAlliedSettlements", LookMode.Deep);
            Scribe_Collections.Look(ref migratedPawnIds, "liAlliedSettlementMigrants", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (settlements == null) settlements = new List<AlliedSettlementState>();
                if (migratedPawnIds == null) migratedPawnIds = new List<string>();
                settlements.RemoveAll(s => s == null || s.settlement == null);
                int now = Find.TickManager.TicksGame;
                foreach (AlliedSettlementState state in settlements)
                {
                    // Older saves gain a fresh grace period on their first load with this scheduler.
                    if (state.nextCrisisTick <= 0) state.nextCrisisTick = now + AlliedSettlementState.ProtectionTicks;
                    if (state.crisisActive && state.crisisDeadlineTick <= 0)
                        state.crisisDeadlineTick = now + AlliedSettlementState.CrisisDeadlineTicks;
                    if (!state.crisisActive) state.crisisKind = AlliedSettlementCrisisKind.None;
                    if (state.lastCrisisTick <= 0) state.lastCrisisTick = now;
                }
            }
        }

        public override void WorldComponentTick()
        {
            if (Find.TickManager.TicksGame % 2500 == 0)
            {
                Refresh();
                TickCrises();
            }
        }

        public AlliedSettlementState Get(Settlement settlement) => settlements.FirstOrDefault(s => s.settlement == settlement);

        public bool Eligible(Settlement settlement)
        {
            if (settlement == null || !settlement.Spawned || settlement.Faction == null ||
                settlement.Faction == Faction.OfPlayer || settlement.Faction.defeated ||
                settlement.Faction.RelationKindWith(Faction.OfPlayer) != FactionRelationKind.Ally ||
                !settlement.Tile.Valid || !settlement.Tile.Layer.IsRootSurface) return false;
            return Find.Maps.Any(map => map.IsPlayerHome && map.Tile.Valid && map.Tile.Layer == settlement.Tile.Layer &&
                Find.WorldGrid.ApproxDistanceInTiles(map.Tile, settlement.Tile) <= SettlementStrengthPolicy.Radius);
        }

        public void Refresh()
        {
            int now = Find.TickManager.TicksGame;
            settlements.RemoveAll(s => s.settlement == null || !s.settlement.Spawned);
            foreach (Settlement settlement in Find.WorldObjects.Settlements)
            {
                bool eligible = Eligible(settlement);
                AlliedSettlementState state = Get(settlement);
                if (state == null)
                {
                    if (!eligible) continue;
                    state = new AlliedSettlementState
                    {
                        settlement = settlement,
                        registeredTick = now,
                        lastTick = now,
                        active = true,
                        nextCrisisTick = now + AlliedSettlementState.ProtectionTicks,
                        lastCrisisTick = now
                    };
                    settlements.Add(state);
                }
                state.strength = SettlementStrengthPolicy.Recover(state.strength, now - state.lastTick,
                    eligible && state.active, state.crisisActive);
                // Advance even while frozen, preventing catch-up recovery after re-alliance.
                state.lastTick = now;
                if (!state.active)
                {
                    // Pause both scheduled starts and active deadlines while outside the system.
                    int pausedTicks = System.Math.Max(0, now - state.lastCrisisTick);
                    state.nextCrisisTick += pausedTicks;
                    if (state.crisisActive) state.crisisDeadlineTick += pausedTicks;
                }
                state.lastCrisisTick = now;
                state.active = eligible;
            }
        }

        private void TickCrises()
        {
            int now = Find.TickManager.TicksGame;
            int activeCount = settlements.Count(s => s.crisisActive);
            foreach (AlliedSettlementState state in settlements.OrderBy(s => s.nextCrisisTick).ToList())
            {
                if (!state.active || state.settlement == null || state.strength <= 0f) continue;

                if (state.crisisActive)
                {
                    if (now < state.crisisDeadlineTick) continue;
                    FailCrisis(state, now);
                    activeCount--;
                    continue;
                }

                if (now < state.nextCrisisTick || activeCount >= AlliedSettlementState.MaximumConcurrentCrises) continue;
                if (StartCrisis(state, now)) activeCount++;
            }
        }

        private bool StartCrisis(AlliedSettlementState state, int now)
        {
            AlliedSettlementCrisisKind kind = (AlliedSettlementCrisisKind)Rand.RangeInclusive(1, 3);
            Quest aidQuest = null;
            Quest threatQuest = null;
            if (kind == AlliedSettlementCrisisKind.FoodShortage || kind == AlliedSettlementCrisisKind.DiseaseOutbreak)
            {
                if (!AlliedSettlementAidQuestUtility.TryCreate(this, state, kind, out aidQuest))
                {
                    state.nextCrisisTick = now + 2500;
                    return false;
                }
            }
            else if (kind == AlliedSettlementCrisisKind.HostileThreat &&
                !AlliedSettlementThreatQuestUtility.TryCreate(state, out threatQuest))
            {
                state.nextCrisisTick = now + 2500;
                return false;
            }

            state.crisisKind = kind;
            state.crisisActive = true;
            state.crisisDeadlineTick = now + AlliedSettlementState.CrisisDeadlineTicks;
            state.aidQuest = aidQuest;
            state.threatQuest = threatQuest;
            state.crisisCount++;
            string kindLabel = CrisisKindLabel(state.crisisKind);
            Find.LetterStack.ReceiveLetter(
                "LiASS_CrisisLetterLabel".Translate(state.settlement.LabelCap),
                "LiASS_CrisisLetterText".Translate(state.settlement.LabelCap, kindLabel,
                    (AlliedSettlementState.CrisisDeadlineTicks / (float)SettlementStrengthPolicy.TicksPerDay).ToString("0")),
                LetterDefOf.NegativeEvent,
                state.settlement,
                null,
                threatQuest ?? aidQuest);
            return true;
        }

        private static void FailCrisis(AlliedSettlementState state, int now)
        {
            AlliedSettlementCrisisKind failedKind = state.crisisKind;
            float oldStrength = state.strength;
            Quest failedQuest = state.aidQuest;
            Quest failedThreatQuest = state.threatQuest;
            state.strength = SettlementStrengthPolicy.Clamp(state.strength - AlliedSettlementState.FailedCrisisStrengthLoss);
            state.crisisActive = false;
            state.crisisKind = AlliedSettlementCrisisKind.None;
            state.crisisDeadlineTick = 0;
            state.aidQuest = null;
            state.threatQuest = null;
            state.nextCrisisTick = now + Rand.RangeInclusive(
                AlliedSettlementState.MinimumCrisisIntervalTicks,
                AlliedSettlementState.MaximumCrisisIntervalTicks);
            Find.LetterStack.ReceiveLetter(
                "LiASS_CrisisFailedLabel".Translate(state.settlement.LabelCap),
                "LiASS_CrisisFailedText".Translate(state.settlement.LabelCap,
                    CrisisKindLabel(failedKind), oldStrength.ToString("0.#"), state.strength.ToString("0.#")),
                LetterDefOf.NegativeEvent,
                state.settlement);
            if (failedQuest != null && failedQuest.State == QuestState.Ongoing)
                failedQuest.End(QuestEndOutcome.Fail, false);
            if (failedThreatQuest != null && failedThreatQuest.State == QuestState.Ongoing)
                failedThreatQuest.End(QuestEndOutcome.Fail, false);
        }

        public bool CompleteCrisis(Settlement settlement, AlliedSettlementCrisisKind expectedKind)
        {
            AlliedSettlementState state = Get(settlement);
            if (state == null || !state.crisisActive || state.crisisKind != expectedKind) return false;
            Quest completedQuest = state.aidQuest;
            Quest completedThreatQuest = state.threatQuest;
            state.crisisActive = false;
            state.crisisKind = AlliedSettlementCrisisKind.None;
            state.crisisDeadlineTick = 0;
            state.aidQuest = null;
            state.threatQuest = null;
            state.nextCrisisTick = Find.TickManager.TicksGame + Rand.RangeInclusive(
                AlliedSettlementState.MinimumCrisisIntervalTicks,
                AlliedSettlementState.MaximumCrisisIntervalTicks);
            Find.LetterStack.ReceiveLetter(
                "LiASS_CrisisResolvedLabel".Translate(settlement.LabelCap),
                "LiASS_CrisisResolvedText".Translate(settlement.LabelCap),
                LetterDefOf.PositiveEvent,
                settlement);
            if (completedQuest != null && completedQuest.State == QuestState.Ongoing)
                completedQuest.End(QuestEndOutcome.Success, false);
            if (completedThreatQuest != null && completedThreatQuest.State == QuestState.Ongoing)
                completedThreatQuest.End(QuestEndOutcome.Success, false);
            return true;
        }

        private static string CrisisKindLabel(AlliedSettlementCrisisKind kind) =>
            ("LiASS_CrisisKind_" + kind).Translate();

    }
}
