using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse.AI.Group;
using Verse;
using Verse.AI;
using RimWorld.Planet;

namespace LiAIChat.AlliedSettlementSurvival
{
    public sealed partial class AlliedSettlementWorldComponent
    {
        private const int MilitaryAidClearDelayTicks = 2500;

        private void TickMilitaryAidEvacuation()
        {
            if (militaryAidLossGroups == null || caravanGuardRecords == null)
                return;

            int now = Find.TickManager.TicksGame;
            foreach (AlliedMilitaryAidLossGroup group in militaryAidLossGroups
                .Where(candidate => candidate != null && candidate.evacuateWhenThreatCleared).ToList())
            {
                List<Pawn> survivors = caravanGuardRecords
                    .Where(record => record != null && record.militaryAidGroupId == group.groupId)
                    .Select(record => record.pawn)
                    .Where(pawn => pawn != null && pawn.Spawned && !pawn.Dead && pawn.Map != null)
                    .Distinct().ToList();
                Map[] maps = survivors.Select(pawn => pawn.Map).Distinct().ToArray();
                if (maps.Length != 1)
                {
                    group.hostileFreeSinceTick = -1;
                    continue;
                }

                Map map = maps[0];
                bool hostilesRemain = map.mapPawns.AllPawnsSpawned.Any(pawn => pawn != null && !pawn.Dead &&
                    !pawn.Downed && pawn.Faction != null && pawn.Faction.HostileTo(Faction.OfPlayer));
                if (hostilesRemain)
                {
                    group.hostileFreeSinceTick = -1;
                    continue;
                }

                if (group.hostileFreeSinceTick < 0)
                {
                    group.hostileFreeSinceTick = now;
                    continue;
                }
                if (now - group.hostileFreeSinceTick < MilitaryAidClearDelayTicks)
                    continue;

                foreach (Lord lord in survivors.Select(pawn =>
                    {
                        Lord found;
                        return LordUtility.TryGetLord(pawn, out found) ? found : null;
                    }).Where(lord => lord != null).Distinct())
                {
                    lord.SetJob(new LordJob_ExitMapBest(LocomotionUrgency.Jog, false, true), true);
                }
                group.evacuateWhenThreatCleared = false;
                Messages.Message("LiASS_BlightfallAlliesWithdrawing".Translate(), map.Parent,
                    MessageTypeDefOf.PositiveEvent);
            }
        }

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
