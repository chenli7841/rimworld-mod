using LiAIChat.Background;
using LiAIChat.Dialogue;
using LiAIChat.Heritage;
using LiAIChat.Models;
using LiAIChat.Social;
using System.Collections.Generic;
using Verse;

namespace LiAIChat.Game
{
    public class LiAIChatGameComponent : GameComponent
    {
        public List<PawnAIState> PawnStates = new List<PawnAIState>();
        public List<CivilizationHeritageWork> CivilizationHeritageWorks =
            new List<CivilizationHeritageWork>();

        private int lastProactiveCheckTick = 0;
        private int lastIntellectualExchangeCheckTick = 0;

        // One in-game day. Proactive dialogue should be occasional and
        // should not repeatedly scan every colonist while nothing changed.
        private const int ProactiveCheckIntervalTicks = 60000;

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

            Scribe_Collections.Look(
                ref CivilizationHeritageWorks,
                "civilizationHeritageWorks",
                LookMode.Deep);

            if (Scribe.mode ==
                LoadSaveMode.PostLoadInit)
            {
                if (PawnStates == null)
                {
                    PawnStates =
                        new List<PawnAIState>();
                }

                if (CivilizationHeritageWorks == null)
                {
                    CivilizationHeritageWorks =
                        new List<CivilizationHeritageWork>();
                }

                Log.Message(
                    "[Li AI Chat] Loaded " +
                    PawnStates.Count +
                    " pawn AI states.");
            }
        }

        public CivilizationHeritageWork GetCivilizationHeritageWork(
            int thingId)
        {
            if (CivilizationHeritageWorks == null || thingId < 0)
            {
                return null;
            }

            return CivilizationHeritageWorks.Find(
                work => work != null && work.ThingId == thingId);
        }

        public override void GameComponentTick()
        {
            base.GameComponentTick();

            MainThreadActionQueue.Process();
            if (Find.TickManager == null)
                return;

            int currentTick =
                Find.TickManager.TicksGame;

            // 每游戏日检查一次，避免频繁扫描所有殖民者。
            if (currentTick - lastProactiveCheckTick <
                ProactiveCheckIntervalTicks)
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
