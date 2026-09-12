using System.Collections.Generic;
using System.Threading.Tasks;
using LiAIChat.Models;

namespace LiAIChat.AI
{
    public interface IConversationSummarizer
    {
        Task<string> SummarizeAsync(
            PawnContext pawn,
            string existingSummary,
            IReadOnlyList<ChatMessage> messages);
    }
}