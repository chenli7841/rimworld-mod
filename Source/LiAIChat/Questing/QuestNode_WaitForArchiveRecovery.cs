using RimWorld.QuestGen;
using RimWorld.Planet;

namespace LiAIChat.Questing
{
    public class QuestNode_WaitForArchiveRecovery : QuestNode
    {
        public SlateRef<Site> site;

        protected override bool TestRunInt(Slate slate) => true;

        protected override void RunInt()
        {
            QuestGen.quest.AddPart(new QuestPart_WaitForArchiveRecovery
            {
                site = site.GetValue(QuestGen.slate),
                inSignalEnable = QuestGen.slate.Get<string>("inSignal")
            });
        }
    }
}
