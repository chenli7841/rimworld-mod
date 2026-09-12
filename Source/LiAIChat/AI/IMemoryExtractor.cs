using System.Collections.Generic;
using System.Threading.Tasks;
using LiAIChat.Models;

namespace LiAIChat.AI
{
    public interface IMemoryExtractor
    {
        Task<List<PawnMemory>> ExtractAsync(
            PawnContext pawn,
            IReadOnlyList<ChatMessage> messages);
    }
}