using Verse;

namespace LiAIChat.Refugees
{
    public class ArchiveRefugeeStayEventState
        : IExposable
    {
        public string EventType;

        public int TriggerTick;

        public bool Triggered;

        public bool Resolved;

        public void ExposeData()
        {
            Scribe_Values.Look(
                ref EventType,
                "eventType");

            Scribe_Values.Look(
                ref TriggerTick,
                "triggerTick",
                0);

            Scribe_Values.Look(
                ref Triggered,
                "triggered",
                false);

            Scribe_Values.Look(
                ref Resolved,
                "resolved",
                false);
        }
    }
}