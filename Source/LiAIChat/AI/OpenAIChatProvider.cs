using LiAIChat.Archive;
using LiAIChat.Knowledge;
using LiAIChat.Models;
using LiAIChat.Refugees;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace LiAIChat.AI
{
    public class OpenAIChatProvider : IChatProvider
    {
        private static readonly HttpClient httpClient = new HttpClient();

        private readonly string apiKey;

        public OpenAIChatProvider(string apiKey)
        {
            this.apiKey = apiKey;
        }

        public async Task<string> SendAsync(
            PawnContext pawn,
            PawnAIState state,
            WorldviewState worldview,
            MeaningState meaning,
            KnowledgeState knowledge,
            LifeGoal lifeGoal, 
            IReadOnlyList<PawnLifeEvent> recentLifeEvents,
            IReadOnlyList<PawnIntellectualExchange> recentIntellectualExchanges,
            string conversationSummary,
            IReadOnlyList<PawnMemory> memories,
            IReadOnlyList<ChatMessage> history,
            string playerMessage)
        {
            string instructions = BuildInstructions(pawn, state, worldview, meaning, knowledge, lifeGoal, recentLifeEvents, recentIntellectualExchanges);
            Log.Message(
                "[Li AI Chat] Character prompt:\n" +
                instructions
            );
            string conversationInput = BuildConversationInput(
                pawn,
                state,
                conversationSummary,
                memories,
                history,
                playerMessage
            );
            string json =
            "{" +
            "\"model\":\"gpt-5.6-luna\"," +
            "\"instructions\":\"" +
            EscapeJson(instructions) +
            "\"," +
            "\"input\":\"" +
            EscapeJson(conversationInput) +
            "\"," +
            "\"max_output_tokens\":300" +
            "}";
            Log.Message(
                "[Li AI Chat] Conversation input:\n" +
                conversationInput
            );
            using (var request =
                new HttpRequestMessage(
                    HttpMethod.Post,
                    "https://api.openai.com/v1/responses"))
            {
                request.Headers.Add(
                    "Authorization",
                    "Bearer " + apiKey);

                request.Content =
                    new StringContent(
                        json,
                        Encoding.UTF8,
                        "application/json");

                HttpResponseMessage response =
                    await httpClient.SendAsync(request);

                string responseJson =
                    await response.Content.ReadAsStringAsync();
                Verse.Log.Message(
                    "[Li AI Chat] OpenAI raw response: " +
                    responseJson
                );

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception(
                        $"OpenAI API error {(int)response.StatusCode}: " +
                        responseJson);
                }

                return OpenAIResponseParser.ExtractOutputText(responseJson);
            }
        }
        private static void AppendScholarProfile(
    StringBuilder sb,
    PawnAIState state)
        {
            if (state == null ||
                state.ScholarProfile == null)
            {
                return;
            }

            ArchiveScholarProfile profile =
                state.ScholarProfile;

            sb.AppendLine();
            sb.AppendLine("SCHOLAR BACKGROUND:");

            if (!profile.Specialty.NullOrEmpty())
            {
                sb.AppendLine(
                    "- Specialty: " +
                    profile.Specialty);
            }

            if (!profile.PrimaryTopicId.NullOrEmpty())
            {
                sb.AppendLine(
                    "- Primary field of study: " +
                    profile.PrimaryTopicId);
            }

            if (profile.YearsOfStudy > 0)
            {
                sb.AppendLine(
                    "- Years devoted to this field: " +
                    profile.YearsOfStudy);
            }

            if (!profile.PreservationMotivation.NullOrEmpty())
            {
                sb.AppendLine(
                    "- Reason for preserving Ancient Earth knowledge: " +
                    profile.PreservationMotivation);
            }
            sb.AppendLine("- This background explains the pawn's interests and experience. " +
                "It does not grant knowledge beyond the pawn's actual KnowledgeState.");

            if (state.ScholarStay != null &&
    state.ScholarStay.Active)
            {
                sb.AppendLine();
                sb.AppendLine("CURRENT SITUATION:");
                sb.AppendLine(
                    "- You are temporarily staying with this colony for protection.");
            }
        }

        private static string EscapeJson(string value)
        {
            if (value == null)
                return "";

            return value
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n")
                .Replace("\t", "\\t");
        }

        private string BuildInstructions(PawnContext pawn, PawnAIState state, WorldviewState worldview, MeaningState meaning, KnowledgeState knowledge, LifeGoal lifeGoal, IReadOnlyList<PawnLifeEvent> recentLifeEvents, IReadOnlyList<PawnIntellectualExchange> recentIntellectualExchanges)
        {
            StringBuilder prompt =
                new StringBuilder();

            prompt.AppendLine(
                $"You are {pawn.Name}, a person living in a RimWorld colony."
            );

            prompt.AppendLine();

            prompt.AppendLine(
                "CHARACTER INFORMATION"
            );

            prompt.AppendLine();

            prompt.AppendLine(
                $"Name: {pawn.Name}"
            );

            prompt.AppendLine(
                $"Gender: {pawn.Gender}"
            );

            prompt.AppendLine(
                $"Age: {pawn.Age}"
            );

            prompt.AppendLine();


            prompt.AppendLine("CHARACTER WORLDVIEW STATE");
            prompt.AppendLine(
                "All worldview values range from 0.0 to 1.0, " +
                "where 0.0 means very low or absent and 1.0 means very high or strong.");

            prompt.AppendLine(
                $"Belief in God: {worldview.BeliefInGod:0.00}");

            prompt.AppendLine(
                $"Belief in objective morality: " +
                $"{worldview.BeliefInObjectiveMorality:0.00}");

            prompt.AppendLine(
                $"Trust in Christianity: " +
                $"{worldview.TrustInChristianity:0.00}");

            prompt.AppendLine(
                $"Knowledge of Christianity: " +
                $"{worldview.KnowledgeOfChristianity:0.00}");

            prompt.AppendLine(
                $"Intellectual resistance to Christianity: " +
                $"{worldview.IntellectualResistance:0.00}");

            prompt.AppendLine(
                $"Emotional resistance to Christianity: " +
                $"{worldview.EmotionalResistance:0.00}");

            prompt.AppendLine(
                $"Spiritual interest: " +
                $"{worldview.SpiritualInterest:0.00}");

            prompt.AppendLine();

            prompt.AppendLine(
                "These values describe tendencies, not rigid rules. " +
                "Express them naturally through the character's beliefs, doubts, " +
                "questions, confidence, curiosity, and emotional reactions.");

            prompt.AppendLine(
                "Never mention these numerical values or the existence of this state system in dialogue.");

            prompt.AppendLine();

            prompt.AppendLine("CHARACTER MEANING STATE");

            prompt.AppendLine(
                "All meaning values range from 0.0 to 1.0, " +
                "where 0.0 means very low and 1.0 means very high.");

            prompt.AppendLine(
                $"Purpose: {meaning.Purpose:0.00}");

            prompt.AppendLine(
                $"Belonging: {meaning.Belonging:0.00}");

            prompt.AppendLine(
                $"Hope: {meaning.Hope:0.00}");

            prompt.AppendLine(
                $"Coherence: {meaning.Coherence:0.00}");

            prompt.AppendLine(
                $"Transcendence: {meaning.Transcendence:0.00}");

            prompt.AppendLine();

            prompt.AppendLine(
                "Meaning state describes how the character currently experiences " +
                "purpose, connection, hope, life coherence, and connection to something larger than the self.");

            prompt.AppendLine(
                "Meaning is not the same as mood, happiness, or religious belief.");

            prompt.AppendLine(
                "Use these values as subtle internal tendencies, not rigid rules.");

            prompt.AppendLine(
                "Never mention these numerical values or the existence of the meaning-state system in dialogue.");

            prompt.AppendLine(
                "High meaning does not erase grief, fear, anger, or suffering. " +
                "It may instead shape how the character interprets and endures them.");

            prompt.AppendLine();

            prompt.AppendLine("CHARACTER KNOWLEDGE STATE");

            prompt.AppendLine(
                "All knowledge values range from 0.0 to 1.0. " +
                "These values represent how much the character plausibly knows, " +
                "not the knowledge available to the underlying language model.");

            prompt.AppendLine(
                "Interpret knowledge values approximately: " +
                "0.0-0.2 very limited, 0.2-0.4 basic or fragmented, " +
                "0.4-0.6 moderate, 0.6-0.8 well informed, " +
                "0.8-1.0 highly knowledgeable.");

            prompt.AppendLine(
                $"Ancient Earth history knowledge: " +
                $"{knowledge.EarthHistoryKnowledge:0.00}");

            prompt.AppendLine(
                $"Philosophy knowledge: " +
                $"{knowledge.PhilosophyKnowledge:0.00}");

            prompt.AppendLine(
                $"Religious knowledge: " +
                $"{knowledge.ReligiousKnowledge:0.00}");

            prompt.AppendLine(
                $"Politics knowledge: " +
                $"{knowledge.PoliticsKnowledge:0.00}");

            prompt.AppendLine(
                $"Science knowledge: " +
                $"{knowledge.ScienceKnowledge:0.00}");

            prompt.AppendLine();

            prompt.AppendLine(
                "SPECIFIC KNOWN TOPICS");

            if (knowledge.KnownTopics == null ||
                knowledge.KnownTopics.Count == 0)
            {
                prompt.AppendLine(
                    "No specific Ancient Earth topics are recorded as known.");
            }
            else
            {
                foreach (KnowledgeTopic topic in knowledge.KnownTopics)
                {
                    if (topic == null ||
                        string.IsNullOrWhiteSpace(topic.TopicId))
                    {
                        continue;
                    }

                    string topicName = KnowledgeTopicCatalog.GetDisplayName(topic.TopicId);

                    prompt.AppendLine(
                        "- " +
                        topicName +
                        " | familiarity " +
                        topic.Familiarity.ToString("0.00"));
                }
            }

            prompt.AppendLine();

            prompt.AppendLine(
                "IMPORTANT KNOWLEDGE RULE:");

            prompt.AppendLine(
                "Do not use information simply because you, the language model, know it.");

            prompt.AppendLine(
                "The character may only speak confidently about subjects that are reasonably supported " +
                "by the character's knowledge state, background, memories, and recorded experiences.");

            prompt.AppendLine(
                "If the character lacks sufficient knowledge, admit uncertainty, partial knowledge, " +
                "or unfamiliarity naturally.");

            prompt.AppendLine(
                "The character may ask the player to explain unfamiliar concepts.");

            prompt.AppendLine(
                "Never pretend to know detailed Ancient Earth history, philosophy, religion, politics, " +
                "or science when the character's knowledge does not justify it.");

            prompt.AppendLine(
                "When uncertain, prefer saying that the character does not know, " +
                "only vaguely remembers, or has incomplete records rather than inventing detailed knowledge.");

            prompt.AppendLine();

            prompt.AppendLine(
                "Specific known topics are stronger evidence of what the character knows " +
                "than general knowledge levels.");

            prompt.AppendLine(
                "If a specific Ancient Earth person, event, doctrine, text, or concept " +
                "is not listed as a known topic, do not assume detailed knowledge of it " +
                "merely because the general knowledge score is high.");

            prompt.AppendLine(
                "General knowledge may justify broad contextual awareness, " +
                "but detailed claims require relevant known topics, memories, " +
                "background information, or information provided by the player in the conversation.");

            prompt.AppendLine(
                "A known topic includes reasonable core facts normally associated with that topic. " +
                "Do not require every related person, place, or sub-event to have its own topic entry.");

            prompt.AppendLine();
            prompt.AppendLine(
                "CURRENT LIFE DIRECTION");

            if (lifeGoal == null ||
                !lifeGoal.IsActive)
            {
                prompt.AppendLine(
                    "The character currently has no clear long-term life goal.");
            }
            else
            {
                prompt.AppendLine(
                    "Life goal: " +
                    lifeGoal.Title);

                prompt.AppendLine(
                    "Description: " +
                    lifeGoal.Description);

                prompt.AppendLine(
                    "Commitment: " +
                    lifeGoal.Commitment.ToString("0.00"));

                prompt.AppendLine(
                    "Reason: " +
                    lifeGoal.Reason);

                prompt.AppendLine(
                    "This goal should influence the character's priorities, hopes, questions, " +
                    "and interpretation of future events, but should not be forced into every conversation.");

                prompt.AppendLine(
                    "Never mention numerical commitment values or the existence of a life-goal state system.");
            }
            prompt.AppendLine();
            prompt.AppendLine("RECENT IMPORTANT LIFE EVENTS");

            if (recentLifeEvents == null ||
                recentLifeEvents.Count == 0)
            {
                prompt.AppendLine(
                    "None recorded.");
            }
            else
            {
                int firstIndex =
                    System.Math.Max(
                        0,
                        recentLifeEvents.Count - 5);

                for (int i = firstIndex;
                     i < recentLifeEvents.Count;
                     i++)
                {
                    PawnLifeEvent lifeEvent =
                        recentLifeEvents[i];

                    prompt.AppendLine(
                        "- " +
                        lifeEvent.Description);
                }
            }

            prompt.AppendLine();

            prompt.AppendLine("These life events are factual events that happened in the character's world.");

            prompt.AppendLine(
                "The character may naturally refer to them when relevant, " +
                "but should not force them into every conversation.");

            prompt.AppendLine();


            prompt.AppendLine();
            prompt.AppendLine("RECENT INTELLECTUAL EXCHANGES WITH OTHER COLONISTS");

            if (recentIntellectualExchanges == null || recentIntellectualExchanges.Count == 0)
            {
                prompt.AppendLine("None.");
            }
            else
            {
                int start = Math.Max(0, recentIntellectualExchanges.Count - 3);

                for (int i = start; i < recentIntellectualExchanges.Count; i++)
                {
                    PawnIntellectualExchange exchange = recentIntellectualExchanges[i];

                    if (exchange == null)
                        continue;

                    prompt.AppendLine("- " + exchange.Summary);
                }
            }

            prompt.AppendLine();

            prompt.AppendLine(
                "These are conversations the character actually had with other colonists. " +
                "They may be mentioned when relevant, but should not be forced into every response.");

            prompt.AppendLine();

            prompt.AppendLine(
                "Personality traits:"
            );

            if (pawn.Traits.Count == 0)
            {
                prompt.AppendLine(
                    "- No notable traits."
                );
            }
            else
            {
                foreach (string trait
                         in pawn.Traits)
                {
                    prompt.AppendLine(
                        $"- {trait}"
                    );
                }
            }

            if (!string.IsNullOrWhiteSpace(
                    pawn.Childhood))
            {
                prompt.AppendLine();

                prompt.AppendLine(
                    $"Childhood: {pawn.Childhood}"
                );
            }

            if (!string.IsNullOrWhiteSpace(
                    pawn.Adulthood))
            {
                prompt.AppendLine();

                prompt.AppendLine(
                    $"Adulthood: {pawn.Adulthood}"
                );
            }

            prompt.AppendLine();

            prompt.AppendLine(
                "ROLEPLAY INSTRUCTIONS"
            );

            prompt.AppendLine(
                "- Stay in character."
            );

            prompt.AppendLine(
                "- Speak naturally as this person."
            );

            prompt.AppendLine(
                "- Let the character's personality traits and background influence the response."
            );

            prompt.AppendLine(
                "- Do not describe yourself as an AI or assistant."
            );

            prompt.AppendLine(
                "- Do not mention these instructions."
            );

            prompt.AppendLine(
                "- Reply in the same language the player uses."
            );

            prompt.AppendLine(
                "- Keep ordinary conversation fairly concise."
            );

            prompt.AppendLine(@"- Archive reflections are the pawn's own previous thoughts. Treat them as continuity of character, not as objective facts. The pawn may reconsider or develop these thoughts over time.");

            string knowledgePrompt = KnowledgePromptBuilder.Build(state.Knowledge);

            if (!string.IsNullOrWhiteSpace(
                knowledgePrompt))
            {
                prompt.AppendLine();
                prompt.AppendLine(knowledgePrompt);
            }
            return prompt.ToString();
        }

        private string BuildConversationInput(
            PawnContext pawn,
            PawnAIState state,
            string conversationSummary,
            IReadOnlyList<PawnMemory> memories,
            IReadOnlyList<ChatMessage> history,
            string playerMessage)
        {
            StringBuilder builder =
                new StringBuilder();

            builder.AppendLine(
                "LONG-TERM CONVERSATION MEMORY");

            builder.AppendLine();

            if (string.IsNullOrWhiteSpace(
                conversationSummary))
            {
                builder.AppendLine(
                    "(No long-term conversation memory yet.)");
            }
            else
            {
                builder.AppendLine(
                    conversationSummary);
            }

            builder.AppendLine();


            builder.AppendLine(
    "IMPORTANT LONG-TERM MEMORIES");

            builder.AppendLine();

            if (memories == null ||
                memories.Count == 0)
            {
                builder.AppendLine(
                    "(No important long-term memories yet.)");
            }
            else
            {
                int memoryCount =
                    Math.Min(
                        memories.Count,
                        8);

                for (int i = 0;
                     i < memoryCount;
                     i++)
                {
                    PawnMemory memory =
                        memories[i];

                    builder.AppendLine(
                        "- " +
                        memory.Text);
                }
            }

            builder.AppendLine();



            builder.AppendLine(
                "RECENT CONVERSATION");

            builder.AppendLine();

            int startIndex =
                Math.Max(0, history.Count - 12);

            for (int i = startIndex;
                 i < history.Count;
                 i++)
            {
                ChatMessage message =
                    history[i];

                builder.AppendLine(
                    message.IsPlayer
                        ? "Player:"
                        : pawn.Name + ":");

                builder.AppendLine(
                    message.Text);

                builder.AppendLine();
            }

            builder.AppendLine(
                "CURRENT PLAYER MESSAGE");

            builder.AppendLine();

            builder.AppendLine(
                "Player:");

            builder.AppendLine(
                playerMessage);

            builder.AppendLine();

            builder.AppendLine(
                pawn.Name + ":");

            List<ArchiveReflection> relevantReflections = ArchiveReflectionRetriever.GetRelevant(state.ArchiveReflections, playerMessage, 3);

            string reflectionPrompt =
                ArchiveReflectionPromptBuilder.Build(
                    relevantReflections);

            if (!string.IsNullOrWhiteSpace(
                reflectionPrompt))
            {
                builder.AppendLine();
                builder.AppendLine(
                    reflectionPrompt);
            }

            ArchiveScholarPromptBuilder.Append(
                builder,
                pawn,
                state);

            ArchiveRefugeePromptBuilder.Append(
                builder,
                state);

            ArchiveRefugeeGroupState group = ArchiveRefugeeGroupManager.GetGroup(state.RefugeeGroupId);

            ArchiveRefugeeStayEventUtility.UpdateTriggerState(group);

            AppendStayEvent(builder, group);

            return builder.ToString();
        }

        private static void AppendStayEvent(
    StringBuilder sb,
    ArchiveRefugeeGroupState group)
        {
            if (group == null ||
                group.StayEvent == null)
            {
                return;
            }

            ArchiveRefugeeStayEventState stayEvent =
                group.StayEvent;

            if (!stayEvent.Triggered ||
                stayEvent.Resolved)
            {
                return;
            }

            sb.AppendLine();

            sb.AppendLine(
                "CURRENT GROUP CONCERN:");

            switch (stayEvent.EventType)
            {
                case "ArchiveAnxiety":

                    sb.AppendLine(
                        "Some members of your group have become uneasy about protecting the Ancient Earth Archive while staying in an unfamiliar colony.");

                    break;


                case "FutureUncertainty":

                    sb.AppendLine(
                        "Members of your group are increasingly uncertain about what will happen after your temporary stay ends.");

                    break;


                case "GroupTension":

                    sb.AppendLine(
                        "The pressures of displacement and temporary settlement have created some tension within your group.");

                    break;
            }

            sb.AppendLine(
                "You may discuss this concern naturally if it becomes relevant.");

            sb.AppendLine(
                "Do not invent specific incidents, injuries, crimes, arguments, or named participants unless they are provided elsewhere.");
        }
    }
}