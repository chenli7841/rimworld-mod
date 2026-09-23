using LiAIChat.Knowledge;
using LiAIChat.Models;
using LiAIChat.State;
using RimWorld;
using Verse;

namespace LiAIChat.Archive
{
    public static class ArchiveScholarInitializer
    {
        public static Thing_AncientEarthArchiveFragment Initialize(Pawn pawn)
        {
            if (pawn == null) return null;

            Thing_AncientEarthArchiveFragment archive = GiveScholarArchive(pawn);
            if (archive == null) return null;

            ArchiveScholarKnowledgeInitializer.InitializeFromArchive(pawn, archive);
            ArchiveScholarProfileInitializer.Initialize(pawn, archive);

            PawnAIState state = PawnAIStateManager.GetState(pawn);
            if (state != null) state.AllowsPlayerConversation = true;

            Log.Message("[Li AI Chat] Archive Scholar initialized: " + pawn.LabelShort);
            return archive;
        }

        /// <summary>Makes the generated SpaceRefugee match the Lost Annotator quest role.</summary>
        public static void ConfigureLostAnnotator(Pawn pawn)
        {
            if (pawn == null) return;

            PawnAIState state = PawnAIStateManager.GetState(pawn);
            if (state == null || state.LostAnnotatorPersonaConfigured) return;

            ApplyScholarBackstories(pawn);
            SetMinimumSkill(pawn, SkillDefOf.Intellectual, 16, Passion.Major);
            SetMinimumSkill(pawn, SkillDefOf.Social, 9, Passion.Minor);
            SetMinimumSkill(pawn, SkillDefOf.Artistic, 8, Passion.Minor);
            SetMinimumSkill(pawn, SkillDefOf.Crafting, 7, Passion.Minor);
            InitializeBroadCivilizationKnowledge(state);

            state.AllowsPlayerConversation = true;
            state.LostAnnotatorPersonaConfigured = true;
            Log.Message("[Li AI Chat] Lost Annotator persona configured: " + pawn.LabelShort);
        }

        public static Thing_AncientEarthArchiveFragment GiveScholarArchive(Pawn pawn)
        {
            if (pawn == null) return null;

            Thing_AncientEarthArchiveFragment archive = AncientArchiveFactory.Create(
                ArchiveSourceType.ScholarCollection, BuildSourceDescription(pawn));
            if (archive == null)
            {
                Log.Warning("[Li AI Chat] Failed to create scholar archive.");
                return null;
            }

            if (pawn.inventory == null)
            {
                Log.Warning("[Li AI Chat] Scholar pawn has no inventory tracker.");
                return null;
            }

            if (!pawn.inventory.innerContainer.TryAdd(archive))
            {
                Log.Warning("[Li AI Chat] Failed to add archive to scholar inventory.");
                archive.Destroy();
                return null;
            }

            Log.Message("[Li AI Chat] Scholar " + pawn.LabelShort +
                " received archive: " + archive.Content?.title);
            return archive;
        }

        private static void ApplyScholarBackstories(Pawn pawn)
        {
            if (pawn.story == null) return;

            BackstoryDef childhood = DefDatabase<BackstoryDef>.GetNamedSilentFail(
                "LiAIChat_ArchiveScholarChildhood");
            BackstoryDef adulthood = DefDatabase<BackstoryDef>.GetNamedSilentFail(
                "LiAIChat_ArchiveScholarAdulthood");
            if (childhood != null) pawn.story.Childhood = childhood;
            if (adulthood != null) pawn.story.Adulthood = adulthood;
        }

        private static void SetMinimumSkill(Pawn pawn, SkillDef skillDef,
            int minimumLevel, Passion minimumPassion)
        {
            if (pawn.skills == null || skillDef == null) return;
            SkillRecord skill = pawn.skills.GetSkill(skillDef);
            if (skill == null) return;
            if (skill.Level < minimumLevel) skill.Level = minimumLevel;
            if (skill.passion < minimumPassion) skill.passion = minimumPassion;
        }

        private static void InitializeBroadCivilizationKnowledge(PawnAIState state)
        {
            KnowledgeState knowledge = state.Knowledge;
            if (knowledge == null) return;

            knowledge.EarthHistoryKnowledge = Max(knowledge.EarthHistoryKnowledge, 0.80f);
            knowledge.PhilosophyKnowledge = Max(knowledge.PhilosophyKnowledge, 0.80f);
            knowledge.ReligiousKnowledge = Max(knowledge.ReligiousKnowledge, 0.75f);
            knowledge.PoliticsKnowledge = Max(knowledge.PoliticsKnowledge, 0.75f);
            knowledge.ScienceKnowledge = Max(knowledge.ScienceKnowledge, 0.75f);

            foreach (string topicId in KnowledgeTopicCatalog.GetAllTopicIds())
                EnsureMinimumTopicKnowledge(knowledge, topicId, 0.70f);

            EnsureMinimumTopicKnowledge(knowledge, "earth.history", 0.80f);
            EnsureMinimumTopicKnowledge(knowledge, "earth.philosophy", 0.80f);
            EnsureMinimumTopicKnowledge(knowledge, "earth.religion", 0.75f);
            EnsureMinimumTopicKnowledge(knowledge, "earth.politics", 0.75f);
            EnsureMinimumTopicKnowledge(knowledge, "earth.science", 0.75f);
        }

        private static void EnsureMinimumTopicKnowledge(KnowledgeState knowledge,
            string topicId, float target)
        {
            float current = knowledge.GetFamiliarity(topicId);
            if (current < target) knowledge.LearnTopic(topicId, target - current);
        }

        private static float Max(float first, float second)
        {
            return first > second ? first : second;
        }

        private static string BuildSourceDescription(Pawn pawn)
        {
            return "Preserved in the private collection of " + pawn.LabelShort +
                ", a wandering scholar.";
        }
    }
}
