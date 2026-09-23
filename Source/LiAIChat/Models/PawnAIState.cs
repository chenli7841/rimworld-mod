using LiAIChat.Archive;
using System.Collections.Generic;
using Verse;

namespace LiAIChat.Models
{
    public class PawnAIState : IExposable
    {
        public int PawnId = -1;

        public PawnConversation Conversation =
            new PawnConversation();

        public List<PawnMemory> Memories =
            new List<PawnMemory>();

        public WorldviewState Worldview =
            new WorldviewState();

        public MeaningState Meaning =
            new MeaningState();

        public List<PawnLifeEvent> LifeEvents =
            new List<PawnLifeEvent>();

        public KnowledgeState Knowledge =
            new KnowledgeState();

        public LifeGoal LifeGoal = null;

        public int LastLifeGoalCheckMessageCount = 0;

        /// <summary>
        /// 允许Pawn主动找玩家谈话。不可以实现为每个 tick 都问 AI：Sarah 想不想说话？
        /// </summary>
        public int LastProactiveDialogueTick = 0;

        public bool HasPendingProactiveDialogue = false;

        public string PendingProactiveDialogue = "";

        public List<PawnIntellectualExchange> IntellectualExchanges = new List<PawnIntellectualExchange>();

        public List<string> StudiedArchiveContentIds = new List<string>();
        public Dictionary<string, float> ArchiveReadingProgress = new Dictionary<string, float>();
        public Dictionary<string, float> EarthTextFamiliarity = new Dictionary<string, float>();
        public Dictionary<string, float> EarthTextDomainContribution = new Dictionary<string, float>();
        public List<ArchiveReflection> ArchiveReflections = new List<ArchiveReflection>();

        public ArchiveScholarProfile ScholarProfile;

        public bool AllowsPlayerConversation;

        public ArchiveScholarStayState ScholarStay;

        public string RefugeeGroupId;

        public bool IsRefugeeGroupLeader;
        public bool IsLostAnnotator;
        public bool LostAnnotatorRescued;
        public bool LostAnnotatorPersonaConfigured;

        public LiAIChat.Travel.TravelWish TravelWish;

        public PawnAIState()
        {
        }

        public PawnAIState(int pawnId)
        {
            PawnId = pawnId;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref PawnId, "pawnId", -1);
            Scribe_Deep.Look(ref Conversation, "conversation");
            Scribe_Collections.Look(ref Memories, "memories", LookMode.Deep);
            Scribe_Deep.Look(ref Worldview, "worldview");
            Scribe_Deep.Look(ref Meaning, "meaning");
            Scribe_Collections.Look(ref LifeEvents, "lifeEvents", LookMode.Deep);
            Scribe_Deep.Look(ref Knowledge, "knowledge");
            Scribe_Deep.Look(ref LifeGoal, "lifeGoal");
            Scribe_Values.Look(ref LastLifeGoalCheckMessageCount, "lastLifeGoalCheckMessageCount", 0);
            Scribe_Values.Look(ref LastProactiveDialogueTick, "lastProactiveDialogueTick", 0);
            Scribe_Values.Look(ref HasPendingProactiveDialogue, "hasPendingProactiveDialogue", false);
            Scribe_Values.Look(ref PendingProactiveDialogue, "pendingProactiveDialogue", "");
            Scribe_Collections.Look(ref IntellectualExchanges, "intellectualExchanges", LookMode.Deep);
            Scribe_Collections.Look(ref StudiedArchiveContentIds, "studiedArchiveContentIds", LookMode.Value);
            Scribe_Collections.Look(ref ArchiveReadingProgress, "archiveReadingProgress", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref EarthTextFamiliarity, "earthTextFamiliarity", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref EarthTextDomainContribution, "earthTextDomainContribution", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref ArchiveReflections, "archiveReflections", LookMode.Deep);
            Scribe_Deep.Look(ref ScholarProfile, "scholarProfile");
            Scribe_Values.Look(ref AllowsPlayerConversation, "allowsPlayerConversation", false);
            Scribe_Deep.Look(ref ScholarStay, "scholarStay");
            Scribe_Values.Look(ref RefugeeGroupId, "refugeeGroupId");
            Scribe_Values.Look(ref IsRefugeeGroupLeader, "isRefugeeGroupLeader", false);
            Scribe_Values.Look(ref IsLostAnnotator, "isLostAnnotator", false);
            Scribe_Values.Look(ref LostAnnotatorRescued, "lostAnnotatorRescued", false);
            Scribe_Values.Look(ref LostAnnotatorPersonaConfigured, "lostAnnotatorPersonaConfigured", false);
            Scribe_Deep.Look(ref TravelWish, "travelWish");


            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (Conversation == null)
                {
                    Conversation = new PawnConversation();
                }

