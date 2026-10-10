using System.Collections.Generic;
using System.Linq;
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
            bool systemEnabled = AlliedSettlementSurvivalMod.Current.systemEnabled;
            float penalty = systemEnabled
                ? AlliedCaravanReinforcementPolicy.DeathPenalty(record.role)
                : 0f;
            if (systemEnabled && !string.IsNullOrEmpty(record.militaryAidGroupId))
            {
                AlliedMilitaryAidLossGroup group = GetOrCreateMilitaryAidLossGroup(record.militaryAidGroupId);
                penalty = AlliedCaravanReinforcementPolicy.CappedMilitaryAidDeathPenalty(
                    group.strengthLossAlreadyApplied, record.role);
                group.strengthLossAlreadyApplied += penalty;
            }

            if (systemEnabled && record.sourceSettlement != null)
            {
                AlliedSettlementState state = Get(record.sourceSettlement);
                if (state != null)
                    state.strength = SettlementStrengthPolicy.Clamp(state.strength - penalty);
            }

            CleanupMilitaryAidLossGroup(record.militaryAidGroupId);
        }

        public void ForgetCaravanGuard(Pawn pawn)
        {
            if (pawn == null || caravanGuardRecords == null) return;
            List<string> groupIds = caravanGuardRecords
                .Where(record => record != null && record.pawn == pawn &&
                    !string.IsNullOrEmpty(record.militaryAidGroupId))
                .Select(record => record.militaryAidGroupId)
                .Distinct()
                .ToList();
            caravanGuardRecords.RemoveAll(record => record != null && record.pawn == pawn);
            foreach (string groupId in groupIds)
                CleanupMilitaryAidLossGroup(groupId);
        }

        private AlliedMilitaryAidLossGroup GetOrCreateMilitaryAidLossGroup(string groupId)
        {
            if (militaryAidLossGroups == null)
                militaryAidLossGroups = new List<AlliedMilitaryAidLossGroup>();
            AlliedMilitaryAidLossGroup group = militaryAidLossGroups
                .FirstOrDefault(candidate => candidate != null && candidate.groupId == groupId);
            if (group == null)
            {
                group = new AlliedMilitaryAidLossGroup { groupId = groupId };
                militaryAidLossGroups.Add(group);
            }
            return group;
        }

        private void CleanupMilitaryAidLossGroup(string groupId)
        {
            if (string.IsNullOrEmpty(groupId) || militaryAidLossGroups == null)
                return;
            bool stillHasTrackedPawns = caravanGuardRecords != null && caravanGuardRecords
                .Any(record => record != null && record.militaryAidGroupId == groupId);
            if (!stillHasTrackedPawns)
                militaryAidLossGroups.RemoveAll(group => group != null && group.groupId == groupId);
        }
    }
}
