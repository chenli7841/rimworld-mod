using LiAIChat.Archive;
using LiAIChat.State;
using RimWorld;
using System.Linq;
using Verse;
using Verse.AI;

namespace LiAIChat.Questing
{
    public static class LostAnnotatorRescueManager
    {
        public static void Check()
        {
            EnsureLostAnnotatorPersonas();
            if (!HasActiveLostAnnotatorQuest()) return;
            foreach (Map map in Find.Maps)
            {
                if (map == null || !map.IsPlayerHome) continue;
                foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
                {
                    var state = PawnAIStateManager.TryGetExistingState(pawn);
                    if (state == null || !state.IsLostAnnotator || state.LostAnnotatorRescued || pawn.Dead) continue;
                    if (!IsArchiveRecovered(state))
                    {
                        Messages.Message("学者拒绝离开：他的远古文献仍在敌人营地中。", pawn, MessageTypeDefOf.RejectInput);
                        continue;
                    }
                    state.LostAnnotatorRescued = true;
                    ArchiveScholarStayUtility.StartStay(pawn, 300000, "", "");
                    ArchiveScholarGiftUtility.TryGiveArchiveToColony(pawn);
                    EndLostAnnotatorQuest();
                    Messages.Message(pawn.LabelShort + " 安全抵达殖民地，并愿意暂住协助解读文献。", pawn, MessageTypeDefOf.PositiveEvent);
                    LiAIChat.Events.ColonyEventLog.Record("注疏者获救",
                        pawn.LabelShort + " 已安全抵达殖民地，愿意暂住协助解读文献。", 3, pawn,
                        "lost-annotator-rescued:" + pawn.thingIDNumber);
                }
            }
        }

        public static bool IsArchiveRecovered(LiAIChat.Models.PawnAIState state)
        {
            if (state == null || state.LostAnnotatorArchiveThingId < 0) return false;
            foreach (Map map in Find.Maps)
            {
                if (map == null || !map.IsPlayerHome) continue;
                if (map.listerThings.AllThings.Any(t => t.thingIDNumber == state.LostAnnotatorArchiveThingId)) return true;
            }
            return false;
        }

        // Upgrades annotators from saves made before the dedicated persona was
        // introduced, including scholars who have already reached the colony.
        private static void EnsureLostAnnotatorPersonas()
        {
            foreach (Map map in Find.Maps)
            {
                if (map?.mapPawns == null) continue;
                foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
                {
                    var state = PawnAIStateManager.TryGetExistingState(pawn);
                    if (state != null && state.IsLostAnnotator && !pawn.Dead)
                    {
                        ArchiveScholarInitializer.ConfigureLostAnnotator(pawn);
                        if (!state.LostAnnotatorRescued && !map.IsPlayerHome &&
                            pawn.CurJob?.def?.defName != "LiAIChat_WaitForLostAnnotatorRescue")
                        {
                            pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(
                                DefDatabase<JobDef>.GetNamed("LiAIChat_WaitForLostAnnotatorRescue"), pawn.Position));
                        }
                        HandleTemporaryStay(pawn, state);
                    }
                }
            }
        }

        private static void HandleTemporaryStay(Pawn pawn, LiAIChat.Models.PawnAIState state)
        {
            if (!state.LostAnnotatorRescued || state.LostAnnotatorPermanentMember ||
                state.LostAnnotatorDepartureHandled || state.ScholarStay == null ||
                !state.ScholarStay.Active || Find.TickManager.TicksGame < state.ScholarStay.EndTick)
                return;
            if (LostAnnotatorRecruitmentUtility.MeetsRequirements(pawn)) return;

            state.ScholarStay.Active = false;
            state.ScholarStay.Completed = true;
            state.LostAnnotatorDepartureHandled = true;
            pawn.DeSpawn();
            Find.WorldPawns.PassToWorld(pawn);
            Messages.Message(pawn.LabelShort + " 感谢了这段暂住时光，随后继续了自己的学术旅程。", MessageTypeDefOf.NeutralEvent);
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
