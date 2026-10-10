using System;
using System.Collections.Generic;
using Verse;
using RimWorld.Planet;

namespace LiAIChat.AlliedSettlementSurvival
{
    public sealed class AlliedCaravanGuardRecord : IExposable
    {
        public Pawn pawn;
        public Settlement sourceSettlement;
        public AlliedCaravanGuardRole role;
        public string militaryAidGroupId;

        public string SourceSettlementId => sourceSettlement?.GetUniqueLoadID();

        public void ExposeData()
        {
            Scribe_References.Look(ref pawn, "pawn");
            Scribe_References.Look(ref sourceSettlement, "sourceSettlement");
            Scribe_Values.Look(ref role, "role", AlliedCaravanGuardRole.Guard);
            Scribe_Values.Look(ref militaryAidGroupId, "militaryAidGroupId");
        }
    }

    public sealed class AlliedMilitaryAidLossGroup : IExposable
    {
        public string groupId;
        public float strengthLossAlreadyApplied;

        public void ExposeData()
        {
            Scribe_Values.Look(ref groupId, "groupId");
            Scribe_Values.Look(ref strengthLossAlreadyApplied, "strengthLossAlreadyApplied", 0f);
        }
    }

    public sealed partial class AlliedSettlementWorldComponent
    {
        private List<AlliedCaravanGuardRecord> caravanGuardRecords = new List<AlliedCaravanGuardRecord>();
        private List<AlliedMilitaryAidLossGroup> militaryAidLossGroups = new List<AlliedMilitaryAidLossGroup>();

        public void RegisterCaravanGuards(Settlement source, IEnumerable<Pawn> pawns,
            Pawn leader, IEnumerable<Pawn> eliteGuards)
        {
            RegisterTrackedGuards(source, pawns, leader, eliteGuards, null);
        }

        public void RegisterMilitaryAidGuards(Settlement source, IEnumerable<Pawn> pawns,
            Pawn leader, IEnumerable<Pawn> eliteGuards)
        {
            if (source == null || pawns == null) return;
            string groupId = Guid.NewGuid().ToString("N");
            RegisterTrackedGuards(source, pawns, leader, eliteGuards, groupId);
            if (caravanGuardRecords.Exists(record => record != null && record.militaryAidGroupId == groupId))
            {
                if (militaryAidLossGroups == null)
                    militaryAidLossGroups = new List<AlliedMilitaryAidLossGroup>();
                militaryAidLossGroups.Add(new AlliedMilitaryAidLossGroup { groupId = groupId });
            }
        }

        private void RegisterTrackedGuards(Settlement source, IEnumerable<Pawn> pawns,
            Pawn leader, IEnumerable<Pawn> eliteGuards, string militaryAidGroupId)
        {
            if (source == null || pawns == null) return;
            if (caravanGuardRecords == null)
                caravanGuardRecords = new List<AlliedCaravanGuardRecord>();
            HashSet<Pawn> eliteSet = eliteGuards == null ? new HashSet<Pawn>() : new HashSet<Pawn>(eliteGuards);
            foreach (Pawn pawn in pawns)
            {
                if (pawn == null || caravanGuardRecords.Exists(record => record != null && record.pawn == pawn))
                    continue;
                caravanGuardRecords.Add(new AlliedCaravanGuardRecord
                {
                    pawn = pawn,
                    sourceSettlement = source,
                    militaryAidGroupId = militaryAidGroupId,
                    role = pawn == leader ? AlliedCaravanGuardRole.Leader :
                        eliteSet.Contains(pawn) ? AlliedCaravanGuardRole.Elite : AlliedCaravanGuardRole.Guard
                });
            }
        }

    }
}
