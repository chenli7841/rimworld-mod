using LiAIChat.AI;
using LiAIChat.Background;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace LiAIChat.Archive
{
    public class OpenAIArchiveReflectionGenerator
        : IArchiveReflectionGenerator
    {
        private readonly HttpClient httpClient;
        private readonly string apiKey;
        private readonly string model;

        public OpenAIArchiveReflectionGenerator(
            HttpClient httpClient,
            string apiKey,
            string model = "gpt-5.6-luna")
        {
            this.httpClient =
                httpClient ??
                throw new ArgumentNullException(
                    nameof(httpClient));

            this.apiKey =
                apiKey ??
                throw new ArgumentNullException(
                    nameof(apiKey));

            this.model = model;
        }

        public async Task<string> GenerateAsync(
            PawnAISnapshot pawnSnapshot,
            EarthTextDef earthText,
            string sourceDescription)
        {
            if (pawnSnapshot == null)
            {
                throw new ArgumentNullException(
                    nameof(pawnSnapshot));
            }

            if (earthText == null)
            {
                throw new ArgumentNullException(
                    nameof(earthText));
            }

            string prompt =
                BuildPrompt(
                    pawnSnapshot,
                    earthText,
                    sourceDescription);

            string requestJson =
                BuildRequestJson(prompt);

            using (HttpRequestMessage request =
                new HttpRequestMessage(
                    HttpMethod.Post,
                    "https://api.openai.com/v1/responses"))
            {
                request.Headers.Add(
                    "Authorization",
                    "Bearer " + apiKey);

                request.Content =
                    new StringContent(
                        requestJson,
                        Encoding.UTF8,
                        "application/json");

                using (HttpResponseMessage response =
                    await httpClient.SendAsync(request)
                        .ConfigureAwait(false))
                {
                    string responseJson =
                        await response.Content
                            .ReadAsStringAsync()
                            .ConfigureAwait(false);

                    if (!response.IsSuccessStatusCode)
                    {
                        throw new Exception(
                            "OpenAI archive reflection request failed. " +
                            "Status=" +
                            (int)response.StatusCode +
                            " Response=" +
                            responseJson);
                    }

                    string reflection =
                        OpenAIResponseParser
                            .ExtractOutputText(
                                responseJson);

                    if (string.IsNullOrWhiteSpace(
                        reflection))
                    {
                        return null;
                    }

                    return reflection.Trim();
                }
            }
        }

        private string BuildPrompt(
            PawnAISnapshot pawnSnapshot,
            EarthTextDef earthText,
            string sourceDescription)
        {
            StringBuilder builder =
                new StringBuilder();

            builder.AppendLine(
                "You are writing the private reflection of a RimWorld pawn.");

            builder.AppendLine();

            builder.AppendLine(
                "The pawn has just finished studying an Ancient Earth archive.");

            builder.AppendLine();

            builder.AppendLine(
    "EARTH TEXT:");

            builder.AppendLine(
                "Title: " +
                Safe(earthText.title));

            if (!string.IsNullOrWhiteSpace(
                earthText.titleChinese))
            {
                builder.AppendLine(
                    "Chinese title: " +
                    earthText.titleChinese);
            }

            builder.AppendLine(
                "Author: " +
                Safe(earthText.author));

            builder.AppendLine(
                "Approximate date: " +
                earthText.YearDisplay);

            builder.AppendLine(
                "Primary knowledge topic: " +
                Safe(earthText.primaryTopicId));

            builder.AppendLine(
                "Archive source: " +
                Safe(sourceDescription));

            builder.AppendLine();

            builder.AppendLine(
                "PAWN:");

            AppendPawnContext(
                builder,
                pawnSnapshot);

            builder.AppendLine();

            builder.AppendLine(
                "INSTRUCTIONS:");

            builder.AppendLine(
                "- Write one short first-person reflection.");

            builder.AppendLine(
                "- Usually write 1 to 3 sentences.");

            builder.AppendLine(
                "- Reflect this pawn's existing personality, worldview, knowledge, and concerns.");

            builder.AppendLine(
                "- Understanding the archive does not imply agreeing with it.");

            builder.AppendLine(
                "- Do not automatically make the pawn more religious, less religious, or otherwise change beliefs.");

            builder.AppendLine(
                "- Do not invent detailed Ancient Earth knowledge that the pawn has not learned.");

            builder.AppendLine(
                "- Base the reflection on the identified Earth text listed above.");

            builder.AppendLine(
                "- Do not claim the pawn has memorized or fully mastered the entire work.");

            builder.AppendLine(
                "- The reflection may contain curiosity, agreement, skepticism, confusion, interest, or unresolved questions.");

            builder.AppendLine(
                "- Do not mention numeric values, prompts, AI, language models, game mechanics, or internal systems.");

            builder.AppendLine(
                "- Do not describe actions that did not happen.");

            builder.AppendLine(
                "- Return only the reflection text.");

            return builder.ToString();
        }

        private void AppendPawnContext(
            StringBuilder builder,
            PawnAISnapshot snapshot)
        {
            /*
             * IMPORTANT:
             *
             * PawnAISnapshot 的实际字段要以你项目里的 class 为准。
             *
             * 下面先只使用最安全的字段。
             * 如果你的 PawnAISnapshot 字段名称不同，
             * 对应修改这里即可。
             */

            builder.AppendLine(
                "Pawn ID: " +
                snapshot.PawnId);

            /*
             * 等我们下一步看到你的 PawnAISnapshot 完整定义以后，
             * 再把这些真正接进来：
             *
             * Name
             * Worldview
             * Meaning
             * Knowledge
             * relevant memories
             * LifeGoal
             *
             * 现在不要为了编译这一课而猜字段名。
             */
        }

        private string BuildRequestJson(
            string prompt)
        {
            return
                "{" +
                    "\"model\":\"" +
                    EscapeJson(model) +
                    "\"," +

                    "\"input\":\"" +
                    EscapeJson(prompt) +
                    "\"" +
                "}";
        }

        private static string Safe(
            string value)
        {
            if (string.IsNullOrWhiteSpace(
                value))
            {
                return "Unknown";
            }

            return value.Trim();
        }

        private static string EscapeJson(
            string value)
        {
            if (value == null)
            {
                return "";
            }

            return value
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n")
                .Replace("\t", "\\t");
        }
    }
}