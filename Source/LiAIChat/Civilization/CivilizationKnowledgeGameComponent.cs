using RimWorld;
using System.Collections.Generic;
using Verse;

namespace LiAIChat.Civilization
{
    public class CivilizationKnowledgeGameComponent
        : GameComponent
    {
        private List<CivilizationKnowledgeState>
            states =
                new List<CivilizationKnowledgeState>();

        private int nextStatusCheckTick = -1;

        public CivilizationKnowledgeGameComponent(
            Verse.Game game)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Collections.Look(
                ref states,
                "civilizationKnowledgeStates",
                LookMode.Deep);

            if (states == null)
            {
                states =
                    new List<
                        CivilizationKnowledgeState>();
            }
        }

        public List<CivilizationKnowledgeState>
            States
        {
            get
            {
                return states;
            }
        }

        public override void GameComponentTick()
        {
            base.GameComponentTick();

            if (Find.TickManager == null)
            {
                return;
            }

            int currentTick =
                Find.TickManager.TicksGame;

            if (nextStatusCheckTick < 0)
            {
                nextStatusCheckTick =
                    currentTick + GenDate.TicksPerDay;
                return;
            }

            if (currentTick <
                nextStatusCheckTick)
            {
                return;
            }

            nextStatusCheckTick =
                currentTick + GenDate.TicksPerDay;

            CivilizationKnowledgeEvaluator
                .UpdateAllMissingStates();

            CivilizationKnowledgeEvaluator
                .UpdateAllUnstableStates();
        }
    }
}