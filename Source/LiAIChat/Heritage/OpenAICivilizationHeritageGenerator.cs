using LiAIChat.AI;
using LiAIChat.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace LiAIChat.Heritage
{
    public class OpenAICivilizationHeritageGenerator
    {
        private readonly HttpClient httpClient;
        private readonly string apiKey;

        public OpenAICivilizationHeritageGenerator(
            HttpClient httpClient,
            string apiKey)
        {
            this.httpClient = httpClient;
            this.apiKey = apiKey;
        }

        public async Task<CivilizationHeritageText> GenerateAsync(
            string creatorName,
            string productLabel,
            string productKind,
            string materialLabel,
            string qualityLabel,
            List<string> topics,
            List<string> texts)
        {
            string prompt = BuildPrompt(
                creatorName,
                productLabel,
                productKind,
                materialLabel,
                qualityLabel,
                topics,
                texts);

            string requestJson =
                "{" +
                "\"model\":\"gpt-5.6-luna\"," +
                "\"input\":\"" + EscapeJson(prompt) + "\"," +
                "\"max_output_tokens\":180" +
                "}";

            using (HttpRequestMessage request =
                new HttpRequestMessage(
                    HttpMethod.Post,
                    "https://api.openai.com/v1/responses"))
            {
                request.Headers.Add("Authorization", "Bearer " + apiKey);
                request.Content = new StringContent(
                    requestJson,
                    Encoding.UTF8,
                    "application/json");

                using (HttpResponseMessage response =
                    await httpClient.SendAsync(request).ConfigureAwait(false))
                {
                    string responseJson =
                        await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                    if (!response.IsSuccessStatusCode)
                    {
                        throw new Exception(
                            "Civilization heritage request failed. Status=" +
                            (int)response.StatusCode);
                    }

                    return Parse(
                        OpenAIResponseParser.ExtractOutputText(responseJson));
                }
            }
        }

        private static string BuildPrompt(
            string creatorName,
            string productLabel,
            string productKind,
            string materialLabel,
            string qualityLabel,
            List<string> topics,
            List<string> texts)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("为 RimWorld 殖民地的一件文明遗产作品写中文标题和物品说明。");
            builder.AppendLine("创作者: " + Safe(creatorName));
            builder.AppendLine("成品: " + Safe(productLabel));
            builder.AppendLine("类别: " + Safe(productKind));
            builder.AppendLine("材质: " + Safe(materialLabel));
            builder.AppendLine("品质: " + Safe(qualityLabel));
            builder.AppendLine("创作者熟悉的主题: " + JoinOrNone(topics));
            builder.AppendLine("创作者研读过的文献: " + JoinOrNone(texts));
            builder.AppendLine();
            builder.AppendLine("要求:");
            builder.AppendLine("- 只使用列出的知识作为灵感；若没有文献，不要编造具体引文。 ");
            builder.AppendLine("- 描述应让作品类别、材质和制作者的知识自然结合。 ");
            builder.AppendLine("- 不要提及 AI、提示词、数值、游戏机制或现实世界玩家。 ");
            builder.AppendLine("- 不要假装是历史原文或给出虚构的直接引文。 ");
            builder.AppendLine("- 标题精炼，描述为 1 至 2 句、适合物品检查面板。 ");
            builder.AppendLine("- 严格只返回两行：TITLE: 标题 和 DESCRIPTION: 描述。");
            return builder.ToString();
        }

        private static CivilizationHeritageText Parse(string output)
        {
            if (string.IsNullOrWhiteSpace(output))
            {
                return null;
            }

            string title = "";
            string description = "";
            string[] lines = output.Replace("\r", "").Split('\n');

            foreach (string line in lines)
            {
                string trimmed = line.Trim();

                if (trimmed.StartsWith("TITLE:", StringComparison.OrdinalIgnoreCase))
                {
                    title = trimmed.Substring("TITLE:".Length).Trim();
                }
                else if (trimmed.StartsWith("DESCRIPTION:", StringComparison.OrdinalIgnoreCase))
                {
                    description = trimmed.Substring("DESCRIPTION:".Length).Trim();
                }
            }

            if (string.IsNullOrWhiteSpace(description))
            {
                description = output.Trim();
            }

            if (string.IsNullOrWhiteSpace(title))
            {
                title = "未命名的文明遗产";
            }

            return new CivilizationHeritageText
            {
                Title = Trim(title, 80),
                Description = Trim(description, 420)
            };
        }

        private static string JoinOrNone(List<string> values)
        {
            return values == null || values.Count == 0
                ? "无"
                : string.Join("；", values.ToArray());
        }

        private static string Safe(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "未知" : value.Trim();
        }

        private static string Trim(string value, int maximumLength)
        {
            if (value == null || value.Length <= maximumLength)
            {
                return value;
            }

            return value.Substring(0, maximumLength).TrimEnd();
        }

        private static string EscapeJson(string value)
        {
            return (value ?? "")
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n")
                .Replace("\t", "\\t");
        }
    }

    public class CivilizationHeritageText
    {
        public string Title;
        public string Description;
    }
}