                if (Memories == null)
                {
                    Memories = new List<PawnMemory>();
                }

                if (Worldview == null)
                {
                    Worldview = new WorldviewState();
                }

                if (Meaning == null)
                {
                    Meaning = new MeaningState();
                }

                if (LifeEvents == null)
                {
                    LifeEvents = new List<PawnLifeEvent>();
                }

                if (Knowledge == null)
                {
                    Knowledge = new KnowledgeState();
                }

                if (IntellectualExchanges == null)
                {
                    IntellectualExchanges = new List<PawnIntellectualExchange>();
                }

                if (StudiedArchiveContentIds == null)
                {
                    StudiedArchiveContentIds =
                        new List<string>();
                }
                if (ArchiveReadingProgress == null)
                {
                    ArchiveReadingProgress =
                        new Dictionary<string, float>();
                }
                if (EarthTextFamiliarity == null)
                {
                    EarthTextFamiliarity =
                        new Dictionary<string, float>();
                }
                if (EarthTextDomainContribution == null)
                {
                    EarthTextDomainContribution =
                        new Dictionary<string, float>();
                }
                if (ArchiveReflections == null)
                {
                    ArchiveReflections =
                        new List<ArchiveReflection>();
                }

                // 这里不用 PostLoadInit 强制创建LifeGoal。因为：Pawn 没有人生目标是合法状态。
            }
        }

        public float GetArchiveReadingProgress(string studyId)
        {
            if (string.IsNullOrEmpty(studyId))
                return 0f;

            if (ArchiveReadingProgress == null)
                return 0f;

            float progress;

            if (ArchiveReadingProgress.TryGetValue(
                studyId,
                out progress))
            {
                return progress;
            }

            return 0f;
        }

        public void SetArchiveReadingProgress(string studyId, float progress)
        {
            if (string.IsNullOrEmpty(studyId))
                return;

            if (ArchiveReadingProgress == null)
            {
                ArchiveReadingProgress =
                    new Dictionary<string, float>();
            }

            if (progress <= 0f)
            {
                ArchiveReadingProgress.Remove(
                    studyId);

                return;
            }

            ArchiveReadingProgress[studyId] =
                progress;
        }

        public float GetEarthTextFamiliarity(string studyId)
        {
            if (string.IsNullOrEmpty(studyId))
            {
                return 0f;
            }

            float familiarity;

            if (EarthTextFamiliarity != null &&
                EarthTextFamiliarity.TryGetValue(studyId, out familiarity))
            {
                return Clamp01(familiarity);
            }

            return StudiedArchiveContentIds != null &&
                StudiedArchiveContentIds.Contains(studyId)
                    ? 1f
                    : 0f;
        }

        public void LearnEarthText(string studyId, float familiarityGain)
        {
            if (string.IsNullOrEmpty(studyId) ||
                familiarityGain <= 0f)
            {
                return;
            }

            SetEarthTextFamiliarity(
                studyId,
                GetEarthTextFamiliarity(studyId) + familiarityGain);
        }

        public void SetEarthTextFamiliarity(string studyId, float familiarity)
        {
            if (string.IsNullOrEmpty(studyId))
            {
                return;
            }

            if (EarthTextFamiliarity == null)
            {
                EarthTextFamiliarity =
                    new Dictionary<string, float>();
            }

            EarthTextFamiliarity[studyId] =
                Clamp01(familiarity);
        }

        private static float Clamp01(float value)
        {
            if (value < 0f)
            {
                return 0f;
            }

            return value > 1f ? 1f : value;
        }
    }
}
