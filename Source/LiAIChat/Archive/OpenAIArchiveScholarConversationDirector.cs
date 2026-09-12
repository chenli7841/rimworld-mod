using LiAIChat.AI;
using LiAIChat.Models;
using LiAIChat.State;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace LiAIChat.Archive
{
    public class OpenAIArchiveScholarConversationDirector
        : IArchiveScholarConversationDirector
    {
        private readonly string apiKey;

        private static readonly HttpClient httpClient =
            new HttpClient();


        public OpenAIArchiveScholarConversationDirector(
            string apiKey)
        {
            this.apiKey = apiKey;
        }


        public async Task<ArchiveScholarConversationImpact>
            AnalyzeAsync(
                PawnContext context,
                PawnAIState state,
                string playerMessage,
                string scholarResponse)
        {
            if (context == null ||
                state == null ||
                state.ScholarProfile == null ||
                state.ScholarStay == null)
            {
                return null;
            }

            string instructions =
                BuildInstructions();

            string input =
                BuildInput(
                    context,
                    state,
                    playerMessage,
                    scholarResponse);

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
                    await httpClient.SendAsync(
                        request);

                string responseJson =
                    await response.Content
                        .ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception(
                        "Archive Scholar Conversation Director failed: " +
                        response.StatusCode +
                        "\n" +
                        responseJson);
                }

                string output =
                    OpenAIResponseParser
                        .ExtractOutputText(
                            responseJson)
                        .Trim();

                return ParseResult(
                    output);
            }
        }


        private static string BuildInstructions()
        {
            StringBuilder prompt =
                new StringBuilder();

            prompt.AppendLine(
                "Analyze one completed conversation exchange between " +
                "the player and a fictional wandering Archive Scholar.");

            prompt.AppendLine();

            prompt.AppendLine(
                "Your task is NOT to decide whether the Scholar gives away the archive.");

            prompt.AppendLine(
                "Only evaluate how this exchange affects the Scholar's impression " +
                "of the colony and whether the exchange was meaningfully substantive.");

            prompt.AppendLine();

            prompt.AppendLine("RULES:");

            prompt.AppendLine(
                "- MeaningfulConversation should be true only if the exchange contains substantive personal, intellectual, moral, historical, philosophical, religious, or relational content.");

            prompt.AppendLine(
                "- Greetings, small talk, repeated questions, or trivial remarks should normally be false.");

            prompt.AppendLine(
                "- HospitalityDelta represents only this exchange's effect on the Scholar's impression of the colony.");

            prompt.AppendLine(
                "- HospitalityDelta must be between -0.20 and +0.20.");

            prompt.AppendLine(
                "- Most normal exchanges should be close to 0.");

            prompt.AppendLine(
                "- Respect, sincere interest, empathy, thoughtful disagreement, and concern may produce a small positive change.");

            prompt.AppendLine(
                "- Contempt, threats, cruelty, mockery, manipulation, or deliberate disrespect may produce a negative change.");

            prompt.AppendLine(
                "- Do not reward the player merely for agreeing with the Scholar.");

            prompt.AppendLine(
                "- Respectful disagreement can still be positive.");

            prompt.AppendLine(
                "- Do not infer events that are not present.");

            prompt.AppendLine();

            prompt.AppendLine(
                "Return exactly three lines:");

            prompt.AppendLine(
                "MeaningfulConversation=true|false");

            prompt.AppendLine(
                "HospitalityDelta=<number>");

            prompt.AppendLine(
                "Reason=<short explanation>");

            return prompt.ToString();
        }


        private static string BuildInput(
            PawnContext context,
            PawnAIState state,
            string playerMessage,
            string scholarResponse)
        {
            StringBuilder prompt =
                new StringBuilder();

            prompt.AppendLine("SCHOLAR");

            prompt.AppendLine(
                "Name: " +
                context.Name);

            if (state.ScholarProfile != null)
            {
                prompt.AppendLine(
                    "Specialty: " +
                    state.ScholarProfile.Specialty);

                prompt.AppendLine(
                    "Years of study: " +
                    state.ScholarProfile.YearsOfStudy);

                prompt.AppendLine(
                    "Preservation motivation: " +
                    state.ScholarProfile.PreservationMotivation);
            }

            prompt.AppendLine();

            prompt.AppendLine(
                "CURRENT COLONY IMPRESSION:");

            prompt.AppendLine(
                state.ScholarStay
                    .HospitalityImpression
                    .ToString("0.00"));

            prompt.AppendLine();

            prompt.AppendLine(
                "PLAYER MESSAGE:");

            prompt.AppendLine(
                playerMessage ?? "");

            prompt.AppendLine();

            prompt.AppendLine(
                "SCHOLAR RESPONSE:");

            prompt.AppendLine(
                scholarResponse ?? "");

            return prompt.ToString();
        }


        private static ArchiveScholarConversationImpact
            ParseResult(
                string output)
        {
            if (string.IsNullOrWhiteSpace(
                output))
            {
                return null;
            }

            ArchiveScholarConversationImpact result =
                new ArchiveScholarConversationImpact();

            string[] lines =
                output.Split(
                    new[]
                    {
                        '\r',
                        '\n'
                    },
                    StringSplitOptions
                        .RemoveEmptyEntries);

            foreach (string rawLine in lines)
            {
                string line =
                    rawLine.Trim();

                if (line.StartsWith(
                    "MeaningfulConversation=",
                    StringComparison.OrdinalIgnoreCase))
                {
                    string value =
                        line.Substring(
                            "MeaningfulConversation="
                                .Length)
                            .Trim();

                    bool parsed;

                    if (bool.TryParse(
                        value,
                        out parsed))
                    {
                        result.MeaningfulConversation =
                            parsed;
                    }
                }
                else if (line.StartsWith(
                    "HospitalityDelta=",
                    StringComparison.OrdinalIgnoreCase))
                {
                    string value =
                        line.Substring(
                            "HospitalityDelta="
                                .Length)
                            .Trim();

                    float parsed;

                    if (float.TryParse(
                        value,
                        System.Globalization
                            .NumberStyles.Float,
                        System.Globalization
                            .CultureInfo.InvariantCulture,
                        out parsed))
                    {
                        result.HospitalityDelta =
                            parsed;
                    }
                }
                else if (line.StartsWith(
                    "Reason=",
                    StringComparison.OrdinalIgnoreCase))
                {
                    result.Reason =
                        line.Substring(
                            "Reason="
                                .Length)
                            .Trim();
                }
            }

            return result;
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
                "\"max_output_tokens\":160" +
                "}";
        }


        private static string EscapeJson(
            string text)
        {
            if (text == null)
            {
                return "";
            }

            return text
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n")
                .Replace("\t", "\\t");
        }
    }
}