using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using LiAIChat.Models;

namespace LiAIChat.AI
{
    public class OpenAIConversationSummarizer
        : IConversationSummarizer
    {
        private static readonly HttpClient httpClient =
            new HttpClient();

        private readonly string apiKey;

        public OpenAIConversationSummarizer(
            string apiKey)
        {
            this.apiKey = apiKey;
        }

        public async Task<string> SummarizeAsync(
            PawnContext pawn,
            string existingSummary,
            IReadOnlyList<ChatMessage> messages)
        {
            string instructions =
                "You maintain long-term conversation memory " +
                "for a fictional RimWorld character. " +
                "Update the existing conversation summary " +
                "using the new conversation messages. " +
                "Preserve important facts, beliefs, questions, " +
                "relationships, promises, disagreements, " +
                "personal information shared by the player, " +
                "and major changes in the character's thinking. " +
                "Do not invent information. " +
                "Do not roleplay or respond to the player. " +
                "Write a concise factual memory summary. " +
                "Prefer information likely to matter in future conversations.";

            string input =
                BuildSummaryInput(
                    pawn,
                    existingSummary,
                    messages);

            string json =
                "{" +
                "\"model\":\"gpt-5.6-luna\"," +
                "\"instructions\":\"" +
                EscapeJson(instructions) +
                "\"," +
                "\"input\":\"" +
                EscapeJson(input) +
                "\"," +
                "\"max_output_tokens\":500" +
                "}";

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

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception(
                        "OpenAI summary API error " +
                        (int)response.StatusCode +
                        ": " +
                        responseJson);
                }
                return OpenAIResponseParser.ExtractOutputText(responseJson);
            }
        }

        private static string BuildSummaryInput(
            PawnContext pawn,
            string existingSummary,
            IReadOnlyList<ChatMessage> messages)
        {
            StringBuilder builder =
                new StringBuilder();

            builder.AppendLine(
                "CHARACTER:");

            builder.AppendLine(
                pawn.Name);

            builder.AppendLine();

            builder.AppendLine(
                "EXISTING LONG-TERM CONVERSATION SUMMARY:");

            if (string.IsNullOrWhiteSpace(existingSummary))
            {
                builder.AppendLine(
                    "(No previous summary.)");
            }
            else
            {
                builder.AppendLine(
                    existingSummary);
            }

            builder.AppendLine();

            builder.AppendLine(
                "NEW CONVERSATION TO INCORPORATE:");

            builder.AppendLine();

            foreach (ChatMessage message in messages)
            {
                builder.AppendLine(
                    message.IsPlayer
                        ? "Player:"
                        : pawn.Name + ":");

                builder.AppendLine(
                    message.Text);

                builder.AppendLine();
            }

            builder.AppendLine(
                "Return the updated memory summary only.");

            return builder.ToString();
        }

        private static string EscapeJson(
            string value)
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
    }
}