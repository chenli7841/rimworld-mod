using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using LiAIChat.Models;

namespace LiAIChat.AI
{
    public class OpenAILifeGoalDirector
        : ILifeGoalDirector
    {
        private readonly string apiKey;

        private static readonly HttpClient httpClient =
            new HttpClient();

        public OpenAILifeGoalDirector(
            string apiKey)
        {
            this.apiKey = apiKey;
        }

        public async Task<LifeGoalProposal> AnalyzeAsync(
            PawnContext pawn,
            PawnAIState state)
        {
            string instructions =
                BuildInstructions();

            string input =
                BuildInput(
                    pawn,
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
                        "Life Goal Director failed: " +
                        response.StatusCode +
                        "\n" +
                        responseJson);
                }

                string outputText =
                    OpenAIResponseParser
                        .ExtractOutputText(
                            responseJson);

                return ParseResult(
                    outputText);
            }
        }

        private static string BuildInstructions()
        {
            StringBuilder prompt =
                new StringBuilder();

            prompt.AppendLine(
                "You are a narrative psychology system for a fictional RimWorld character.");

            prompt.AppendLine(
                "You do NOT roleplay the character.");

            prompt.AppendLine(
                "Your job is to determine whether this character has enough psychological reason " +
                "to form a meaningful long-term life goal.");

            prompt.AppendLine();

            prompt.AppendLine("IMPORTANT RULES:");

            prompt.AppendLine(
                "- Do not create a goal just because the system asks for one.");

            prompt.AppendLine(
                "- It is valid for the character to have no clear life goal.");

            prompt.AppendLine(
                "- Goals should emerge from personality, memories, losses, relationships, worldview, " +
                "knowledge, meaning, and major life experiences.");

            prompt.AppendLine(
                "- A life goal should be broader than a single immediate task.");

            prompt.AppendLine(
                "- Avoid goals like 'eat dinner', 'build one wall', or 'finish today's research'.");

            prompt.AppendLine(
                "- Prefer goals that could plausibly matter for months or years of the character's life.");

            prompt.AppendLine(
                "- Do not require gameplay systems that do not exist in RimWorld.");

            prompt.AppendLine(
                "- Translate abstract beliefs into goals achievable through existing or plausible colony actions.");

            prompt.AppendLine(
                "- Christianity, atheism, transhumanism, family, knowledge, service, wealth, survival, " +
                "legacy, justice, exploration, or community can all become sources of meaning.");

            prompt.AppendLine(
                "- Do not treat religion as automatically superior or inferior.");

            prompt.AppendLine(
                "- Do not make every character altruistic.");

            prompt.AppendLine(
                "- Do not make every low-meaning character depressed or suicidal.");

            prompt.AppendLine();

            prompt.AppendLine(
                "Commitment must be between 0.00 and 1.00.");

            prompt.AppendLine(
                "Newly emerging goals should usually begin between 0.30 and 0.65.");

            prompt.AppendLine();

            prompt.AppendLine(
                "Return exactly 5 lines:");

            prompt.AppendLine(
                "ShouldCreateGoal|true or false");

            prompt.AppendLine(
                "Title|short title");

            prompt.AppendLine(
                "Description|one clear sentence");

            prompt.AppendLine(
                "Reason|brief psychological explanation");

            prompt.AppendLine(
                "Commitment|0.00 to 1.00");

            return prompt.ToString();
        }

        private static string BuildInput(
    PawnContext pawn,
    PawnAIState state)
        {
            StringBuilder prompt =
                new StringBuilder();

            prompt.AppendLine("CHARACTER");

            prompt.AppendLine(
                $"Name: {pawn.Name}");

            prompt.AppendLine(
                $"Age: {pawn.Age}");

            prompt.AppendLine(
                $"Gender: {pawn.Gender}");

            if (pawn.Traits != null &&
                pawn.Traits.Count > 0)
            {
                prompt.AppendLine(
                    "Traits: " +
                    string.Join(", ", pawn.Traits));
            }

            prompt.AppendLine();

            prompt.AppendLine("WORLDVIEW");

            prompt.AppendLine(
                $"Belief in God: " +
                $"{state.Worldview.BeliefInGod:0.00}");

            prompt.AppendLine(
                $"Trust in Christianity: " +
                $"{state.Worldview.TrustInChristianity:0.00}");

            prompt.AppendLine(
                $"Spiritual interest: " +
                $"{state.Worldview.SpiritualInterest:0.00}");

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

            prompt.AppendLine(
                $"Transcendence: {state.Meaning.Transcendence:0.00}");
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
                        state.Memories.Count - 8);

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
                        state.LifeEvents.Count - 5);

                for (int i = start;
                     i < state.LifeEvents.Count;
                     i++)
                {
                    PawnLifeEvent lifeEvent =
                        state.LifeEvents[i];

                    if (lifeEvent == null)
                        continue;

                    prompt.AppendLine(
                        "- " +
                        lifeEvent.Description);
                }
            }
            prompt.AppendLine();

            if (state.LifeGoal == null)
            {
                prompt.AppendLine(
                    "CURRENT LIFE GOAL: None");
            }
            else
            {
                prompt.AppendLine(
                    "CURRENT LIFE GOAL:");

                prompt.AppendLine(
                    state.LifeGoal.Title);

                prompt.AppendLine(
                    state.LifeGoal.Description);

                prompt.AppendLine(
                    "Commitment: " +
                    state.LifeGoal.Commitment.ToString("0.00"));
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
                "\"max_output_tokens\":400" +
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
        private static LifeGoalProposal ParseResult(
    string text)
        {
            LifeGoalProposal result =
                new LifeGoalProposal();

            if (string.IsNullOrWhiteSpace(text))
                return result;

            string[] lines =
                text.Split(
                    new[] { '\r', '\n' },
                    StringSplitOptions.RemoveEmptyEntries);

            foreach (string line in lines)
            {
                string[] parts =
                    line.Split(
                        new[] { '|' },
                        2);

                if (parts.Length != 2)
                    continue;

                string key =
                    parts[0].Trim();

                string value =
                    parts[1].Trim();

                switch (key)
                {
                    case "ShouldCreateGoal":
                        result.ShouldCreateGoal =
                            string.Equals(
                                value,
                                "true",
                                StringComparison.OrdinalIgnoreCase);
                        break;

                    case "Title":
                        result.Title =
                            value;
                        break;

                    case "Description":
                        result.Description =
                            value;
                        break;

                    case "Reason":
                        result.Reason =
                            value;
                        break;

                    case "Commitment":
                        if (float.TryParse(
                            value,
                            System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture,
                            out float commitment))
                        {
                            result.Commitment =
                                Clamp01(commitment);
                        }
                        break;
                }
            }

            return result;
        }

        private static float Clamp01(
            float value)
        {
            if (value < 0f)
                return 0f;

            if (value > 1f)
                return 1f;

            return value;
        }
    }
}