using LiAIChat.Background;
using System.Threading.Tasks;

namespace LiAIChat.Archive
{
    public interface IArchiveReflectionGenerator
    {
        Task<string> GenerateAsync(
            PawnAISnapshot pawnSnapshot,
            EarthTextDef earthText,
            string sourceDescription);
    }
}