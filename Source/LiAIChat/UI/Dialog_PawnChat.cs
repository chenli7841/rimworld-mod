using LiAIChat.AI;
using LiAIChat.Archive;
using LiAIChat.Goals;
using LiAIChat.Knowledge;
using LiAIChat.Memory;
using LiAIChat.Models;
using LiAIChat.Pawns;
using LiAIChat.Refugees;
using LiAIChat.State;
using LiAIChat.Worldview;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace LiAIChat.UI
{
    public class Dialog_PawnChat : Window
    {
        private readonly Pawn pawn;

        private string inputText = "";

        private readonly PawnAIState pawnState;

        private readonly PawnConversation conversation;

        private Vector2 scrollPosition =
            Vector2.zero;

        private bool scrollToBottom =
            false;

        private readonly IChatProvider chatProvider;

        private bool isWaitingForAI = false;

        public override Vector2 InitialSize =>
            new Vector2(700f, 500f);

        private readonly PawnContext pawnContext;

        private readonly IConversationSummarizer conversationSummarizer;

        private const int SummaryBatchSize = 10;

        private readonly IMemoryExtractor memoryExtractor;

        private readonly IWorldviewDirector worldviewDirector;

        private readonly IKnowledgeDirector knowledgeDirector;

        private readonly ILifeGoalDirector lifeGoalDirector;

        private const int LifeGoalCheckInterval = 10;

        private readonly IArchiveScholarConversationDirector
    archiveScholarConversationDirector;

        private bool scholarMeaningfulConversationRecordedThisSession = false;

        private bool refugeeMeaningfulConversationRecordedThisSession = false;
        public Dialog_PawnChat(Pawn pawn)
        {
            this.pawn = pawn;

            pawnContext = PawnContextBuilder.Build(pawn);

            pawnState = PawnAIStateManager.GetState(pawn);

            conversation = pawnState.Conversation;

            if (conversation == null)
            {
                Log.Error(
                    "[Li AI Chat] Could not load conversation for " +
                    pawn.LabelShort);
            }

            if (pawnState.HasPendingProactiveDialogue && !string.IsNullOrWhiteSpace(pawnState.PendingProactiveDialogue))
            {
                conversation.Messages.Add(
                    new ChatMessage(
                        false,
                        pawnState.PendingProactiveDialogue));

                pawnState.HasPendingProactiveDialogue =
                    false;

                pawnState.PendingProactiveDialogue =
                    "";
            }

            chatProvider = new OpenAIChatProvider(Config.Config.OpenAI_API_KEY);

            doCloseX = true;
            closeOnClickedOutside = false;
            absorbInputAroundWindow = true;

            conversationSummarizer = new OpenAIConversationSummarizer(Config.Config.OpenAI_API_KEY);
            memoryExtractor = new OpenAIMemoryExtractor(Config.Config.OpenAI_API_KEY);
            worldviewDirector = new OpenAIWorldviewDirector(Config.Config.OpenAI_API_KEY);
            knowledgeDirector = new OpenAIKnowledgeDirector(Config.Config.OpenAI_API_KEY);
            lifeGoalDirector = new OpenAILifeGoalDirector(Config.Config.OpenAI_API_KEY);
            archiveScholarConversationDirector = new OpenAIArchiveScholarConversationDirector(Config.Config.OpenAI_API_KEY);
        }

        public override void DoWindowContents(Rect inRect)
        {
            float y = 0f;

            // =================================================
            // Title
            // =================================================

            Text.Font = GameFont.Medium;

            Widgets.Label(
                new Rect(
                    0f,
                    y,
                    inRect.width,
                    35f
                ),
                $"Talk with {pawn.LabelShort}"
            );

            Text.Font = GameFont.Small;

            y += 50f;

            // =================================================
            // Conversation
            // =================================================

            Rect conversationOuterRect =
                new Rect(
                    0f,
                    y,
                    inRect.width,
                    220f
                );

            Widgets.DrawMenuSection(
                conversationOuterRect
            );

            string conversationText =
                BuildConversationText();

            if (isWaitingForAI)
            {
                conversationText +=
                    $"\n{pawn.LabelShort} is thinking...";
            }

            float contentWidth =
                conversationOuterRect.width - 30f;

            float contentHeight =
                Text.CalcHeight(
                    conversationText,
                    contentWidth
                );

            contentHeight =
                Mathf.Max(
                    contentHeight + 20f,
                    conversationOuterRect.height
                );

            if (scrollToBottom)
            {
                scrollPosition.y =
                    contentHeight;

                scrollToBottom =
                    false;
            }

            Rect viewRect =
                new Rect(
                    0f,
                    0f,
                    contentWidth,
                    contentHeight
                );

            Widgets.BeginScrollView(
                conversationOuterRect.ContractedBy(8f),
                ref scrollPosition,
                viewRect
            );

            Widgets.Label(
                new Rect(
                    0f,
                    0f,
                    contentWidth,
                    contentHeight
                ),
                conversationText
            );

            Widgets.EndScrollView();

            y += 240f;

            // =================================================
            // Input label
            // =================================================

            Widgets.Label(
                new Rect(
                    0f,
                    y,
                    inRect.width,
                    25f
                ),
                "Your message:"
            );

            y += 30f;

            // =================================================
            // Input
            // =================================================

            Rect inputRect =
                new Rect(
                    0f,
                    y,
                    inRect.width - 110f,
                    80f
                );

            inputText =
                Widgets.TextArea(
                    inputRect,
                    inputText
                );

            // =================================================
            // Send
            // =================================================

            Rect sendButtonRect =
                new Rect(
                    inRect.width - 100f,
                    y,
                    100f,
                    40f
                );

            if (isWaitingForAI)
            {
                GUI.color = Color.gray;
            }

            bool sendClicked =
                Widgets.ButtonText(
                    sendButtonRect,
                    isWaitingForAI
                        ? "Thinking..."
                        : "Send"
                );

            GUI.color = Color.white;

            if (sendClicked && !isWaitingForAI)
            {
                SendMessage();
            }

        }

        private async void SendMessage()
        {
            if (isWaitingForAI)
                return;

            string playerMessage =
                inputText?.Trim();

            if (string.IsNullOrEmpty(playerMessage))
                return;

            inputText = "";

            isWaitingForAI = true;

            try
            {
                List<PawnMemory> relevantMemories = MemoryRetriever.GetRelevantMemories(
                    pawnState.Memories,
                    playerMessage);
                foreach (PawnMemory memory in relevantMemories)
                {
                    Log.Message(
                        "[Li AI Chat] Retrieved memory for " +
                        pawn.LabelShort +
                        ": " +
                        memory.Text);
                }
                string aiResponse = await chatProvider.SendAsync(
                    pawnContext,
                    PawnAIStateManager.GetState(pawn),
                    pawnState.Worldview,
                    pawnState.Meaning,
                    pawnState.Knowledge,
                    pawnState.LifeGoal,
                    pawnState.LifeEvents,
                    pawnState.IntellectualExchanges,
                    conversation.Summary,
                    relevantMemories,
                    conversation.Messages,
                    playerMessage);

                conversation.Messages.Add(
    new ChatMessage(
        true,
        playerMessage));

                conversation.Messages.Add(
                    new ChatMessage(
                        false,
                        aiResponse));

                await UpdateKnowledge();

                await UpdateWorldview();

                await UpdateArchiveScholarConversationImpact(
                    playerMessage,
                    aiResponse);

                await UpdateSummaryIfNeeded();

                // 不要每轮对话都调用 Life Goal Director。Life Goal 不应该像 Worldview 一样每次 Talk 都分析，完全浪费。
                // 此处先用一个非常简单的触发条件。例如每完成 10 条 messages 才检查一次。
                if (ShouldCheckLifeGoal())
                {
                    pawnState.LastLifeGoalCheckMessageCount = conversation.Messages.Count;
                    await UpdateLifeGoal();
                }

                if (pawnState != null && pawnState.IsRefugeeGroupLeader)
                {
                    ArchiveRefugeeConversationImpact impact =
                        await ArchiveRefugeeConversationDirector
                            .AnalyzeAsync(
                                pawnState,
                                playerMessage,
                                aiResponse);

                    ArchiveRefugeeConversationImpactApplier
                        .Apply(
                            pawnState,
                            impact,
                            ref refugeeMeaningfulConversationRecordedThisSession);
                }

            }
            catch (System.Exception ex)
            {
                conversation.Messages.Add(
                    new ChatMessage(
                        true,
                        playerMessage));

                conversation.Messages.Add(
                    new ChatMessage(
                        false,
                        "AI request failed: " +
                        ex.Message));

                Log.Error(
                    "[Li AI Chat] AI request failed: " +
                    ex);
            }
            finally
            {
                isWaitingForAI = false;
                scrollToBottom = true;
            }
        }

        private string BuildConversationText()
        {
            if (conversation.Messages.Count == 0)
            {
                return
                    $"{pawn.LabelShort} is waiting for you to speak.";
            }

            StringBuilder text =
                new StringBuilder();

            foreach (ChatMessage message in conversation.Messages)
            {
                if (message.IsPlayer)
                {
                    text.AppendLine("You:");
                }
                else
                {
                    text.AppendLine(
                        $"{pawn.LabelShort}:"
                    );
                }

                text.AppendLine(
                    message.Text
                );

                text.AppendLine();
            }

            return text.ToString();
        }

        private async Task UpdateWorldview()
        {
            try
            {
                WorldviewChange change =
                    await worldviewDirector.AnalyzeAsync(
                        pawnContext,
                        pawnState.Worldview,
                        conversation.Messages);

                AppliedWorldviewChange applied = WorldviewUpdater.Apply(
                    pawnState.Worldview,
                    change);

                Log.Message(
                    "[Li AI Chat] Worldview Director for " +
                    pawn.LabelShort +
                    ": " +
                    change.Reason);

                Log.Message(
                    "[Li AI Chat] Trust proposed=" +
                    change.TrustInChristianityDelta.ToString("0.000") +
                    ", applied=" +
                    applied.TrustInChristianityDelta.ToString("0.000"));

                Log.Message(
                    "[Li AI Chat] Knowledge proposed=" +
                    change.KnowledgeOfChristianityDelta.ToString("0.000") +
                    ", applied=" +
                    applied.KnowledgeOfChristianityDelta.ToString("0.000"));

                Log.Message(
                    "[Li AI Chat] IntellectualResistance proposed=" +
                    change.IntellectualResistanceDelta.ToString("0.000") +
                    ", applied=" +
                    applied.IntellectualResistanceDelta.ToString("0.000"));


            }
            catch (Exception ex)
            {
                Log.Warning(
                    "[Li AI Chat] Worldview Director failed: " +
                    ex);
            }
        }

        private async Task UpdateSummaryIfNeeded()
        {
            int unsummarizedCount =
                conversation.Messages.Count -
                conversation.SummarizedMessageCount;

            if (unsummarizedCount <
                SummaryBatchSize)
            {
                return;
            }

            int countToSummarize =
                unsummarizedCount;

            List<ChatMessage> messagesToSummarize =
                conversation.Messages.GetRange(
                    conversation.SummarizedMessageCount,
                    countToSummarize);

            try
            {
                string newSummary =
                    await conversationSummarizer
                        .SummarizeAsync(
                            pawnContext,
                            conversation.Summary,
                            messagesToSummarize);

                await ExtractMemories(messagesToSummarize);

                conversation.Summary =
                    newSummary;

                conversation.SummarizedMessageCount =
                    conversation.Messages.Count;

                Log.Message(
                    "[Li AI Chat] Updated conversation summary " +
                    "for " +
                    pawn.LabelShort +
                    ".");

                Log.Message(
                    "[Li AI Chat] Summary: " +
                    conversation.Summary);
            }
            catch (System.Exception ex)
            {
                Log.Error(
                    "[Li AI Chat] Summary update failed: " +
                    ex);
            }
        }

        private async Task UpdateKnowledge()
        {
            try
            {
                List<KnowledgeAcquisition> acquisitions =
                    await knowledgeDirector.AnalyzeAsync(
                        pawnContext,
                        pawnState.Knowledge,
                        conversation.Messages);

                foreach (KnowledgeAcquisition acquisition
                         in acquisitions)
                {
                    float applied =
                        KnowledgeUpdater.Apply(
                            pawnState.Knowledge,
                            acquisition);

                    if (applied <= 0f)
                        continue;

                    Log.Message(
                        "[Li AI Chat] " +
                        pawn.LabelShort +
                        " learned " +
                        KnowledgeTopicCatalog.GetDisplayName(
                            acquisition.TopicId) +
                        ": proposed=" +
                        acquisition.LearningStrength.ToString("0.000") +
                        ", applied=" +
                        applied.ToString("0.000") +
                        ", familiarity=" +
                        pawnState.Knowledge
                            .GetFamiliarity(
                                acquisition.TopicId)
                            .ToString("0.000") +
                        ". Reason: " +
                        acquisition.Reason);
                }
            }
            catch (Exception ex)
            {
                Log.Warning(
                    "[Li AI Chat] Knowledge Director failed: " +
                    ex);
            }
        }

        private bool ShouldCheckLifeGoal()
        {
            if (pawnState.LifeGoal != null)
                return false;

            int currentCount =
                conversation.Messages.Count;

            int messagesSinceLastCheck =
                currentCount -
                pawnState.LastLifeGoalCheckMessageCount;

            return messagesSinceLastCheck >=
                   LifeGoalCheckInterval;
        }
        private async Task UpdateLifeGoal()
        {
            if (pawnState.LifeGoal != null)
                return;

            try
            {
                LifeGoalProposal proposal =
                    await lifeGoalDirector.AnalyzeAsync(
                        pawnContext,
                        pawnState);

                bool created =
                    LifeGoalUpdater.TryCreateGoal(
                        pawnState,
                        proposal);

                if (!created)
                    return;

                Log.Message(
                    "[Li AI Chat] " +
                    pawn.LabelShort +
                    " formed a life goal: " +
                    pawnState.LifeGoal.Title +
                    " | Commitment=" +
                    pawnState.LifeGoal.Commitment
                        .ToString("0.00") +
                    " | Reason=" +
                    pawnState.LifeGoal.Reason);
            }
            catch (Exception ex)
            {
                Log.Warning(
                    "[Li AI Chat] Life Goal Director failed: " +
                    ex);
            }
        }

        private async Task ExtractMemories(List<ChatMessage> messages)
        {
            try
            {
                List<PawnMemory> newMemories =
                    await memoryExtractor
                        .ExtractAsync(
                            pawnContext,
                            messages);

                foreach (PawnMemory memory
                         in newMemories)
                {
                    if (memory.Importance < 0.65f)
                        continue;

                    bool alreadyExists = false;

                    foreach (PawnMemory existing
                             in pawnState.Memories)
                    {
                        if (string.Equals(
                            existing.Text,
                            memory.Text,
                            System.StringComparison
                                .OrdinalIgnoreCase))
                        {
                            alreadyExists = true;
                            break;
                        }
                    }

                    if (!alreadyExists)
                    {
                        pawnState.Memories.Add(
                            memory);

                        Log.Message(
                            "[Li AI Chat] New memory for " +
                            pawn.LabelShort +
                            ": " +
                            memory.Text +
                            " (" +
                            memory.Importance
                                .ToString("0.00") +
                            ")");
                    }
                }
            }
            catch (System.Exception ex)
            {
                Log.Error(
                    "[Li AI Chat] Memory extraction failed: " +
                    ex);
            }
        }

        private async Task UpdateArchiveScholarConversationImpact(
                string playerMessage,
                string scholarResponse)
        {
            if (archiveScholarConversationDirector == null)
            {
                return;
            }

            if (pawnState == null ||
                pawnState.ScholarProfile == null ||
                pawnState.ScholarStay == null)
            {
                return;
            }

            if (!pawnState.ScholarStay.Active)
            {
                return;
            }

            try
            {
                ArchiveScholarConversationImpact impact =
                    await archiveScholarConversationDirector
                        .AnalyzeAsync(
                            pawnContext,
                            pawnState,
                            playerMessage,
                            scholarResponse);

                if (impact == null)
                {
                    return;
                }


                // =====================================================
                // C# is authoritative.
                // Never trust the model's numeric range directly.
                // =====================================================

                float hospitalityDelta =
                    Mathf.Clamp(
                        impact.HospitalityDelta,
                        -0.20f,
                        0.20f);


                ArchiveScholarGiftDecisionUtility
                    .ApplyHospitalityChange(
                        pawnState,
                        hospitalityDelta);


                bool meaningfulRecordedNow = false;

                if (impact.MeaningfulConversation &&
                    !scholarMeaningfulConversationRecordedThisSession)
                {
                    ArchiveScholarGiftDecisionUtility
                        .RecordMeaningfulConversation(
                            pawnState);

                    scholarMeaningfulConversationRecordedThisSession =
                        true;

                    meaningfulRecordedNow = true;
                }

                Log.Message(
                    "[Li AI Chat] Scholar conversation impact for " +
                    pawn.LabelShort +
                    ": Meaningful=" +
                    impact.MeaningfulConversation +
                    ", RecordedThisTurn=" +
                    meaningfulRecordedNow +
                    ", HospitalityDelta=" +
                    hospitalityDelta.ToString("0.000") +
                    ", HospitalityTotal=" +
                    pawnState.ScholarStay
                        .HospitalityImpression
                        .ToString("0.000") +
                    ", MeaningfulCount=" +
                    pawnState.ScholarStay
                        .MeaningfulConversationCount +
                    ", Reason=" +
                    impact.Reason);


                Log.Message(
                    "[Li AI Chat] Scholar conversation impact for " +
                    pawn.LabelShort +
                    ": Meaningful=" +
                    impact.MeaningfulConversation +
                    ", HospitalityDelta=" +
                    hospitalityDelta.ToString("0.000") +
                    ", HospitalityTotal=" +
                    pawnState.ScholarStay
                        .HospitalityImpression
                        .ToString("0.000") +
                    ", MeaningfulCount=" +
                    pawnState.ScholarStay
                        .MeaningfulConversationCount +
                    ", Reason=" +
                    impact.Reason);
            }
            catch (Exception ex)
            {
                Log.Warning(
                    "[Li AI Chat] Scholar conversation impact failed: " +
                    ex);
            }
        }
    }
}