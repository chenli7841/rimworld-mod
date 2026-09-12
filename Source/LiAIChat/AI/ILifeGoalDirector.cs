using System.Threading.Tasks;
using LiAIChat.Models;

namespace LiAIChat.AI
{
    public interface ILifeGoalDirector
    {
        /// <summary>
        /// 和其他Director class的AnalyzeAsync相比，这里直接传递PawnAIState，因为几乎需要看其中的所有属性
        /// </summary>
        Task<LifeGoalProposal> AnalyzeAsync(
            PawnContext pawn,
            PawnAIState state);
    }
}