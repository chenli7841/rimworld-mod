using Verse;
using RimWorld.Planet;

namespace LiAIChat.AlliedSettlementSurvival
{
    public sealed partial class AlliedSettlementWorldComponent
    {
        public AlliedCaravanGuardRecord FindActiveCaravanGuard(Pawn pawn)
        {
            if (pawn == null) return null;
            return caravanGuardRecords.Find(record => record != null && record.pawn == pawn &&
                record.sourceSettlement != null && !record.pawn.Dead);
        }

        public void RecordCaravanGuardDeath(AlliedCaravanGuardRecord record)
        {
            if (record == null || record.pawn == null || !record.pawn.Dead || !caravanGuardRecords.Contains(record))
                return;

            caravanGuardRecords.Remove(record);
            if (!AlliedSettlementSurvivalMod.Current.systemEnabled || record.sourceSettlement == null)
                return;

            AlliedSettlementState state = Get(record.sourceSettlement);
            if (state == null) return;
            state.strength = SettlementStrengthPolicy.Clamp(state.strength -
                AlliedCaravanReinforcementPolicy.DeathPenalty(record.role));
        }

        public void ForgetCaravanGuard(Pawn pawn)
        {
            if (pawn != null) caravanGuardRecords.RemoveAll(record => record != null && record.pawn == pawn);
        }
    }
}
