using System;
using System.Collections.Generic;
using System.Linq;
using LiAIChat.Models;

namespace LiAIChat.Memory
{
    public static class MemoryRetriever
    {
        public static List<PawnMemory> GetRelevantMemories(
            IReadOnlyList<PawnMemory> memories,
            string playerMessage,
            int maxCount = 6)
        {
            if (memories == null ||
                memories.Count == 0 ||
                string.IsNullOrWhiteSpace(playerMessage))
            {
                return new List<PawnMemory>();
            }

            HashSet<string> queryWords =
                Tokenize(playerMessage);

            var scoredMemories =
                new List<(PawnMemory Memory, float Score)>();

            foreach (PawnMemory memory in memories)
            {
                if (memory == null ||
                    string.IsNullOrWhiteSpace(memory.Text))
                {
                    continue;
                }

                HashSet<string> memoryWords =
                    Tokenize(memory.Text);

                int overlapCount = 0;

                foreach (string word in queryWords)
                {
                    if (memoryWords.Contains(word))
                    {
                        overlapCount++;
                    }
                }

                float score =
                    overlapCount * 1.0f +
                    memory.Importance * 0.5f;

                if (overlapCount > 0)
                {
                    scoredMemories.Add(
                        (memory, score));
                }
            }

            if (scoredMemories.Count == 0)
            {
                return memories
                    .Where(m => m != null)
                    .OrderByDescending(m => m.Importance)
                    .Take(3)
                    .ToList();
            }

            return scoredMemories
                .OrderByDescending(x => x.Score)
                .Take(maxCount)
                .Select(x => x.Memory)
                .ToList();
        }

        private static HashSet<string> Tokenize(
            string text)
        {
            char[] separators =
            {
                ' ',
                '\t',
                '\r',
                '\n',
                '.',
                ',',
                '!',
                '?',
                ':',
                ';',
                '"',
                '\'',
                '(',
                ')',
                '[',
                ']'
            };

            string[] words =
                text.ToLowerInvariant()
                    .Split(
                        separators,
                        StringSplitOptions.RemoveEmptyEntries);

            return new HashSet<string>(
                words.Where(word => word.Length >= 3));
        }
    }
}