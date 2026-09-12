using LiAIChat.Background;
using LiAIChat.Models;
using System.Threading.Tasks;

namespace LiAIChat.AI
{
    public interface IIntellectualExchangeDirector
    {
        Task<IntellectualExchangeResult> GenerateAsync(PawnAISnapshot pawnA, PawnAISnapshot pawnB);
    }
}