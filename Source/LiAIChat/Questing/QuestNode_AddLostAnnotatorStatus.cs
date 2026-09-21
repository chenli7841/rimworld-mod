using RimWorld.QuestGen;

namespace LiAIChat.Questing
{
    public class QuestNode_AddLostAnnotatorStatus : QuestNode
    {
        protected override bool TestRunInt(Slate slate) { return true; }
        protected override void RunInt() { QuestGen.quest.AddPart(new QuestPart_LostAnnotatorStatus()); }
    }
}
