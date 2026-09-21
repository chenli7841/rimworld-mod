using LiAIChat.State;
using RimWorld;
using Verse;

namespace LiAIChat.Questing
{
    public class QuestPart_LostAnnotatorStatus : QuestPart
    {
        public override string DescriptionPart
        {
            get
            {
                foreach (Map map in Find.Maps)
                foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
                {
                    var state = PawnAIStateManager.TryGetExistingState(pawn);
                    if (state == null || !state.IsLostAnnotator) continue;
                    if (state.LostAnnotatorRescued || map.IsPlayerHome) return "营救状态：学者已安全抵达殖民地。";
                    return "营救状态：学者仍被困在遗迹中。";
                }
                return "营救状态：学者正在返程。";
            }
        }
    }
}
