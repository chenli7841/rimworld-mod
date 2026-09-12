using System.Collections.Generic;
using System.Threading.Tasks;
using LiAIChat.Models;

namespace LiAIChat.AI
{
    public interface IWorldviewDirector
    {
        Task<WorldviewChange> AnalyzeAsync(
            PawnContext pawn,
            WorldviewState worldview,
            IReadOnlyList<ChatMessage> recentConversation);
    }
}