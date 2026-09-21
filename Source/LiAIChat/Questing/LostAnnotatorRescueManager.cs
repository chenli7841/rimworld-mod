using LiAIChat.Archive;
using LiAIChat.State;
using RimWorld;
using Verse;

namespace LiAIChat.Questing
{
    public static class LostAnnotatorRescueManager
    {
        public static void Check()
        {
            if (!HasActiveLostAnnotatorQuest()) return;
            foreach (Map map in Find.Maps)
            {
                if (map == null || !map.IsPlayerHome) continue;
                foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
                {
                    var state = PawnAIStateManager.TryGetExistingState(pawn);
                    if (state == null || !state.IsLostAnnotator || state.LostAnnotatorRescued || pawn.Dead) continue;
                    state.LostAnnotatorRescued = true;
                    ArchiveScholarStayUtility.StartStay(pawn, 300000, "", "");
                    ArchiveScholarGiftUtility.TryGiveArchiveToColony(pawn);
                    EndLostAnnotatorQuest();
                    Messages.Message(pawn.LabelShort + " 安全抵达殖民地，并愿意暂住协助解读文献。", pawn, MessageTypeDefOf.PositiveEvent);
                }
            }
        }

        private static void EndLostAnnotatorQuest()
        {
            if (Find.QuestManager == null) return;
            foreach (Quest quest in Find.QuestManager.QuestsListForReading)
            {
                if (quest != null && quest.root != null &&
                    quest.root.defName == "LiAIChat_LostAnnotator" &&
                    quest.State == QuestState.Ongoing)
                {
                    quest.End(QuestEndOutcome.Success, true, true);
                    return;
                }
            }
        }

        private static bool HasActiveLostAnnotatorQuest()
        {
            if (Find.QuestManager == null) return false;
            foreach (Quest quest in Find.QuestManager.QuestsListForReading)
                if (quest != null && quest.root != null &&
                    quest.root.defName == "LiAIChat_LostAnnotator" && quest.State == QuestState.Ongoing)
                    return true;
            return false;
        }
    }
}
