using System;
using System.Linq;
using LiAIChat.Archive;
using LiAIChat.Models;
using LiAIChat.State;
using RimWorld;
using Verse;
using Verse.AI;

namespace LiAIChat.Questing
{
    public static class LostAnnotatorRecruitmentUtility
    {
        public const int RequiredConversations = 3;

        public static bool IsCandidate(Pawn pawn, out PawnAIState state)
        {
            state = pawn == null ? null : PawnAIStateManager.TryGetExistingState(pawn);
            return state != null && state.IsLostAnnotator && state.LostAnnotatorRescued &&
                !state.LostAnnotatorPermanentMember && !pawn.Dead;
        }

        public static bool MeetsRequirements(Pawn scholar)
        {
            PawnAIState state;
            if (!IsCandidate(scholar, out state) || state.ScholarStay == null) return false;
            return state.ScholarStay.MeaningfulConversationCount >= RequiredConversations &&
                state.LostAnnotatorJointStudyCompleted && HasKnowledgeHome(scholar.Map) &&
                scholar.needs?.mood != null && scholar.needs.mood.CurLevel >= 0.45f;
        }

        public static string RequirementSummary(Pawn scholar)
        {
            PawnAIState state;
            if (!IsCandidate(scholar, out state)) return "这名学者已不再处于可招募的暂住状态。";
            int talks = state.ScholarStay?.MeaningfulConversationCount ?? 0;
            return "留下条件：有内容的交谈 " + talks + "/" + RequiredConversations +
                "；共同解读 " + (state.LostAnnotatorJointStudyCompleted ? "完成" : "未完成") +
                "；殖民地收藏至少 3 本已识别文献，并有书架和文稿桌；学者心情良好。";
        }

        public static bool HasKnowledgeHome(Map map)
        {
            if (map == null || !map.IsPlayerHome) return false;
            int books = map.listerThings.AllThings.OfType<Thing_AncientEarthArchiveFragment>()
                .Count(book => book.Identified && !book.Destroyed);
            bool shelf = map.listerThings.AllThings.Any(thing => thing.def != null &&
                thing.def.defName.IndexOf("Bookshelf", StringComparison.OrdinalIgnoreCase) >= 0);
            bool desk = map.listerThings.AllThings.Any(thing => thing.def != null &&
                thing.def.defName == "LiAIChat_WritingDesk");
            return books >= 3 && shelf && desk;
        }

        public static Thing_AncientEarthArchiveFragment FindBook(Map map)
        {
            return map?.listerThings.AllThings.OfType<Thing_AncientEarthArchiveFragment>()
                .FirstOrDefault(book => book.Identified && !book.Destroyed && !book.SoldByPlayer);
        }

        public static void CompleteJointStudy(Pawn researcher, Pawn scholar,
            Thing_AncientEarthArchiveFragment book)
        {
            PawnAIState state;
            if (!IsCandidate(scholar, out state) || state.LostAnnotatorJointStudyCompleted) return;
            state.LostAnnotatorJointStudyCompleted = true;
            AncientArchiveStudyUtility.CompleteStudy(researcher, book, false);
            Messages.Message("共同解读完成。" + scholar.LabelShort + " 对殖民地的学术诚意留下了深刻印象。",
                scholar, MessageTypeDefOf.PositiveEvent);
        }
    }

    public class JobDriver_LostAnnotatorJointStudy : JobDriver
    {
        private Thing_AncientEarthArchiveFragment Book => TargetA.Thing as Thing_AncientEarthArchiveFragment;
        private Pawn Scholar => TargetB.Thing as Pawn;
        public override bool TryMakePreToilReservations(bool errorOnFailed) =>
            pawn.Reserve(Book, job, 1, -1, null, errorOnFailed) && pawn.Reserve(Scholar, job, 1, -1, null, errorOnFailed);
        protected override System.Collections.Generic.IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A); this.FailOnDespawnedOrNull(TargetIndex.B);
            this.FailOn(() => pawn.Drafted || Scholar.Drafted || Scholar.Map != pawn.Map);
            Toil invite = ToilMaker.MakeToil(); invite.initAction = () => Scholar.jobs.TryTakeOrderedJob(JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("LiAIChat_AssistLostAnnotatorStudy"), Book, pawn)); yield return invite;
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            Toil study = ToilMaker.MakeToil(); study.defaultCompleteMode = ToilCompleteMode.Delay; study.defaultDuration = 2500;
            study.FailOn(() => Scholar.CurJob?.def.defName != "LiAIChat_AssistLostAnnotatorStudy" || Scholar.Position.DistanceToSquared(Book.Position) > 9);
            study.tickAction = () => pawn.rotationTracker.FaceTarget(Scholar);
            study.AddFinishAction(() =>
            {
                if (Book != null && Scholar != null)
                    LostAnnotatorRecruitmentUtility.CompleteJointStudy(pawn, Scholar, Book);
            });
            study.WithProgressBarToilDelay(TargetIndex.A); yield return study;
        }
    }

    public class JobDriver_AssistLostAnnotatorStudy : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override System.Collections.Generic.IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            Toil assist = ToilMaker.MakeToil(); assist.defaultCompleteMode = ToilCompleteMode.Delay; assist.defaultDuration = 2600; yield return assist;
        }
    }
}
