using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using LiAIChat.Models;

namespace LiAIChat.AI
{
    public class OpenAIProactiveDialogueDirector
        : IProactiveDialogueDirector
    {
        private readonly string apiKey;

        private static readonly HttpClient httpClient =
            new HttpClient();

        public OpenAIProactiveDialogueDirector(
            string apiKey)
        {
            this.apiKey = apiKey;
        }

        public async Task<string> GenerateAsync(
    PawnContext context,
    PawnAIState state,
    string specialInstruction)
        {
            string instructions =
                BuildInstructions(specialInstruction);

            string input =
                BuildInput(
                    context,
                    state);

            string requestJson =
                BuildRequestJson(
                    instructions,
                    input);

            using (HttpRequestMessage request =
                new HttpRequestMessage(
                    HttpMethod.Post,
                    "https://api.openai.com/v1/responses"))
            {
                request.Headers.Authorization =
                    new AuthenticationHeaderValue(
                        "Bearer",
                        apiKey);

                request.Content =
                    new StringContent(
                        requestJson,
                        Encoding.UTF8,
                        "application/json");

                HttpResponseMessage response =
                    await httpClient.SendAsync(request);

                string responseJson =
                    await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception(
                        "Proactive Dialogue API failed: " +
                        response.StatusCode +
                        "\n" +
                        responseJson);
                }

                return OpenAIResponseParser
                    .ExtractOutputText(
                        responseJson)
                    .Trim();
            }
        }

        /// <summary>
        /// Prompt：只生成一句自然开场
        /// </summary>
        /// <returns></returns>
        private static string BuildInstructions(string specialInstruction)
        {
            StringBuilder prompt =
                new StringBuilder();

            prompt.AppendLine(
                "You generate a short proactive conversation opener " +
                "for a fictional RimWorld character.");

            prompt.AppendLine(
                "The character wants to initiate a conversation with the player.");

            prompt.AppendLine();

            prompt.AppendLine("RULES:");

            prompt.AppendLine(
                "- Write only one or two short sentences.");

            prompt.AppendLine(
                "- Make it sound like the character naturally wants to talk.");

            prompt.AppendLine(
                "- Base it on the character's actual memories, life events, worldview, meaning, knowledge, or life goal.");

            prompt.AppendLine(
                "- Do not invent an event that is not present in the state.");

            prompt.AppendLine(
                "- Do not mention numerical values or hidden systems.");

            prompt.AppendLine(
                "- Do not summarize the whole character.");

            prompt.AppendLine(
                "- Do not make every opener dramatic.");

            prompt.AppendLine(
                "- It is fine to sound uncertain, reflective, practical, curious, hopeful, or troubled.");

            prompt.AppendLine(
                "- Do not include quotation marks.");

            prompt.AppendLine(
                "- Match vocabulary, confidence, and emotional framing to the character's biological age; children must not sound like adults.");

            if (!string.IsNullOrEmpty(specialInstruction))
            {
                prompt.AppendLine();
                prompt.AppendLine(
                    "SPECIAL SITUATION:");

                prompt.AppendLine(specialInstruction);
            }

            return prompt.ToString();
        }

