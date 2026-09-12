using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace LiAIChat.Archive
{
    public static class ArchiveReflectionRetriever
    {
        public static List<ArchiveReflection> GetRelevant(
            List<ArchiveReflection> reflections,
            string playerMessage,
            int maxCount = 3)
        {
            if (reflections == null ||
                reflections.Count == 0)
            {
                return new List<ArchiveReflection>();
            }

            HashSet<string> messageWords =
                Tokenize(playerMessage);

            var scored =
                reflections
                    .Where(r =>
                        r != null &&
                        !string.IsNullOrWhiteSpace(r.Text))
                    .Select(r => new
                    {
                        Reflection = r,
                        Score = CalculateScore(
                            r,
                            messageWords)
                    })
                    .Where(x => x.Score > 0)
                    .OrderByDescending(x => x.Score)
                    .ThenByDescending(
                        x => x.Reflection.CreatedTick)
                    .Take(maxCount)
                    .Select(x => x.Reflection)
                    .ToList();

            return scored;
        }

        private static int CalculateScore(
    ArchiveReflection reflection,
    HashSet<string> messageWords)
        {
            if (reflection == null ||
                messageWords == null ||
                messageWords.Count == 0)
            {
                return 0;
            }

            int score = 0;

            // Reflection 本身的文字
            score += CountOverlap(
                messageWords,
                Tokenize(reflection.Text));

            // 找到产生这条 Reflection 的 Archive Content
            ArchiveContentDef content =
                DefDatabase<ArchiveContentDef>
                    .GetNamedSilentFail(
                        reflection.ContentDefName);

            if (content == null)
            {
                return score;
            }

            // Title 权重高
            score += CountOverlap(
                messageWords,
                Tokenize(content.title)) * 4;

            // Topic ID 权重高
            score += CountOverlap(
                messageWords,
                TokenizeTopicId(content.topicId)) * 4;

            // Theme
            score += CountOverlap(
                messageWords,
                Tokenize(
                    content.theme.ToString())) * 2;

            // Summary 普通权重
            score += CountOverlap(
                messageWords,
                Tokenize(content.summary));

            return score;
        }

        private static int CountOverlap(
    HashSet<string> first,
    HashSet<string> second)
        {
            int count = 0;

            foreach (string word in first)
            {
                if (second.Contains(word))
                {
                    count++;
                }
            }

            return count;
        }
        private static HashSet<string> TokenizeTopicId(
    string topicId)
        {
            if (string.IsNullOrWhiteSpace(topicId))
            {
                return new HashSet<string>();
            }

            string normalized =
                topicId
                    .Replace(".", " ")
                    .Replace("_", " ")
                    .Replace("-", " ");

            return Tokenize(normalized);
        }

        private static HashSet<string> Tokenize(
            string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return new HashSet<string>();
            }

            char[] separators =
            {
                ' ', ',', '.', '?', '!',
                ':', ';', '"', '\'',
                '\r', '\n', '\t',
                '(', ')', '[', ']'
            };

            return new HashSet<string>(
                text
                    .ToLowerInvariant()
                    .Split(
                        separators,
                        StringSplitOptions.RemoveEmptyEntries));
        }
    }
}