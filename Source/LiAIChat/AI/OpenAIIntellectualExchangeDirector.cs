using LiAIChat.Background;
using LiAIChat.Models;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace LiAIChat.AI
{
    public class OpenAIIntellectualExchangeDirector
        : IIntellectualExchangeDirector
    {
        private readonly string apiKey;

        private static readonly HttpClient httpClient =
            new HttpClient();

        public OpenAIIntellectualExchangeDirector(
            string apiKey)
        {
            this.apiKey = apiKey;
        }

        public async Task<IntellectualExchangeResult> GenerateAsync(PawnAISnapshot pawnA, PawnAISnapshot pawnB)
        {
            string instructions = BuildInstructions();

            string input = BuildInput(pawnA, pawnB);

            string requestJson = BuildRequestJson(instructions, input);

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
                        "Intellectual Exchange API failed: " +
                        response.StatusCode +
                        "\n" +
                        responseJson);
                }

                string text =
                    OpenAIResponseParser
                        .ExtractOutputText(responseJson);

                return ParseResult(text);
            }
        }

        /// <summary>
        /// Prompt：不要强迫每两个 Pawn 都聊哲学
        /// </summary>
        private static string BuildInstructions()
        {
            StringBuilder prompt =
                new StringBuilder();

            prompt.AppendLine(
                "You simulate whether two fictional RimWorld characters " +
                "would have a meaningful intellectual conversation.");

            prompt.AppendLine(
                "You do not write a long transcript.");

            prompt.AppendLine();

            prompt.AppendLine("RULES:");

            prompt.AppendLine(
                "- It is valid for no meaningful exchange to occur.");

            prompt.AppendLine(
                "- Do not force philosophy, religion, or politics into every interaction.");

            prompt.AppendLine(
                "- A conversation should emerge from something at least one character " +
                "actually cares about, remembers, recently experienced, knows, or is questioning.");

            prompt.AppendLine(
                "- The two characters may agree, disagree, misunderstand, challenge, teach, " +
                "encourage, or simply become curious.");

            prompt.AppendLine(
                "- Do not make either character omniscient.");

            prompt.AppendLine(
                "- Respect each character's knowledge limitations.");

            prompt.AppendLine(
                "- Do not assume that hearing an idea causes belief.");

            prompt.AppendLine(
                "- Do not assume disagreement causes hostility.");

            prompt.AppendLine(
                "- Keep the exchange plausible for ordinary colony life.");

            prompt.AppendLine(
                "- Do not invent major external events.");

            prompt.AppendLine();

            prompt.AppendLine(
                "Return exactly 5 lines:");

            prompt.AppendLine(
                "ShouldExchange|true or false");

            prompt.AppendLine(
                "Topic|short topic");

            prompt.AppendLine(
                "Summary|one or two sentences describing what they discussed");

            prompt.AppendLine(
                "PawnAReflection|one sentence about what Pawn A took away");

            prompt.AppendLine(
                "PawnBReflection|one sentence about what Pawn B took away");

            return prompt.ToString();
        }

        /// <summary>
        /// BuildInput 不要把所有存档都塞进去，否则 prompt 越来越大。只传核心状态。
        /// </summary>
        private static string BuildInput(PawnAISnapshot pawnA, PawnAISnapshot pawnB)
        {
            StringBuilder prompt = new StringBuilder();
            AppendPawn(prompt, "PAWN A", pawnA);
            prompt.AppendLine();
            AppendPawn(prompt, "PAWN B", pawnB);
            return prompt.ToString();
        }

        private static void AppendPawn(StringBuilder prompt, string label, PawnAISnapshot pawn)
        {
            prompt.AppendLine(label);

            prompt.AppendLine("Name: " + pawn.Name);

            if (pawn.Traits != null && pawn.Traits.Count > 0)
            {
                prompt.AppendLine("Traits: " + string.Join(", ", pawn.Traits));
            }

            prompt.AppendLine("Belief in God: " + pawn.BeliefInGod.ToString("0.00"));
            prompt.AppendLine("Trust in Christianity: " + pawn.TrustInChristianity.ToString("0.00"));
            prompt.AppendLine("Spiritual interest: " + pawn.SpiritualInterest.ToString("0.00"));
            prompt.AppendLine("Purpose: " + pawn.Purpose.ToString("0.00"));
            prompt.AppendLine("Hope: " + pawn.Hope.ToString("0.00"));
            prompt.AppendLine("Coherence: " + pawn.Coherence.ToString("0.00"));

            if (pawn.LifeGoalTitle != null)
            {
                prompt.AppendLine("Life goal: " + pawn.LifeGoalTitle);
            }

            prompt.AppendLine("Recent important memories:");

            // 加少量记忆
            if (pawn.ImportantMemories == null || pawn.ImportantMemories.Count == 0)
            {
                prompt.AppendLine("- None");
            }
            else
            {
                int start = Math.Max(0, pawn.ImportantMemories.Count - 4);

                for (int i = start; i < pawn.ImportantMemories.Count; i++)
                {
                    prompt.AppendLine("- " + pawn.ImportantMemories[i]);
                }
            }

            // 加known topics
            prompt.AppendLine("Known topics:");

            if (pawn.KnownTopics == null || pawn.KnownTopics.Count == 0)
            {
                prompt.AppendLine("- None");
            }
            else
            {
                int count = 0;

                foreach (KnowledgeTopicSnapshot topic in pawn.KnownTopics)
                {
                    if (topic == null)
                        continue;

                    prompt.AppendLine("- " + topic.TopicId + " | " + topic.Familiarity.ToString("0.00"));

                    count++;

                    if (count >= 8)
                        break;
                }
            }
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
                "\"max_output_tokens\":400" +
                "}";
        }

        private static string EscapeJson(string text)
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
        private static IntellectualExchangeResult ParseResult(string text)
        {
            IntellectualExchangeResult result = new IntellectualExchangeResult();

            if (string.IsNullOrWhiteSpace(text))
                return result;

            string[] lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string line in lines)
            {
                string[] parts = line.Split(new[] { '|' }, 2);

                if (parts.Length != 2)
                    continue;

                string key = parts[0].Trim();

                string value = parts[1].Trim();

                switch (key)
                {
                    case "ShouldExchange":
                        result.ShouldExchange = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
                        break;

                    case "Topic":
                        result.Topic = value;
                        break;

                    case "Summary":
                        result.Summary = value;
                        break;

                    case "PawnAReflection":
                        result.PawnAReflection = value;
                        break;

                    case "PawnBReflection":
                        result.PawnBReflection = value;
                        break;
                }
            }

            return result;
        }
    }
}