        private static string BuildInput(
    PawnContext context,
    PawnAIState state)
        {
            StringBuilder prompt =
                new StringBuilder();

            prompt.AppendLine("CHARACTER");

            prompt.AppendLine(
                $"Name: {context.Name}");

            prompt.AppendLine(
                $"Biological age: {context.Age}");

            if (context.Traits != null && context.Traits.Count > 0)
                prompt.AppendLine("Traits: " + string.Join(", ", context.Traits));
            if (context.Skills != null && context.Skills.Count > 0)
            {
                prompt.AppendLine("Skills and passions:");
                foreach (PawnSkillContext skill in context.Skills)
                    if (skill != null && !string.IsNullOrWhiteSpace(skill.Name))
                        prompt.AppendLine("- " + skill.Name + " level " + skill.Level + ", passion " + skill.Passion);
            }

            prompt.AppendLine();

            prompt.AppendLine("MEANING");

            prompt.AppendLine(
                $"Purpose: {state.Meaning.Purpose:0.00}");

            prompt.AppendLine(
                $"Belonging: {state.Meaning.Belonging:0.00}");

            prompt.AppendLine(
                $"Hope: {state.Meaning.Hope:0.00}");

            prompt.AppendLine(
                $"Coherence: {state.Meaning.Coherence:0.00}");

            prompt.AppendLine();

            prompt.AppendLine(
                "RECENT IMPORTANT LIFE EVENTS");

            if (state.LifeEvents == null ||
                state.LifeEvents.Count == 0)
            {
                prompt.AppendLine("None.");
            }
            else
            {
                int start =
                    Math.Max(
                        0,
                        state.LifeEvents.Count - 3);

                for (int i = start;
                     i < state.LifeEvents.Count;
                     i++)
                {
                    if (state.LifeEvents[i] == null)
                        continue;

                    prompt.AppendLine(
                        "- " +
                        state.LifeEvents[i].Description);
                }
            }

            prompt.AppendLine();
            prompt.AppendLine("ANCIENT EARTH STUDY");
            if (state.Knowledge?.KnownTopics == null || state.Knowledge.KnownTopics.Count == 0)
            {
                prompt.AppendLine("No named topics recorded.");
            }
            else
            {
                foreach (KnowledgeTopic topic in state.Knowledge.KnownTopics)
                    if (topic != null && !string.IsNullOrWhiteSpace(topic.TopicId) && topic.Familiarity > 0f)
                        prompt.AppendLine("- " + topic.TopicId + " familiarity " + topic.Familiarity.ToString("0.00"));
            }

            prompt.AppendLine();
            prompt.AppendLine("RECENT INTELLECTUAL EXCHANGES");
            if (state.IntellectualExchanges == null || state.IntellectualExchanges.Count == 0)
            {
                prompt.AppendLine("None.");
            }
            else
            {
                int exchangeStart = Math.Max(0, state.IntellectualExchanges.Count - 2);
                for (int i = exchangeStart; i < state.IntellectualExchanges.Count; i++)
                    if (state.IntellectualExchanges[i] != null)
                        prompt.AppendLine("- " + state.IntellectualExchanges[i].Summary);
            }

            prompt.AppendLine();
            prompt.AppendLine(
                "IMPORTANT MEMORIES");

            if (state.Memories == null ||
                state.Memories.Count == 0)
            {
                prompt.AppendLine("None.");
            }
            else
            {
                int start =
                    Math.Max(
                        0,
                        state.Memories.Count - 5);

                for (int i = start;
                     i < state.Memories.Count;
                     i++)
                {
                    PawnMemory memory =
                        state.Memories[i];

                    if (memory == null)
                        continue;

                    prompt.AppendLine(
                        "- " +
                        memory.Text);
                }
            }

            prompt.AppendLine();

            if (state.LifeGoal == null)
            {
                prompt.AppendLine(
                    "LIFE GOAL: None.");
            }
            else
            {
                prompt.AppendLine(
                    "LIFE GOAL:");

                prompt.AppendLine(
                    state.LifeGoal.Title);

                prompt.AppendLine(
                    state.LifeGoal.Description);
            }
            AppendScholarContext(
                prompt,
                context,
                state);
            return prompt.ToString();
        }
        private static void AppendScholarContext(
    StringBuilder prompt,
    PawnContext pawn,
    PawnAIState state)
        {
            if (prompt == null ||
                pawn == null ||
                state == null ||
                state.ScholarProfile == null)
            {
                return;
            }

            prompt.AppendLine();
            prompt.AppendLine(
                "ARCHIVE SCHOLAR BACKGROUND");

            if (!string.IsNullOrEmpty(
                state.ScholarProfile.Specialty))
            {
                prompt.AppendLine(
                    "Specialty: " +
                    state.ScholarProfile.Specialty);
            }

            if (!string.IsNullOrEmpty(
                state.ScholarProfile.PrimaryTopicId))
            {
                prompt.AppendLine(
                    "Primary field: " +
                    state.ScholarProfile.PrimaryTopicId);
            }

            if (state.ScholarProfile.YearsOfStudy > 0)
            {
                prompt.AppendLine(
                    "Years of study: " +
                    state.ScholarProfile.YearsOfStudy);
            }

            if (!string.IsNullOrEmpty(
                state.ScholarProfile.PreservationMotivation))
            {
                prompt.AppendLine(
                    "Reason for preserving ancient knowledge: " +
                    state.ScholarProfile.PreservationMotivation);
            }

            if (state.ScholarStay == null)
            {
                return;
            }

            prompt.AppendLine();
            prompt.AppendLine(
                "CURRENT SCHOLAR SITUATION");

            if (state.ScholarStay.Active)
            {
                prompt.AppendLine(
                    "The character is currently staying temporarily " +
                    "with the colony for protection.");

                int remainingTicks =
                    state.ScholarStay.EndTick -
                    pawn.CurrentGameTick;

                if (remainingTicks < 0)
                {
                    remainingTicks = 0;
                }

                float remainingDays =
                    remainingTicks / 60000f;

                prompt.AppendLine(
                    "Approximate time remaining: " +
                    remainingDays.ToString("0.0") +
                    " days.");

                prompt.AppendLine(
                    "The character still possesses the Ancient Earth Archive.");
            }
            else if (state.ScholarStay.Completed)
            {
                prompt.AppendLine(
                    "The agreed temporary stay has been completed.");
            }
        }
        private static string BuildRequestJson(
    string instructions,
    string input)
        {
            return
                "{" +
                "\"model\":\"gpt-5.6-luna\"," +
                "\"instructions\":\"" +
                EscapeJson(instructions) +
                "\"," +
                "\"input\":\"" +
                EscapeJson(input) +
                "\"," +
                "\"max_output_tokens\":120" +
                "}";
        }

        private static string EscapeJson(
            string text)
        {
            if (text == null)
                return "";

            return text
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n")
                .Replace("\t", "\\t");
        }
    }
}
