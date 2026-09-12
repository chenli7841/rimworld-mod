using LiAIChat.Background;
using LiAIChat.Knowledge;
using LiAIChat.Models;
using System;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace LiAIChat.AI
{
    public class OpenAIIntellectualExchangeImpactDirector : IIntellectualExchangeImpactDirector
    {
        private readonly string apiKey;

        private static readonly HttpClient httpClient = new HttpClient();

        public OpenAIIntellectualExchangeImpactDirector(string apiKey)
        {
            this.apiKey = apiKey;
        }

        public async Task<IntellectualExchangeImpactResult> AnalyzeAsync(PawnAISnapshot pawnA, PawnAISnapshot pawnB, IntellectualExchangeResult exchange)
        {
            string instructions = BuildInstructions();

            string input = BuildInput(pawnA, pawnB, exchange);

            string requestJson = BuildRequestJson(instructions, input);

            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/responses"))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue(
                    "Bearer",
                    apiKey);

                request.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await httpClient.SendAsync(request);

                string responseJson = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception("Exchange Impact API failed: " + response.StatusCode + "\n" + responseJson);
                }

                string text = OpenAIResponseParser.ExtractOutputText(responseJson);

                return ParseResult(text);
            }
        }
        private static string BuildInstructions()
        {
            StringBuilder prompt = new StringBuilder();

            prompt.AppendLine(
                "You analyze the psychological and educational effects " +
                "of a completed conversation between two fictional RimWorld characters.");

            prompt.AppendLine(
                "The conversation has already happened. " +
                "Do not invent a new conversation.");

            prompt.AppendLine();

            prompt.AppendLine("KNOWLEDGE RULES:");

            prompt.AppendLine("- Hearing a topic mentioned is not enough to learn it.");

            prompt.AppendLine("- Learning requires meaningful information to have been communicated.");

            prompt.AppendLine("- A character cannot teach knowledge they did not plausibly possess.");

            prompt.AppendLine("- Existing familiarity should reduce how much is newly learned.");

            prompt.AppendLine("- Learning a topic does not mean agreeing with it.");

            prompt.AppendLine();

            prompt.AppendLine("WORLDVIEW RULES:");

            prompt.AppendLine("- Most ordinary conversations should cause little or no worldview change.");

            prompt.AppendLine("- Understanding is not acceptance.");

            prompt.AppendLine("- A persuasive argument may increase trust, but a strong objection may decrease it.");

            prompt.AppendLine("- Knowledge of Christianity may increase while trust in Christianity decreases.");

            prompt.AppendLine("- Intellectual resistance and emotional resistance are different.");

            prompt.AppendLine("- Do not reward whichever character speaks more confidently.");

            prompt.AppendLine("- Do not assume either character is correct.");

            prompt.AppendLine("- Do not favor Christianity, atheism, or any other worldview.");

            prompt.AppendLine();

            prompt.AppendLine("Worldview delta values represent EFFECT STRENGTH, not literal state changes.");

            prompt.AppendLine("Use values between -0.05 and +0.05.");

            prompt.AppendLine("Near +/-0.05 should be very rare.");

            prompt.AppendLine();

            prompt.AppendLine("LearningStrength must be between 0 and 0.05.");

            prompt.AppendLine("Use NONE when no registered topic was meaningfully learned.");

            prompt.AppendLine();

            prompt.AppendLine("Return exactly 20 lines, using this format:");

            AppendOutputFormat(prompt, "A");

            AppendOutputFormat(prompt, "B");

            return prompt.ToString();
        }

        private static void AppendOutputFormat(StringBuilder prompt, string prefix)
        {
            prompt.AppendLine(prefix + ".LearnedTopicId|NONE or allowed topic id");

            prompt.AppendLine(prefix + ".LearningStrength|0 to 0.05");

            prompt.AppendLine(prefix + ".BeliefInGodDelta|-0.05 to 0.05");

            prompt.AppendLine(prefix + ".BeliefInObjectiveMoralityDelta|-0.05 to 0.05");

            prompt.AppendLine(prefix + ".TrustInChristianityDelta|-0.05 to 0.05");

            prompt.AppendLine(prefix + ".KnowledgeOfChristianityDelta|-0.05 to 0.05");

            prompt.AppendLine(prefix + ".IntellectualResistanceDelta|-0.05 to 0.05");

            prompt.AppendLine(prefix + ".EmotionalResistanceDelta|-0.05 to 0.05");

            prompt.AppendLine(prefix + ".SpiritualInterestDelta|-0.05 to 0.05");

            prompt.AppendLine(prefix + ".Reason|short explanation");
        }

        /// <summary>
        /// 我们需要告诉 Director：这次究竟聊了什么；两个人原本知道什么；两个人原本怎么看。
        /// </summary>
        private static string BuildInput(PawnAISnapshot pawnA, PawnAISnapshot pawnB, IntellectualExchangeResult exchange)
        {
            StringBuilder prompt = new StringBuilder();

            prompt.AppendLine("COMPLETED EXCHANGE");

            prompt.AppendLine("Topic: " + exchange.Topic);

            prompt.AppendLine("Summary: " + exchange.Summary);

            prompt.AppendLine("Pawn A reflection: " + exchange.PawnAReflection);

            prompt.AppendLine("Pawn B reflection: " + exchange.PawnBReflection);

            prompt.AppendLine();

            AppendPawnState(prompt, "PAWN A", pawnA);

            prompt.AppendLine();

            AppendPawnState(prompt, "PAWN B", pawnB);

            prompt.AppendLine();

            prompt.AppendLine("ALLOWED KNOWLEDGE TOPICS:");

            foreach (string topicId in KnowledgeTopicCatalog.GetAllTopicIds())
            {
                prompt.AppendLine("- " + topicId);
            }

            return prompt.ToString();
        }

        private static void AppendPawnState(StringBuilder prompt, string label, PawnAISnapshot pawn)
        {
            prompt.AppendLine(label);

            prompt.AppendLine("Name: " + pawn.Name);

            prompt.AppendLine("BeliefInGod: " + pawn.BeliefInGod.ToString("0.00"));

            prompt.AppendLine("BeliefInObjectiveMorality: " + pawn.BeliefInObjectiveMorality.ToString("0.00"));

            prompt.AppendLine("TrustInChristianity: " + pawn.TrustInChristianity.ToString("0.00"));

            prompt.AppendLine("KnowledgeOfChristianity: " + pawn.KnowledgeOfChristianity.ToString("0.00"));

            prompt.AppendLine("IntellectualResistance: " + pawn.IntellectualResistance.ToString("0.00"));

            prompt.AppendLine("EmotionalResistance: " + pawn.EmotionalResistance.ToString("0.00"));

            prompt.AppendLine("SpiritualInterest: " + pawn.SpiritualInterest.ToString("0.00"));

            prompt.AppendLine("Known topics:");

            if (pawn.KnownTopics == null || pawn.KnownTopics.Count == 0)
            {
                prompt.AppendLine("- None");
            }
            else
            {
                foreach (KnowledgeTopicSnapshot topic in pawn.KnownTopics)
                {
                    if (topic == null)
                        continue;

                    prompt.AppendLine("- " + topic.TopicId + " | " + topic.Familiarity.ToString("0.00"));
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
                "\"max_output_tokens\":700" +
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

        private static IntellectualExchangeImpactResult ParseResult(string text)
        {
            IntellectualExchangeImpactResult result = new IntellectualExchangeImpactResult();

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

                if (key.StartsWith("A."))
                {
                    ParseImpactField(result.PawnA, key.Substring(2), value);
                }
                else if (key.StartsWith("B."))
                {
                    ParseImpactField(result.PawnB, key.Substring(2), value);
                }
            }

            return result;
        }

        private static void ParseImpactField(PawnExchangeImpact impact, string field, string value)
        {
            switch (field)
            {
                case "LearnedTopicId":
                    if (!string.Equals(value, "NONE", StringComparison.OrdinalIgnoreCase) && KnowledgeTopicCatalog.Contains(value))
                    {
                        impact.LearnedTopicId = value;
                    }
                    break;

                case "LearningStrength":
                    impact.LearningStrength = ParseFloat(value, 0f, 0.05f);
                    break;

                case "BeliefInGodDelta":
                    impact.BeliefInGodDelta = ParseFloat(value, -0.05f, 0.05f);
                    break;

                case "BeliefInObjectiveMoralityDelta":
                    impact.BeliefInObjectiveMoralityDelta = ParseFloat(value, -0.05f, 0.05f);
                    break;

                case "TrustInChristianityDelta":
                    impact.TrustInChristianityDelta = ParseFloat(value, -0.05f, 0.05f);
                    break;

                case "KnowledgeOfChristianityDelta":
                    impact.KnowledgeOfChristianityDelta = ParseFloat(value, -0.05f, 0.05f);
                    break;

                case "IntellectualResistanceDelta":
                    impact.IntellectualResistanceDelta = ParseFloat(value, -0.05f, 0.05f);
                    break;

                case "EmotionalResistanceDelta":
                    impact.EmotionalResistanceDelta = ParseFloat(value, -0.05f, 0.05f);
                    break;

                case "SpiritualInterestDelta":
                    impact.SpiritualInterestDelta = ParseFloat(value, -0.05f, 0.05f);
                    break;

                case "Reason":
                    impact.Reason = value;
                    break;
            }
        }
        private static float ParseFloat(string value, float min, float max)
        {
            float parsed;

            if (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed))
            {
                return 0f;
            }

            return Math.Max(min, Math.Min(max, parsed));
        }
    }
}