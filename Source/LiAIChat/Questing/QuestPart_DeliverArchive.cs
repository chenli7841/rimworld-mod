using RimWorld;
using Verse;
using LiAIChat.Archive;

namespace LiAIChat.Questing
{
    public class QuestPart_DeliverArchive : QuestPart
    {
        public string inSignal;

        public override void Notify_QuestSignalReceived(
            Signal signal)
        {
            base.Notify_QuestSignalReceived(signal);

            if (signal.tag != inSignal)
            {
                return;
            }

            Map map = Find.AnyPlayerHomeMap;

            if (map == null)
            {
                Log.Warning(
                    "[Li AI Chat] Cannot deliver archive reward: "
                    + "no player home map.");

                return;
            }

            Thing archive =
                AncientArchiveFactory.Create(
                    ArchiveSourceType.QuestReward,
                    "Recovered as a quest reward.");

            IntVec3 dropCell =
                DropCellFinder.TradeDropSpot(map);

            GenPlace.TryPlaceThing(
                archive,
                dropCell,
                map,
                ThingPlaceMode.Near);

            Messages.Message(
                "An Ancient Earth Archive has been delivered.",
                archive,
                MessageTypeDefOf.PositiveEvent);

            Log.Message(
                "[Li AI Chat] Quest reward archive delivered.");
        }
    }
}