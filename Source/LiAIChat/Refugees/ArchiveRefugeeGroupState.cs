using System.Collections.Generic;
using Verse;

namespace LiAIChat.Refugees
{
    public class ArchiveRefugeeGroupState : IExposable
    {
        public string GroupId;

        public List<int> MemberPawnIds =
            new List<int>();

        public int ArchiveThingId;

        public int CarrierPawnId;

        public bool Active;

        public bool Completed;

        public int LeaderPawnId;

        public ArchiveRefugeeStayEventState StayEvent;

        // ---------------------------------------------------------
        // Refugee situation
        // ---------------------------------------------------------

        public string SituationType;

        public string TravelHardship;

        public string ResourceCondition;

        public int YoungestAge;

        public int OldestAge;

        public int MeaningfulConversationCount;

        public float HospitalityImpression;

        public int StayStartTick;

        public int PlannedStayEndTick;

        public bool ArchiveDispositionDecided;

        public bool WillLeaveArchive;

        public bool DepartureStarted;

        public string ExitSignal;

        public string FailureSignal;

        public bool ExitSignalSent;

        public bool FailureSignalSent;

        public List<int> ExitedPawnIds =
            new List<int>();


        public void ExposeData()
        {
            Scribe_Values.Look(
                ref GroupId,
                "groupId");

            Scribe_Collections.Look(
                ref MemberPawnIds,
                "memberPawnIds",
                LookMode.Value);

            Scribe_Values.Look(
                ref ArchiveThingId,
                "archiveThingId",
                0);

            Scribe_Values.Look(
                ref CarrierPawnId,
                "carrierPawnId",
                0);

            Scribe_Values.Look(
                ref Active,
                "active",
                false);

            Scribe_Values.Look(
                ref Completed,
                "completed",
                false);


            Scribe_Values.Look(
                ref SituationType,
                "situationType");

            Scribe_Values.Look(
                ref TravelHardship,
                "travelHardship");

            Scribe_Values.Look(
                ref ResourceCondition,
                "resourceCondition");

            Scribe_Values.Look(
                ref YoungestAge,
                "youngestAge",
                0);

            Scribe_Values.Look(
                ref OldestAge,
                "oldestAge",
                0);

            Scribe_Values.Look(
                ref LeaderPawnId,
                "leaderPawnId",
                0);
            Scribe_Values.Look(
                ref MeaningfulConversationCount,
                "meaningfulConversationCount",
                0);

            Scribe_Values.Look(
                ref HospitalityImpression,
                "hospitalityImpression",
                0f);
            Scribe_Values.Look(
                ref StayStartTick,
                "stayStartTick",
                0);

            Scribe_Values.Look(
                ref PlannedStayEndTick,
                "plannedStayEndTick",
                0);

            Scribe_Deep.Look(
                ref StayEvent,
                "stayEvent");

            Scribe_Values.Look(
                ref ArchiveDispositionDecided,
                "archiveDispositionDecided",
                false);

            Scribe_Values.Look(
                ref WillLeaveArchive,
                "willLeaveArchive",
                false);

            Scribe_Values.Look(
    ref DepartureStarted,
    "departureStarted",
    false);

            Scribe_Values.Look(
                ref ExitSignal,
                "exitSignal");

            Scribe_Values.Look(
                ref FailureSignal,
                "failureSignal");

            Scribe_Values.Look(
                ref ExitSignalSent,
                "exitSignalSent",
                false);

            Scribe_Values.Look(
                ref FailureSignalSent,
                "failureSignalSent",
                false);

            Scribe_Collections.Look(
                ref ExitedPawnIds,
                "exitedPawnIds",
                LookMode.Value);

            if (ExitedPawnIds == null)
            {
                ExitedPawnIds =
                    new List<int>();
            }

            if (MemberPawnIds == null)
            {
                MemberPawnIds =
                    new List<int>();
            }
        }
    }
}