using System;
using System.Collections.Generic;
using System.Linq;
using LiAIChat.Game;
using LiAIChat.Archive;
using LiAIChat.Models;
using LiAIChat.State;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;

namespace LiAIChat.Questing
{
    public static class LostAnnotatorRecruitmentUtility
    {
        public const int RequiredConversations = 3;
        private const int OpportunityRefreshFrames = 30;

        private static readonly Dictionary<Map, JointStudyOpportunity>
            jointStudyOpportunities =
                new Dictionary<Map, JointStudyOpportunity>();

        public class JointStudyOpportunity
        {
            public Pawn Annotator;
            public Thing_AncientEarthArchiveFragment Book;
            public int LastCheckedFrame;
        }

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

        // Gizmos are rebuilt for every selected pawn on every UI repaint.
        // Keep the map-wide search out of that hot path. A brief frame cache
        // makes a multi-selection cost one scan instead of one scan per pawn.
        public static JointStudyOpportunity GetJointStudyOpportunity(Map map)
        {
            if (map == null)
            {
                return null;
            }

            JointStudyOpportunity cached;
            if (jointStudyOpportunities.TryGetValue(map, out cached) &&
                Time.frameCount - cached.LastCheckedFrame <
                    OpportunityRefreshFrames)
            {
                return cached.Annotator == null
                    ? null
                    : cached;
            }

            JointStudyOpportunity refreshed =
                new JointStudyOpportunity
                {
                    Annotator = FindCandidateOnMap(map),
                    LastCheckedFrame = Time.frameCount
                };

            if (refreshed.Annotator != null)
            {
                refreshed.Book = FindBook(map);
            }

            jointStudyOpportunities[map] = refreshed;

            return refreshed.Annotator == null
                ? null
                : refreshed;
        }

        public static bool TryStartJointStudy(
            Pawn researcher,
            JointStudyOpportunity opportunity)
        {
            if (researcher == null || researcher.Drafted ||
                opportunity == null || opportunity.Annotator == null ||
                opportunity.Book == null ||
                researcher.Map != opportunity.Annotator.Map ||
                researcher.Map != opportunity.Book.Map ||
                !CanStillStudyWith(opportunity.Annotator) ||
                !opportunity.Book.Identified ||
                opportunity.Book.Destroyed ||
                opportunity.Book.SoldByPlayer ||
                !researcher.CanReserveAndReach(
                    opportunity.Book,
                    PathEndMode.Touch,
                    Danger.Some) ||
                !researcher.CanReserve(opportunity.Annotator))
            {
                return false;
            }

            researcher.jobs.TryTakeOrderedJob(JobMaker.MakeJob(
                DefDatabase<JobDef>.GetNamed(
                    "LiAIChat_LostAnnotatorJointStudy"),
                opportunity.Book,
                opportunity.Annotator));

            return true;
        }

        private static Pawn FindCandidateOnMap(Map map)
        {
            LiAIChatGameComponent component =
                Current.Game?.GetComponent<LiAIChatGameComponent>();

            if (component == null || component.PawnStates == null)
            {
                return null;
            }

            foreach (PawnAIState state in component.PawnStates)
            {
                if (state == null || !state.IsLostAnnotator ||
                    !state.LostAnnotatorRescued ||
                    state.LostAnnotatorPermanentMember)
                {
                    continue;
                }

                Pawn pawn = map.mapPawns.AllPawnsSpawned.FirstOrDefault(
                    candidate => candidate.thingIDNumber == state.PawnId);

                if (pawn != null && !pawn.Dead)
                {
                    return pawn;
                }
            }

            return null;
        }

        private static bool CanStillStudyWith(Pawn scholar)
        {
            PawnAIState state;
            return IsCandidate(scholar, out state) &&
                !state.LostAnnotatorJointStudyCompleted;
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
