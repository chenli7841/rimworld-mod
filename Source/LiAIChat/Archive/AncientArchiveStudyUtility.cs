using LiAIChat.Knowledge;
using LiAIChat.Models;
using LiAIChat.State;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace LiAIChat.Archive
{
    public static class AncientArchiveStudyUtility
    {
        private const float FamiliarityGain =
            0.20f;

        public static void TryStudyArchive(
    Pawn pawn)
        {
            if (pawn == null ||
                pawn.Map == null)
            {
                return;
            }

            PawnAIState state =
                PawnAIStateManager.GetState(
                    pawn);

            if (state == null)
            {
                return;
            }

            List<Thing_AncientEarthArchiveFragment> archives =
                pawn.Map.listerThings
                    .ThingsOfDef(
                        ThingDefOfArchive
                            .LiAIChat_AncientEarthArchiveFragment)
                    .OfType<Thing_AncientEarthArchiveFragment>()
                    .Where(
                        archive =>
                            archive.Content != null &&
                            archive.Identified &&
                            pawn.CanReach(
                                archive,
                                PathEndMode.Touch,
                                Danger.Some))
                    .OrderBy(
                        archive =>
                            pawn.Position
                                .DistanceToSquared(
                                    archive.Position))
                    .ToList();

            if (archives.Count == 0)
            {
                Messages.Message(
                    "No reachable ancient Earth archive fragment is available.",
                    MessageTypeDefOf.RejectInput);

                return;
            }

            ShowArchiveSelectionMenu(
                pawn,
                state,
                archives);
        }

        /// <summary>
        /// 让玩家选择 Pawn 要读哪一份 Ancient Earth Archive。
        /// </summary>
        private static void ShowArchiveSelectionMenu(
    Pawn pawn,
    PawnAIState state,
    List<Thing_AncientEarthArchiveFragment> archives)
        {
            List<FloatMenuOption> options =
                new List<FloatMenuOption>();

            foreach (
                Thing_AncientEarthArchiveFragment archive
                in archives)
            {
                string studyId =
    archive.StudyIdentityId;

                if (string.IsNullOrWhiteSpace(
                    studyId))
                {
                    continue;
                }

                bool alreadyStudied =
                    state
                        .StudiedArchiveContentIds
                        .Contains(studyId);

                string label;

                if (archive.EarthText != null)
                {
                    label =
                        archive.EarthText.title;

                    if (!string.IsNullOrWhiteSpace(
                        archive.EarthText.author))
                    {
                        label +=
                            " — " +
                            archive.EarthText.author;
                    }
                }
                else
                {
                    ArchiveContentDef content =
                        archive.Content;

                    if (content == null)
                    {
                        continue;
                    }

                    label =
                        content.title;
                }

                if (alreadyStudied)
                {
                    label +=
                        " (already studied)";
                }

                Thing_AncientEarthArchiveFragment
                    capturedArchive = archive;

                if (alreadyStudied)
                {
                    options.Add(
                        new FloatMenuOption(
                            label,
                            null));
                }
                else
                {
                    options.Add(
                        new FloatMenuOption(
                            label,
                            () =>
                            {
                                StartStudyJob(
                                    pawn,
                                    capturedArchive);
                            }));
                }
            }

            if (options.Count == 0)
            {
                Messages.Message(
                    "No archive content is available.",
                    MessageTypeDefOf.RejectInput);

                return;
            }

            Find.WindowStack.Add(
                new FloatMenu(options));
        }

        private static void StartStudyJob(
    Pawn pawn,
    Thing_AncientEarthArchiveFragment archive)
        {
            if (pawn == null ||
                archive == null ||
                archive.Destroyed ||
                !archive.Spawned)
            {
                return;
            }

            Job job =
                JobMaker.MakeJob(
                    LiAIChatJobDefOf
                        .LiAIChat_StudyAncientArchive,
                    archive);

            pawn.jobs.TryTakeOrderedJob(
                job,
                JobTag.Misc);
        }

        public static void CompleteStudy(Pawn pawn, Thing archiveThing)
        {
            CompleteStudy(pawn, archiveThing, true);
        }

        public static void CompleteStudy(Pawn pawn, Thing archiveThing, bool generateReflection)
        {
            if (pawn == null || archiveThing == null)
            {
                return;
            }

            Thing_AncientEarthArchiveFragment archive = archiveThing as Thing_AncientEarthArchiveFragment;

            if (archive == null)
            {
                return;
            }

            ArchiveContentDef content = archive.Content;

            if (content == null)
            {
                return;
            }

            string topicId = archive.KnowledgeTopicId;

            string studyId = archive.StudyIdentityId;

            if (string.IsNullOrWhiteSpace(topicId) || string.IsNullOrWhiteSpace(studyId))
            {
                return;
            }

            // ---------------------------------------------------------
            // Validate topic
            // ---------------------------------------------------------

            if (!KnowledgeTopicCatalog.Contains(topicId))
            {
                Log.Error("[Li AI Chat] Archive references unknown topic: " + topicId);
                return;
            }

            PawnAIState state =
                PawnAIStateManager.GetState(
                    pawn);

            if (state == null ||
                state.Knowledge == null)
            {
                return;
            }

            string contentId = studyId;

            // ---------------------------------------------------------
            // Prevent studying the same content twice
            // ---------------------------------------------------------

            if (state
                .StudiedArchiveContentIds
                .Contains(contentId))
            {
                Messages.Message(
                    pawn.LabelShort +
                    " has already studied this archive content.",
                    MessageTypeDefOf.NeutralEvent);

                return;
            }

            // ---------------------------------------------------------
            // Learn specific topic
            // ---------------------------------------------------------

            KnowledgeAcquisition acquisition =
                new KnowledgeAcquisition
                {
                    TopicId =
                        topicId,

                    LearningStrength =
                        content.familiarityGain
                };

            KnowledgeUpdater.Apply(
                state.Knowledge,
                acquisition);

            // ---------------------------------------------------------
            // Learn parent topic
            // ---------------------------------------------------------

            if (archive.EarthText == null && !string.IsNullOrWhiteSpace(content.parentTopicId))
            {
                if (KnowledgeTopicCatalog.Contains(
                    content.parentTopicId))
                {
                    KnowledgeAcquisition parentAcquisition =
                        new KnowledgeAcquisition
                        {
                            TopicId =
                                content.parentTopicId,

                            LearningStrength =
                                content.parentFamiliarityGain
                        };

                    KnowledgeUpdater.Apply(
                        state.Knowledge,
                        parentAcquisition);
                }
                else
                {
                    Log.Warning(
                        "[Li AI Chat] ArchiveContentDef " +
                        content.defName +
                        " references unknown parent topic: " +
                        content.parentTopicId);
                }
            }

            // ---------------------------------------------------------
            // Mark content as studied
            // ---------------------------------------------------------

            state
                .StudiedArchiveContentIds
                .Add(contentId);

            state.SetEarthTextFamiliarity(
                contentId,
                1f);

            CivilizationDomainKnowledgeUtility.ReconcileTextContribution(
                state,
                contentId,
                topicId);

            // ---------------------------------------------------------
            // Long-term memory
            // ---------------------------------------------------------

            string studyTitle;

            if (archive.EarthText != null)
            {
                studyTitle =
                    archive.EarthText.title;
            }
            else
            {
                studyTitle =
                    content.title;
            }

            string memoryText =
                "Studied the Ancient Earth text \"" +
                studyTitle +
                "\".";

            if (archive.EarthText != null &&
                !string.IsNullOrWhiteSpace(
                    archive.EarthText.author))
            {
                memoryText +=
                    " Author: " +
                    archive.EarthText.author +
                    ".";
            }

            memoryText +=
                " Topic learned: " +
                topicId +
                ".";

            if (!string.IsNullOrWhiteSpace(
                archive.SourceDescription))
            {
                memoryText +=
                    " Source: " +
                    archive.SourceDescription;
            }

            state.Memories.Add(
                new PawnMemory(
                    memoryText,
                    0.75f));

            // ---------------------------------------------------------
            // Intellectual XP
            // ---------------------------------------------------------

            if (pawn.skills != null)
            {
                float xp =
                    ArchiveStudyCalculator
                        .GetIntellectualXp(
                            archive);

                pawn.skills.Learn(
                    SkillDefOf.Intellectual,
                    xp);
            }

            // ---------------------------------------------------------
            // Player feedback
            // ---------------------------------------------------------

            Messages.Message(
                pawn.LabelShort +
                " studied " +
                studyTitle +
                ".",
                MessageTypeDefOf.PositiveEvent);

            LiAIChat.Events.ColonyEventLog.Record("研读文献", pawn.LabelShort + " 完成研读“" + studyTitle + "”。",
                2, pawn, "archive-study:" + pawn.thingIDNumber + ":" + contentId);

            Log.Message(
                "[Li AI Chat] " +
                pawn.LabelShort +
                " learned " +
                topicId +
                " from \"" +
                studyTitle +
                "\".");

            // ---------------------------------------------------------
            // AI reflection
            // ---------------------------------------------------------

            if (generateReflection)
            {
                ArchiveReflectionService.GenerateReflection(
                    pawn,
                    archive);
            }
        }
    }
}
