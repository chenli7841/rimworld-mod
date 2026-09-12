using System.Collections.Generic;
using System.Threading.Tasks;
using LiAIChat.Models;

namespace LiAIChat.AI
{
    public interface IChatProvider
    {
        Task<string> SendAsync(
            PawnContext pawn,
            PawnAIState state,
            WorldviewState worldview,
            MeaningState meaning,
            KnowledgeState knowledge,
            LifeGoal lifeGoal, 
            IReadOnlyList<PawnLifeEvent> recentLifeEvents,
            IReadOnlyList<PawnIntellectualExchange> recentIntellectualExchanges,
            string conversationSummary,
            IReadOnlyList<PawnMemory> memories,
            IReadOnlyList<ChatMessage> history,
            string playerMessage);
    }
}