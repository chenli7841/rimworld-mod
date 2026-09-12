using Verse;

namespace LiAIChat.Archive
{
    public class ArchiveScholarStayState : IExposable
    {
        public bool Active;

        public int StartTick;

        public int EndTick;

        public bool Completed;

        public string DeathSignal;
        public bool ArrivalDialogueTriggered;
        public bool FarewellDialogueTriggered;
        public int MeaningfulConversationCount;

        public float HospitalityImpression;

        public bool ArchiveGiftDecided;

        public bool WillGiftArchive;
        public string ExitSignal;
        public bool ExitSignalSent;
        public void ExposeData()
        {
            Scribe_Values.Look(
                ref Active,
                "active",
                false);

            Scribe_Values.Look(
                ref StartTick,
                "startTick",
                0);

            Scribe_Values.Look(
                ref EndTick,
                "endTick",
                0);

            Scribe_Values.Look(
                ref Completed,
                "completed",
                false);

            Scribe_Values.Look(
                ref DeathSignal,
                "deathSignal");

            Scribe_Values.Look(ref ArrivalDialogueTriggered, "arrivalDialogueTriggered", false);
            Scribe_Values.Look(ref FarewellDialogueTriggered, "farewellDialogueTriggered", false);
            Scribe_Values.Look(
    ref MeaningfulConversationCount,
    "meaningfulConversationCount",
    0);

            Scribe_Values.Look(
                ref HospitalityImpression,
                "hospitalityImpression",
                0f);

            Scribe_Values.Look(
                ref ArchiveGiftDecided,
                "archiveGiftDecided",
                false);

            Scribe_Values.Look(
                ref WillGiftArchive,
                "willGiftArchive",
                false);
            Scribe_Values.Look(
                ref ExitSignal,
                "exitSignal");
            Scribe_Values.Look(
                ref ExitSignalSent,
                "exitSignalSent",
                false);
        }
    }
}