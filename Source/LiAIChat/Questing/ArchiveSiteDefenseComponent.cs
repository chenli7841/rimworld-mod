using System.Collections.Generic;
using Verse;

namespace LiAIChat.Questing
{
    public class ArchiveSiteDefenseComponent
        : MapComponent
    {
        private List<int> defenderPawnIds =
            new List<int>();
        
        public bool HasRegisteredDefenders
        {
            get
            {
                return defenderPawnIds != null &&
                       defenderPawnIds.Count > 0;
            }
        }

        public ArchiveSiteDefenseComponent(
            Map map)
            : base(map)
        {
        }

        public IReadOnlyList<int>
            DefenderPawnIds
        {
            get
            {
                return defenderPawnIds;
            }
        }

        public void RegisterDefender(
            Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            int pawnId =
                pawn.thingIDNumber;

            if (defenderPawnIds.Contains(
                pawnId))
            {
                return;
            }

            defenderPawnIds.Add(
                pawnId);
        }

        public bool IsRegisteredDefender(
            Pawn pawn)
        {
            if (pawn == null)
            {
                return false;
            }

            return defenderPawnIds.Contains(
                pawn.thingIDNumber);
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Collections.Look(
                ref defenderPawnIds,
                "liAIArchiveDefenderPawnIds",
                LookMode.Value);

            if (defenderPawnIds == null)
            {
                defenderPawnIds =
                    new List<int>();
            }
        }
    }
}