using Verse;

namespace LiAIChat.Refugees
{
    public static class ArchiveRefugeeStayEventGenerator
    {
        public static ArchiveRefugeeStayEventState Generate(
            int startTick,
            int endTick)
        {
            if (endTick <= startTick)
            {
                return null;
            }

            ArchiveRefugeeStayEventState stayEvent =
                new ArchiveRefugeeStayEventState();

            stayEvent.EventType =
                ChooseEventType();

            int duration =
                endTick - startTick;

            float triggerProgress =
                Rand.Range(0.30f, 0.70f);

            stayEvent.TriggerTick =
                startTick +
                (int)(duration * triggerProgress);

            stayEvent.Triggered = false;
            stayEvent.Resolved = false;

            return stayEvent;
        }


        private static string ChooseEventType()
        {
            int roll =
                Rand.RangeInclusive(0, 2);

            switch (roll)
            {
                case 0:
                    return "ArchiveAnxiety";

                case 1:
                    return "FutureUncertainty";

                default:
                    return "GroupTension";
            }
        }
    }
}