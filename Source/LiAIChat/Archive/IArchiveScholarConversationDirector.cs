using System.Threading.Tasks;
using LiAIChat.Models;
using LiAIChat.State;

namespace LiAIChat.Archive
{
    public interface IArchiveScholarConversationDirector
    {
        Task<ArchiveScholarConversationImpact>
            AnalyzeAsync(
                PawnContext context,
                PawnAIState state,
                string playerMessage,
                string scholarResponse);
    }
}