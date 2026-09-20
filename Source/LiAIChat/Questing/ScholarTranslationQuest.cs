using System.Collections.Generic;
using System.Linq;
using LiAIChat.Archive;
using LiAIChat.Civilization;
using LiAIChat.State;
using RimWorld;
using RimWorld.QuestGen;
using Verse;
using Verse.AI;

namespace LiAIChat.Questing
{
    // TestRun only inspects existing books: no pawn creation or state mutation.
    public class QuestNode_PrepareScholarTranslation : QuestNode
    {
        private static Thing_AncientEarthArchiveFragment FindBook(Map map)
        {
            return map?.listerThings.AllThings.OfType<Thing_AncientEarthArchiveFragment>()
                .FirstOrDefault(a => a.Identified && a.EarthText != null && !a.SoldByPlayer);
        }

        protected override bool TestRunInt(Slate slate)
        {
            // Vanilla QuestUnique checks ongoing quests only. An unaccepted
            // invitation also reserves this opportunity, including older saves.
            bool invitationExists = Find.QuestManager.QuestsListForReading.Any(q =>
                q.root != null && q.root.defName == "LiAIChat_ScholarTranslation" &&
                (q.State == QuestState.NotYetAccepted || q.State == QuestState.Ongoing));
            return !invitationExists && FindBook(slate.Get<Map>("map")) != null;
        }

        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            Map map = slate.Get<Map>("map");
            Thing_AncientEarthArchiveFragment book = FindBook(map);
            if (book == null)
                throw new System.InvalidOperationException("Scholar translation requires an identified EarthText on the selected map.");
            Pawn scholar = PawnGenerator.GeneratePawn(PawnKindDefOf.SpaceRefugee, null);
            scholar.relations.everSeenByPlayer = true;
            ArchiveScholarKnowledgeInitializer.InitializeFromArchive(scholar, book);
            ArchiveScholarProfileInitializer.Initialize(scholar, book);
            var scholarState = PawnAIStateManager.GetState(scholar);
            if (scholarState != null)
                scholarState.AllowsPlayerConversation = true;
            slate.Set("translationScholar", scholar);
            slate.Set("translationTitle", book.EarthText.title ?? book.EarthText.defName);
            QuestGen.quest.AddPart(new QuestPart_ScholarTranslation
            {
                map = map,
                scholar = scholar,
                requiredText = book.EarthText,
                rewardText = EarthTextSelector.SelectMissingOrRandom(),
                inSignalEnable = slate.Get<string>("inSignal")
            });
        }
    }

    public class QuestPart_ScholarTranslation : QuestPartActivable
    {
        public Map map;
        public Pawn scholar;
        public EarthTextDef requiredText;
        public EarthTextDef rewardText;
        public float progress;
        public const float RequiredWork = 6000f;
        private bool arrived;
        private bool rewarded;
        private Pawn finishingResearcher;
        private Thing_AncientEarthArchiveFragment studiedBook;

        public bool Matches(Thing_AncientEarthArchiveFragment book) => book != null &&
            !book.Destroyed && book.Spawned && book.Map == map && book.Identified &&
            !book.SoldByPlayer && book.EarthText == requiredText;

        public bool CanParticipate(Pawn researcher, Thing_AncientEarthArchiveFragment book)
        {
            return quest.State == QuestState.Ongoing && Matches(book) &&
                researcher != null && researcher != scholar && researcher.IsColonistPlayerControlled &&
                !researcher.Drafted &&
                !researcher.skills.GetSkill(SkillDefOf.Intellectual).TotallyDisabled &&
                researcher.Map == map && scholar != null && scholar.Spawned &&
                scholar.Map == map && !scholar.Dead && !scholar.Downed && scholar.Awake() &&
                scholar.Faction == Faction.OfPlayer && !scholar.InMentalState && !scholar.Drafted;
        }

        public bool CanResearch(Pawn researcher, Thing_AncientEarthArchiveFragment book)
        {
            return CanParticipate(researcher, book) &&
                scholar.Position.DistanceToSquared(book.Position) <= 36 &&
                GenSight.LineOfSight(scholar.Position, book.Position, map);
        }

        public override string DescriptionPart =>
            "LiAIChat_TranslationProgress".Translate(requiredText?.title ?? "?",
                (progress / RequiredWork).ToStringPercent()) + "\n" +
            "LiAIChat_TranslationReward".Translate(
                (rewardText ?? requiredText)?.title ?? "?");

        public override IEnumerable<Gizmo> ExtraGizmos(ISelectable selected)
        {
            Pawn pawn = selected as Pawn;
            if (quest.State != QuestState.Ongoing || pawn == null || pawn == scholar ||
                !pawn.IsColonistPlayerControlled || pawn.Map != map)
                yield break;

            yield return new Command_Action
            {
                defaultLabel = "LiAIChat_TranslationButton".Translate(),
                defaultDesc = "LiAIChat_TranslationInstructions".Translate(),
                icon = LiAIChat.UI.LiAIChatTextures.StudyArchive,
                action = () =>
                {
                    var options = new List<FloatMenuOption>();
                    foreach (var book in map.listerThings.AllThings
                        .OfType<Thing_AncientEarthArchiveFragment>().Where(Matches))
                    {
                        var target = book;
                        bool valid = CanParticipate(pawn, target) &&
                            pawn.CanReserveAndReach(target, PathEndMode.Touch, Danger.Some) &&
                            pawn.CanReserve(scholar) &&
                            scholar.CanReach(target, PathEndMode.Touch, Danger.Some);
                        options.Add(new FloatMenuOption(target.LabelCap,
                            valid ? (System.Action)(() => pawn.jobs.TryTakeOrderedJob(
                                JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("LiAIChat_JointTranslation"),
                                    target, scholar))) : null));
                    }
                    if (options.Count == 0)
                        Messages.Message("LiAIChat_TranslationNoBook".Translate(), MessageTypeDefOf.RejectInput);
                    else
                        Find.WindowStack.Add(new FloatMenu(options));
                }
            };
        }

        public static QuestPart_ScholarTranslation FindFor(Pawn scholar)
        {
            return Find.QuestManager.QuestsListForReading
                .Where(q => q.State == QuestState.Ongoing)
                .SelectMany(q => q.PartsListForReading).OfType<QuestPart_ScholarTranslation>()
                .FirstOrDefault(p => p.scholar == scholar);
        }

        public void AddWork(Pawn researcher, Thing_AncientEarthArchiveFragment book)
        {
            if (!CanResearch(researcher, book) || rewarded)
                return;
            // Once complete, restarting the job must not reassign the learning credit.
            if (progress >= RequiredWork)
                return;
            finishingResearcher = researcher;
            studiedBook = book;
            float work =
                (1f + researcher.skills.GetSkill(SkillDefOf.Intellectual).Level * 0.05f) *
                CivilizationKnowledgeEffectUtility.GetScholarTranslationSpeedFactor();
            progress = System.Math.Min(RequiredWork, progress + work);
            researcher.skills.Learn(SkillDefOf.Intellectual, 0.03f);
        }

        public override void QuestPartTick()
        {
            base.QuestPartTick();
            if (quest.State != QuestState.Ongoing || Find.TickManager.TicksGame % 60 != 0)
                return;
            if (scholar == null || scholar.Dead || map == null || requiredText == null)
            {
                quest.End(QuestEndOutcome.Fail, true);
                return;
            }
            if (scholar.Spawned && scholar.Map == map)
                arrived = true;
            if (arrived && (!scholar.Spawned || scholar.Map != map || scholar.Faction != Faction.OfPlayer))
            {
                quest.End(QuestEndOutcome.Fail, true);
                return;
            }
            if (progress < RequiredWork || rewarded)
                return;

            var reward = AncientArchiveFactory.Create(ArchiveSourceType.QuestReward,
                "Scholar's translation research reward", rewardText ?? requiredText);
            if (reward == null)
                return;
            if (!GenPlace.TryPlaceThing(reward, DropCellFinder.TradeDropSpot(map), map, ThingPlaceMode.Near))
            {
                reward.Destroy();
                return;
            }
            rewarded = true;
            // Reuse ordinary study's per-pawn StudyIdentityId deduplication.
            // Quest rewards remain independent of whether this text was studied before.
            if (finishingResearcher != null && !finishingResearcher.Dead &&
                finishingResearcher.Faction == Faction.OfPlayer && studiedBook != null)
            {
                AncientArchiveStudyUtility.CompleteStudy(finishingResearcher, studiedBook, false);
            }
            Find.LetterStack.ReceiveLetter("LiAIChat_TranslationNotesTitle".Translate(),
                "LiAIChat_TranslationNotes".Translate(requiredText.title), LetterDefOf.PositiveEvent, reward);
            quest.End(QuestEndOutcome.Success, true);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref map, "map");
            Scribe_References.Look(ref scholar, "scholar");
            Scribe_Defs.Look(ref requiredText, "requiredText");
            // Legacy quests use their requested text rather than rerolling on load.
            Scribe_Defs.Look(ref rewardText, "rewardText");
            Scribe_Values.Look(ref progress, "progress");
            Scribe_Values.Look(ref arrived, "arrived");
            Scribe_Values.Look(ref rewarded, "rewarded");
            Scribe_References.Look(ref finishingResearcher, "finishingResearcher");
            Scribe_References.Look(ref studiedBook, "studiedBook");
        }
    }

    public class JobDriver_JointTranslation : JobDriver
    {
        private Thing_AncientEarthArchiveFragment Book => TargetA.Thing as Thing_AncientEarthArchiveFragment;
        private Pawn Scholar => TargetB.Thing as Pawn;
        private QuestPart_ScholarTranslation Part => QuestPart_ScholarTranslation.FindFor(Scholar);

        private bool ScholarIsAssisting => Scholar?.CurJob?.def.defName == "LiAIChat_AssistTranslation" &&
            Scholar.CurJob.targetB.Thing == pawn && Scholar.CurJob.targetA.Thing == Book;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(Book, job, 1, -1, null, errorOnFailed) &&
                pawn.Reserve(Scholar, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOnDespawnedOrNull(TargetIndex.B);
            this.FailOn(() => Part == null || !Part.CanParticipate(pawn, Book));
            AddFinishAction(condition =>
            {
                // Only release the job started for this researcher, never another order.
                if (ScholarIsAssisting)
                    Scholar.jobs.EndCurrentJob(JobCondition.InterruptForced);
            });
            Toil invite = ToilMaker.MakeToil("LiAIChatInviteScholar");
            invite.defaultCompleteMode = ToilCompleteMode.Instant;
            invite.initAction = () => Scholar.jobs.TryTakeOrderedJob(JobMaker.MakeJob(
                DefDatabase<JobDef>.GetNamed("LiAIChat_AssistTranslation"), Book, pawn));
            yield return invite;
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            Toil wait = ToilMaker.MakeToil("LiAIChatWaitForScholar");
            wait.defaultCompleteMode = ToilCompleteMode.Delay;
            wait.defaultDuration = 2500;
            wait.FailOn(() => !ScholarIsAssisting);
            wait.tickAction = () =>
            {
                if (Part != null && Part.CanResearch(pawn, Book) &&
                    Scholar.Position.AdjacentTo8WayOrInside(Book.Position))
                    ReadyForNextToil();
            };
            yield return wait;
            Toil study = ToilMaker.MakeToil("LiAIChatJointTranslation");
            study.FailOn(() => !ScholarIsAssisting || Part == null || !Part.CanResearch(pawn, Book));
            study.defaultCompleteMode = ToilCompleteMode.Never;
            study.tickAction = () =>
            {
                var part = Part;
                if (part == null)
                    return;
                pawn.rotationTracker.FaceTarget(Scholar);
                part.AddWork(pawn, Book);
                if (part.progress >= QuestPart_ScholarTranslation.RequiredWork)
                    ReadyForNextToil();
            };
            study.WithProgressBar(TargetIndex.A,
                () => (Part?.progress ?? 0f) / QuestPart_ScholarTranslation.RequiredWork);
            yield return study;
        }
    }

    // The scholar has a real paired job, so ordinary wandering cannot interrupt attendance.
    public class JobDriver_AssistTranslation : JobDriver
    {
        private Pawn Researcher => TargetB.Thing as Pawn;
        public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOnDespawnedOrNull(TargetIndex.B);
            this.FailOn(() => Researcher?.CurJob?.def.defName != "LiAIChat_JointTranslation" ||
                Researcher.CurJob.targetB.Thing != pawn ||
                Researcher.CurJob.targetA.Thing != TargetA.Thing);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            Toil assist = ToilMaker.MakeToil("LiAIChatAssistTranslation");
            assist.defaultCompleteMode = ToilCompleteMode.Never;
            assist.tickAction = () => pawn.rotationTracker.FaceTarget(Researcher);
            yield return assist;
        }
    }
}
