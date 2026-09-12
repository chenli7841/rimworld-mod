using Verse;

namespace LiAIChat.Refugees
{
    public static class ArchiveRefugeeStayEventUtility
    {
        public static bool UpdateTriggerState(
            ArchiveRefugeeGroupState group)
        {
            if (group == null ||
                group.StayEvent == null)
            {
                return false;
            }

            ArchiveRefugeeStayEventState stayEvent =
                group.StayEvent;

            if (stayEvent.Triggered)
            {
                return true;
            }

            int currentTick =
                Find.TickManager.TicksGame;

            if (currentTick <
                stayEvent.TriggerTick)
            {
                return false;
            }

            stayEvent.Triggered = true;

            Log.Message(
                "[Li AI Chat] Refugee stay event triggered: " +
                stayEvent.EventType +
                ", group=" +
                group.GroupId);

            return true;
        }
    }
}