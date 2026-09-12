using LiAIChat.Background;
using LiAIChat.Dialogue;
using LiAIChat.Models;
using LiAIChat.Social;
using System.Collections.Generic;
using Verse;

namespace LiAIChat.Game
{
    public class LiAIChatGameComponent : GameComponent
    {
        public List<PawnAIState> PawnStates = new List<PawnAIState>();

        private int lastProactiveCheckTick = 0;
        private int lastIntellectualExchangeCheckTick = 0;

        public int RuntimeGameId { get; private set; }
        private static int nextRuntimeGameId = 1;

        public LiAIChatGameComponent(Verse.Game game) : base()
        {
            RuntimeGameId = nextRuntimeGameId++;
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Collections.Look(
                ref PawnStates,
                "pawnStates",
                LookMode.Deep);

            if (Scribe.mode ==
                LoadSaveMode.PostLoadInit)
            {
                if (PawnStates == null)
                {
                    PawnStates =
                        new List<PawnAIState>();
                }

                Log.Message(
                    "[Li AI Chat] Loaded " +
                    PawnStates.Count +
                    " pawn AI states.");
            }
        }

        public override void GameComponentTick()
        {
            base.GameComponentTick();

            MainThreadActionQueue.Process();
            if (Find.TickManager == null)
                return;

            int currentTick =
                Find.TickManager.TicksGame;

            // 每 2500 ticks 检查一次。2500 ticks ≈ 1 in-game hour
            if (currentTick - lastProactiveCheckTick < 2500)
            {
                return;
            }

            lastProactiveCheckTick =
                currentTick;

            ProactiveDialogueManager.CheckForProactiveDialogue();

            // 每游戏日最多尝试一次。只是：尝试。Director 可以返回：ShouldExchange | false
            if (currentTick - lastIntellectualExchangeCheckTick >= 240000)
            {
                lastIntellectualExchangeCheckTick = currentTick;
                IntellectualExchangeManager.TryCreateExchange();
            }
        }
    }
}