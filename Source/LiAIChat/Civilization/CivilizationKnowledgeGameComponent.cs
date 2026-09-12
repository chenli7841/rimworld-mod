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
    }
}