using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace LiAIChat.Questing
{
    public class QuestNode_Root_LostEarthArchive : QuestNode
    {
        protected override bool TestRunInt(Slate slate)
        {
            return Find.AnyPlayerHomeMap != null;
        }

        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;

            Map map =
                Find.AnyPlayerHomeMap;

            if (map == null)
            {
                Log.Error(
                    "[Li AI Chat] Cannot generate Lost Earth Archive quest: " +
                    "no player home map.");

                return;
            }

            slate.Set(
                "map",
                map);

            Log.Message(
                "[Li AI Chat] Lost Earth Archive quest generated on map: " +
                map);
        }
    }
}