using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using LiAIChat.Models;

namespace LiAIChat.AI
{
    public class OpenAIWorldviewDirector
        : IWorldviewDirector
    {
        private readonly string apiKey;

        private static readonly HttpClient httpClient =
            new HttpClient();

        public OpenAIWorldviewDirector(
            string apiKey)
        {
            this.apiKey = apiKey;
        }

        public async Task<WorldviewChange> AnalyzeAsync(
            PawnContext pawn,
            WorldviewState worldview,
            IReadOnlyList<ChatMessage> recentConversation)
        {
            string instructions =
                BuildInstructions();

            string input =
                BuildInput(
                    pawn,
                    worldview,
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
                        "Worldview Director API failed: " +
                        response.StatusCode +
                        "\n" +
                        responseJson);
                }

                string outputText =
                    OpenAIResponseParser
                        .ExtractOutputText(responseJson);

                return ParseResult(outputText);
            }
        }

        private static string BuildInstructions()
        {
            StringBuilder prompt =
                new StringBuilder();

            prompt.AppendLine(
                "You are a narrative psychology and worldview analysis system.");

            prompt.AppendLine(
                "You do NOT roleplay the character.");

            prompt.AppendLine(
                "Your job is to evaluate whether the recent conversation " +
                "should cause small, believable changes in the character's worldview.");

            prompt.AppendLine();

            prompt.AppendLine(
                "IMPORTANT RULES:");

            prompt.AppendLine(
                "- Do not reward the player simply for arguing confidently.");

            prompt.AppendLine(
                "- Do not assume the player is correct.");

            prompt.AppendLine(
                "- A character may understand an argument without accepting it.");

            prompt.AppendLine(
                "- Knowledge may increase while trust decreases.");

            prompt.AppendLine(
                "- Resistance may increase if the conversation creates new objections.");

            prompt.AppendLine(
                "- Emotional resistance and intellectual resistance are different.");

            prompt.AppendLine(
                "- Most ordinary conversations should cause little or no worldview change.");

            prompt.AppendLine(
                "- Large worldview changes should be extremely rare.");

            prompt.AppendLine(
                "- Judge the effect relative to this specific character's " +
                "current worldview and background.");

            prompt.AppendLine();

            prompt.AppendLine(
                "Return EXACTLY 8 lines in this format:");

            prompt.AppendLine(
                "BeliefInGodDelta|number");

            prompt.AppendLine(
                "BeliefInObjectiveMoralityDelta|number");

            prompt.AppendLine(
                "TrustInChristianityDelta|number");

            prompt.AppendLine(
                "KnowledgeOfChristianityDelta|number");

            prompt.AppendLine(
                "IntellectualResistanceDelta|number");

            prompt.AppendLine(
                "EmotionalResistanceDelta|number");

            prompt.AppendLine(
                "SpiritualInterestDelta|number");

            prompt.AppendLine(
                "Reason|short explanation");

            prompt.AppendLine();

            prompt.AppendLine(
                "All delta values must be between -0.05 and +0.05.");

            prompt.AppendLine(
                "Treat the delta magnitude as the strength and direction of the conversational effect, " +
                "not as a literal state change that will be applied directly.");

            prompt.AppendLine(
                "Use values near +/-0.05 only when the conversation provides unusually strong evidence " +
                "for movement in that dimension.");

            prompt.AppendLine(
                "Use 0 when there is no meaningful reason to change a value.");

            prompt.AppendLine(
                "Do not produce change merely because the topic was mentioned. " +
                "A value should change only when the character actually learned, reconsidered, " +
                "accepted, rejected, or emotionally reacted to something relevant.");

            return prompt.ToString();
        }

        private static string BuildInput(PawnContext pawn, WorldviewState worldview, IReadOnlyList<ChatMessage> conversation)
        {
            StringBuilder prompt =
                new StringBuilder();

            prompt.AppendLine(
                "CHARACTER");

            prompt.AppendLine(
                $"Name: {pawn.Name}");

            prompt.AppendLine(
                $"Age: {pawn.Age}");

            prompt.AppendLine(
                $"Gender: {pawn.Gender}");

            prompt.AppendLine();

            prompt.AppendLine(
                "CURRENT WORLDVIEW");

            prompt.AppendLine(
                "All values range from 0.0 to 1.0.");

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

        private static string BuildRequestJson(string instructions, string input)
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

        private static WorldviewChange ParseResult(string text)
        {
            WorldviewChange result =
                new WorldviewChange();

            if (string.IsNullOrWhiteSpace(text))
            {
                return result;
            }

            string[] lines =
                text.Split(
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

                string key =
                    line.Substring(
                        0,
                        separatorIndex)
                    .Trim();

                string value =
                    line.Substring(
                        separatorIndex + 1)
                    .Trim();

                switch (key)
                {
                    case "BeliefInGodDelta":
                        result.BeliefInGodDelta =
                            ParseDelta(value);
                        break;

                    case "BeliefInObjectiveMoralityDelta":
                        result.BeliefInObjectiveMoralityDelta =
                            ParseDelta(value);
                        break;

                    case "TrustInChristianityDelta":
                        result.TrustInChristianityDelta =
                            ParseDelta(value);
                        break;

                    case "KnowledgeOfChristianityDelta":
                        result.KnowledgeOfChristianityDelta =
                            ParseDelta(value);
                        break;

                    case "IntellectualResistanceDelta":
                        result.IntellectualResistanceDelta =
                            ParseDelta(value);
                        break;

                    case "EmotionalResistanceDelta":
                        result.EmotionalResistanceDelta =
                            ParseDelta(value);
                        break;

                    case "SpiritualInterestDelta":
                        result.SpiritualInterestDelta =
                            ParseDelta(value);
                        break;

                    case "Reason":
                        result.Reason =
                            value;
                        break;
                }
            }

            return result;
        }

        private static float ParseDelta(
            string value)
        {
            if (!float.TryParse(
                value,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out float delta))
            {
                return 0f;
            }

            if (delta < -0.05f)
                return -0.05f;

            if (delta > 0.05f)
                return 0.05f;

            return delta;
        }
    }
}