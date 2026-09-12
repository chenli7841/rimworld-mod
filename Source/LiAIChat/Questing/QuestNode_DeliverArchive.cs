using RimWorld.QuestGen;

namespace LiAIChat.Questing
{
    public class QuestNode_DeliverArchive : QuestNode
    {
        public SlateRef<string> inSignal;

        protected override void RunInt()
        {
            QuestPart_DeliverArchive part =
                new QuestPart_DeliverArchive();

            part.inSignal =
                QuestGenUtility.HardcodedSignalWithQuestID(
                    inSignal.GetValue(QuestGen.slate));

            QuestGen.quest.AddPart(part);
        }

        protected override bool TestRunInt(
            Slate slate)
        {
            return true;
        }
    }
}