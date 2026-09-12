using RimWorld.QuestGen;
using Verse;

namespace LiAIChat.Questing
{
    public class QuestNode_Root_ArchiveTest
        : QuestNode
    {
        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;

            Map map = Find.AnyPlayerHomeMap;

            if (map == null)
            {
                Log.Error(
                    "[Li AI Chat] Cannot generate Archive quest: " +
                    "no player home map.");

                return;
            }

            slate.Set(
                "map",
                map);
        }

        protected override bool TestRunInt(
            Slate slate)
        {
            return true;
        }
    }
}