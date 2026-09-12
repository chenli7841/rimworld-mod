namespace LiAIChat.Background
{
    public class IntellectualExchangeSnapshot
    {
        /// <summary>
        /// GameId重要性，避免：Save A -> 发送 AI request -> 玩家回主菜单 -> Load Save B -> 旧 request 返回 -> 把 Save A 的结果写进 Save B
        /// </summary>
        public int GameId;

        public PawnAISnapshot PawnA;

        public PawnAISnapshot PawnB;
    }
}