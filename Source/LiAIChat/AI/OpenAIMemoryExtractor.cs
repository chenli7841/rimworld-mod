using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using LiAIChat.Models;

namespace LiAIChat.AI
{
    public class OpenAIMemoryExtractor
        : IMemoryExtractor
    {
        private static readonly HttpClient httpClient =
            new HttpClient();

        private readonly string apiKey;

        public OpenAIMemoryExtractor(
            string apiKey)
        {
            this.apiKey = apiKey;
        }

        public async Task<List<PawnMemory>> ExtractAsync(
            PawnContext pawn,
            IReadOnlyList<ChatMessage> messages)
        {
            string instructions =
                "Identify concrete facts or moments from this " +
                "conversation that this RimWorld character may " +
                "remember long term. " +
                "Only include information likely to matter in future " +
                "conversations. " +
                "Do not include ordinary small talk. " +
                "Do not invent anything. " +
                "Return one memory per line in exactly this format: " +
                "importance|memory text " +
                "where importance is between 0.00 and 1.00. " +
                "If there is nothing worth remembering, return NONE.";

            string input =
                BuildInput(
                    pawn,
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
                "\"max_output_tokens\":300" +
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
                        "OpenAI memory API error " +
                        (int)response.StatusCode +
                        ": " +
                        responseJson);
                }

                string output =
                    OpenAIResponseParser
                        .ExtractOutputText(
                            responseJson);

                return ParseMemories(output);
            }
        }

        private static string BuildInput(
            PawnContext pawn,
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
                "CONVERSATION:");

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

            return builder.ToString();
        }

        private static List<PawnMemory>
            ParseMemories(string output)
        {
            List<PawnMemory> memories =
                new List<PawnMemory>();

            if (string.IsNullOrWhiteSpace(output))
            {
                return memories;
            }

            if (output.Trim()
                .Equals(
                    "NONE",
                    StringComparison.OrdinalIgnoreCase))
            {
                return memories;
            }

            string[] lines =
                output.Split(
                    new[]
                    {
                        '\r',
                        '\n'
                    },
                    StringSplitOptions.RemoveEmptyEntries);

            foreach (string line in lines)
            {
                int separatorIndex =
                    line.IndexOf('|');

                if (separatorIndex <= 0)
                    continue;

                string importanceText =
                    line.Substring(
                        0,
                        separatorIndex)
                    .Trim();

                string memoryText =
                    line.Substring(
                        separatorIndex + 1)
                    .Trim();

                if (string.IsNullOrWhiteSpace(memoryText))
                    continue;

                if (!float.TryParse(
                    importanceText,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out float importance))
                {
                    continue;
                }

                importance =
                    Math.Max(
                        0f,
                        Math.Min(
                            1f,
                            importance));

                memories.Add(
                    new PawnMemory(
                        memoryText,
                        importance));
            }

            return memories;
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