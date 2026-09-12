using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using LiAIChat.Knowledge;
using LiAIChat.Models;

namespace LiAIChat.AI
{
    public class OpenAIKnowledgeDirector
        : IKnowledgeDirector
    {
        private readonly string apiKey;

        private static readonly HttpClient httpClient =
            new HttpClient();

        public OpenAIKnowledgeDirector(
            string apiKey)
        {
            this.apiKey = apiKey;
        }

        public async Task<List<KnowledgeAcquisition>> AnalyzeAsync(
            PawnContext pawn,
            KnowledgeState knowledge,
            IReadOnlyList<ChatMessage> recentConversation)
        {
            string instructions =
                BuildInstructions();

            string input =
                BuildInput(
                    pawn,
                    knowledge,
                    recentConversation);

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
                        "Knowledge Director API failed: " +
                        response.StatusCode +
                        "\n" +
                        responseJson);
                }

                string outputText =
                    OpenAIResponseParser
                        .ExtractOutputText(responseJson);

                return ParseResult(
                    outputText);
            }
        }

        private static string BuildInstructions()
        {
            StringBuilder prompt =
                new StringBuilder();

            prompt.AppendLine(
                "You are a knowledge acquisition analysis system.");

            prompt.AppendLine(
                "You do NOT roleplay the character.");

            prompt.AppendLine(
                "Your job is to determine whether the player actually taught " +
                "the character meaningful new information during the recent conversation.");

            prompt.AppendLine();

            prompt.AppendLine("IMPORTANT RULES:");

            prompt.AppendLine(
                "- Do not award knowledge merely because a topic was mentioned.");

            prompt.AppendLine(
                "- Do not award knowledge for questions alone.");

            prompt.AppendLine(
                "- The player must communicate meaningful information about the topic.");

            prompt.AppendLine(
                "- Consider what the character already knows.");

            prompt.AppendLine(
                "- Repeating information the character already understands well " +
                "should produce little or no learning.");

            prompt.AppendLine(
                "- A short explanation should produce only small learning.");

            prompt.AppendLine(
                "- Do not judge whether the character agrees with an idea. " +
                "Understanding something is different from believing it.");

            prompt.AppendLine(
                "- Only use topic IDs from the ALLOWED TOPICS list.");

            prompt.AppendLine(
                "- Never invent a new topic ID.");

            prompt.AppendLine();

            prompt.AppendLine(
                "LearningStrength must be between 0.000 and 0.050.");

            prompt.AppendLine(
                "Use values near 0.050 only for unusually substantial teaching.");

            prompt.AppendLine(
                "Ordinary useful explanations should usually be around 0.005 to 0.020.");

            prompt.AppendLine();

            prompt.AppendLine(
                "Return one line per learned topic in exactly this format:");

            prompt.AppendLine(
                "TopicId|LearningStrength|Reason");

            prompt.AppendLine();

            prompt.AppendLine(
                "If no meaningful learning occurred, return exactly:");

            prompt.AppendLine("NONE");

            return prompt.ToString();
        }

        private static string BuildInput(
    PawnContext pawn,
    KnowledgeState knowledge,
    IReadOnlyList<ChatMessage> conversation)
        {
            StringBuilder prompt =
                new StringBuilder();

            prompt.AppendLine("CHARACTER");

            prompt.AppendLine(
                $"Name: {pawn.Name}");

            prompt.AppendLine();

            prompt.AppendLine(
                "CURRENT GENERAL KNOWLEDGE");

            prompt.AppendLine(
                $"Ancient Earth history: " +
                $"{knowledge.EarthHistoryKnowledge:0.00}");

            prompt.AppendLine(
                $"Philosophy: " +
                $"{knowledge.PhilosophyKnowledge:0.00}");

            prompt.AppendLine(
                $"Religion: " +
                $"{knowledge.ReligiousKnowledge:0.00}");

            prompt.AppendLine(
                $"Politics: " +
                $"{knowledge.PoliticsKnowledge:0.00}");

            prompt.AppendLine(
                $"Science: " +
                $"{knowledge.ScienceKnowledge:0.00}");

            prompt.AppendLine();

            prompt.AppendLine(
                "CURRENT SPECIFIC KNOWLEDGE");

            if (knowledge.KnownTopics == null ||
                knowledge.KnownTopics.Count == 0)
            {
                prompt.AppendLine(
                    "None recorded.");
            }
            else
            {
                foreach (KnowledgeTopic topic
                         in knowledge.KnownTopics)
                {
                    if (topic == null)
                        continue;

                    prompt.AppendLine(
                        topic.TopicId +
                        " | familiarity " +
                        topic.Familiarity.ToString("0.00"));
                }
            }

            prompt.AppendLine();

            prompt.AppendLine(
                "ALLOWED TOPICS");

            foreach (string topicId
                     in KnowledgeTopicCatalog.GetAllTopicIds())
            {
                prompt.AppendLine(
                    topicId +
                    " | " +
                    KnowledgeTopicCatalog.GetDisplayName(
                        topicId));
            }

            prompt.AppendLine();

            prompt.AppendLine(
                "RECENT CONVERSATION");

            int startIndex =
                Math.Max(
                    0,
                    conversation.Count - 6);

            for (int i = startIndex;
                 i < conversation.Count;
                 i++)
            {
                ChatMessage message =
                    conversation[i];

                string speaker =
                    message.IsPlayer
                        ? "Player"
                        : pawn.Name;

                prompt.AppendLine(
                    speaker +
                    ": " +
                    message.Text);
            }

            return prompt.ToString();
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
                "\"max_output_tokens\":300" +
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

        private static List<KnowledgeAcquisition> ParseResult(
    string text)
        {
            List<KnowledgeAcquisition> results =
                new List<KnowledgeAcquisition>();

            if (string.IsNullOrWhiteSpace(text))
                return results;

            text = text.Trim();

            if (string.Equals(
                text,
                "NONE",
                StringComparison.OrdinalIgnoreCase))
            {
                return results;
            }

            string[] lines =
                text.Split(
                    new[] { '\r', '\n' },
                    StringSplitOptions.RemoveEmptyEntries);

            foreach (string line in lines)
            {
                string[] parts =
                    line.Split(
                        new[] { '|' },
                        3);

                if (parts.Length < 2)
                    continue;

                string topicId =
                    parts[0].Trim();

                if (!KnowledgeTopicCatalog.Contains(
                    topicId))
                {
                    continue;
                }

                if (!float.TryParse(
                    parts[1].Trim(),
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out float learningStrength))
                {
                    continue;
                }

                if (learningStrength < 0f)
                    learningStrength = 0f;

                if (learningStrength > 0.05f)
                    learningStrength = 0.05f;

                if (learningStrength <= 0f)
                    continue;

                string reason =
                    parts.Length >= 3
                        ? parts[2].Trim()
                        : "";

                results.Add(
                    new KnowledgeAcquisition
                    {
                        TopicId = topicId,
                        LearningStrength = learningStrength,
                        Reason = reason
                    });
            }

            return results;
        }
    }
}