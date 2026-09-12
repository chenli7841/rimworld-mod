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
                ArchiveContentDef content =
                    archive.Content;

                if (content == null)
                {
                    continue;
                }

                bool alreadyStudied =
                    state
                        .StudiedArchiveContentIds
                        .Contains(
                            content.defName);

                string label =
                    content.title;

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

        public static void CompleteStudy(
    Pawn pawn,
    Thing archiveThing)
        {
            if (pawn == null ||
                archiveThing == null)
            {
                return;
            }

            Thing_AncientEarthArchiveFragment archive =
                archiveThing as
                    Thing_AncientEarthArchiveFragment;

            if (archive == null)
            {
                return;
            }

            ArchiveContentDef content =
                archive.Content;

            if (content == null)
            {
                return;
            }

            // ---------------------------------------------------------
            // Validate topic
            // ---------------------------------------------------------

            if (!KnowledgeTopicCatalog.Contains(
                content.topicId))
            {
                Log.Error(
                    "[Li AI Chat] ArchiveContentDef " +
                    content.defName +
                    " references unknown topic: " +
                    content.topicId);

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

            string contentId =
                content.defName;

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
                        content.topicId,

                    LearningStrength =
                        content.familiarityGain
                };

            KnowledgeUpdater.Apply(
                state.Knowledge,
                acquisition);

            // ---------------------------------------------------------
            // Learn parent topic
            // ---------------------------------------------------------

            if (!string.IsNullOrWhiteSpace(
                content.parentTopicId))
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

            // ---------------------------------------------------------
            // Long-term memory
            // ---------------------------------------------------------

            string memoryText =
                "Studied the Ancient Earth archive \"" +
                content.title +
                "\" about " +
                content.theme +
                ". Topic learned: " +
                content.topicId +
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
                content.title +
                ".",
                MessageTypeDefOf.PositiveEvent);

            Log.Message(
                "[Li AI Chat] " +
                pawn.LabelShort +
                " learned " +
                content.topicId +
                " +" +
                content.familiarityGain);

            // ---------------------------------------------------------
            // AI reflection
            // ---------------------------------------------------------

            ArchiveReflectionService.GenerateReflection(
                pawn,
                archive);
        }
    }
}