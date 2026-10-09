using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace LiAIChat.AlliedSettlementSurvival
{
    public sealed class AlliedSettlementState : IExposable
    {
        public Settlement settlement;
        public float strength = SettlementStrengthPolicy.InitialStrength;
        public int registeredTick;
        public int lastTick;
        public bool active;
        public bool crisisActive;

        public void ExposeData()
        {
            Scribe_References.Look(ref settlement, "settlement");
            Scribe_Values.Look(ref strength, "strength", SettlementStrengthPolicy.InitialStrength);
            Scribe_Values.Look(ref registeredTick, "registeredTick");
            Scribe_Values.Look(ref lastTick, "lastTick");
            Scribe_Values.Look(ref active, "active");
            Scribe_Values.Look(ref crisisActive, "crisisActive");
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
            }
        }

        public override void WorldComponentTick()
        {
            if (Find.TickManager.TicksGame % 2500 == 0) Refresh();
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
                    state = new AlliedSettlementState { settlement = settlement, registeredTick = now, lastTick = now, active = true };
                    settlements.Add(state);
                }
                state.strength = SettlementStrengthPolicy.Recover(state.strength, now - state.lastTick,
                    eligible && state.active, state.crisisActive);
                // Advance even while frozen, preventing catch-up recovery after re-alliance.
                state.lastTick = now;
                state.active = eligible;
            }
        }
    }
}
