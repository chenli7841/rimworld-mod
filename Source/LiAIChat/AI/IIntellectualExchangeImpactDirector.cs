using LiAIChat.Background;
using LiAIChat.Models;
using System.Threading.Tasks;

namespace LiAIChat.AI
{
    public interface IIntellectualExchangeImpactDirector
    {
        Task<IntellectualExchangeImpactResult> AnalyzeAsync(PawnAISnapshot pawnA, PawnAISnapshot pawnB, IntellectualExchangeResult exchange);
    }
}