using System.Threading.Tasks;
using LiAIChat.Models;

namespace LiAIChat.AI
{
    /// <summary>
    /// 这个 AI 的职责非常轻：根据 Pawn 当前状态，写一句自然的“我想跟你谈谈”。
    /// 不是正式完整聊天。
    /// </summary>
    public interface IProactiveDialogueDirector
    {
        Task<string> GenerateAsync(
    PawnContext context,
    PawnAIState state,
    string specialInstruction);
    }
}