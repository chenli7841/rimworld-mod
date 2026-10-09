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

        public string SourceSettlementId => sourceSettlement?.GetUniqueLoadID();

        public void ExposeData()
        {
            Scribe_References.Look(ref pawn, "pawn");
            Scribe_References.Look(ref sourceSettlement, "sourceSettlement");
            Scribe_Values.Look(ref role, "role", AlliedCaravanGuardRole.Guard);
        }
    }

    public sealed partial class AlliedSettlementWorldComponent
    {
        private List<AlliedCaravanGuardRecord> caravanGuardRecords = new List<AlliedCaravanGuardRecord>();

        public void RegisterCaravanGuards(Settlement source, IEnumerable<Pawn> pawns,
            Pawn leader, IEnumerable<Pawn> eliteGuards)
        {
            if (source == null || pawns == null) return;
            HashSet<Pawn> eliteSet = eliteGuards == null ? new HashSet<Pawn>() : new HashSet<Pawn>(eliteGuards);
            foreach (Pawn pawn in pawns)
            {
                if (pawn == null || caravanGuardRecords.Exists(record => record != null && record.pawn == pawn))
                    continue;
                caravanGuardRecords.Add(new AlliedCaravanGuardRecord
                {
                    pawn = pawn,
                    sourceSettlement = source,
                    role = pawn == leader ? AlliedCaravanGuardRole.Leader :
                        eliteSet.Contains(pawn) ? AlliedCaravanGuardRole.Elite : AlliedCaravanGuardRole.Guard
                });
            }
        }

    }
}
