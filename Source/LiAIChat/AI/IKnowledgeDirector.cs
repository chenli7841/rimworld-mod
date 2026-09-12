using System.Collections.Generic;
using System.Threading.Tasks;
using LiAIChat.Models;

namespace LiAIChat.AI
{
    public interface IKnowledgeDirector
    {
        Task<List<KnowledgeAcquisition>> AnalyzeAsync(
            PawnContext pawn,
            KnowledgeState knowledge,
            IReadOnlyList<ChatMessage> recentConversation);
    }
}