using HarmonyLib;
using LiAIChat.State;
using RimWorld;
using Verse;

namespace LiAIChat.Patches
{
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.Kill))]
    public static class PawnKillLostAnnotatorPatch
    {
        public static void Postfix(Pawn __instance)
        {
            if (__instance == null) return;
            var state = PawnAIStateManager.GetState(__instance);
            if (state == null || !state.IsLostAnnotator || state.LostAnnotatorRescued) return;
            foreach (Quest quest in Find.QuestManager.QuestsListForReading)
            {
                if (quest != null && quest.root != null && quest.root.defName == "LiAIChat_LostAnnotator" && quest.State == QuestState.Ongoing)
                { quest.End(QuestEndOutcome.Fail, true, true); break; }
            }
        }
    }
}
