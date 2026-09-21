using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace LiAIChat.Questing
{
    public class QuestNode_PrepareMultiFactionIncursion : QuestNode
    {
        private static bool HasActiveIncursion()
        {
            return Find.QuestManager.QuestsListForReading.Any(quest => quest.root != null &&
                quest.root.defName == "LiAIChat_MultiFactionIncursion" &&
                (quest.State == QuestState.NotYetAccepted || quest.State == QuestState.Ongoing));
        }

        private static List<Faction> FindHumanRaiders(float points)
        {
            if (Find.FactionManager == null || Faction.OfMechanoids == null)
                return new List<Faction>();

            return Find.FactionManager.AllFactionsVisible
                .Where(faction => faction != null && faction != Faction.OfPlayer &&
                    faction != Faction.OfMechanoids && !faction.def.hidden &&
                    faction.def.humanlikeFaction && faction.HostileTo(Faction.OfPlayer) &&
                    PawnGroupMakerUtility.CanGenerateAnyNormalGroup(faction, points))
                .InRandomOrder().Take(2).ToList();
        }

        protected override bool TestRunInt(Slate slate)
        {
            float points = QuestGen.quest != null ? QuestGen.quest.points * 0.38f : 200f;
            return !HasActiveIncursion() && slate.Get<Map>("map") != null &&
                Faction.OfMechanoids != null &&
                PawnGroupMakerUtility.CanGenerateAnyNormalGroup(Faction.OfMechanoids, points) &&
                FindHumanRaiders(points).Count == 2;
        }

        protected override void RunInt()
        {
            Map map = QuestGen.slate.Get<Map>("map");
            float points = QuestGen.quest.points * 0.38f;
            List<Faction> humans = FindHumanRaiders(points);
            if (map == null || humans.Count != 2)
                throw new System.InvalidOperationException("Multi-faction incursion requires two hostile human factions.");

            QuestGen.quest.AddPart(new QuestPart_MultiFactionIncursion
            {
                map = map,
                humanFactionA = humans[0],
                humanFactionB = humans[1],
                mechanoidFaction = Faction.OfMechanoids,
                pointsPerFaction = points,
                inSignalEnable = QuestGen.slate.Get<string>("inSignal")
            });
        }
    }
}
