using System;
using System.Collections.Generic;
using Verse;

namespace LiAIChat.Refugees
{
    public static class ArchiveRefugeeGroupManager
    {
        private static ArchiveRefugeeGroupComponent Component
        {
            get
            {
                if (Current.Game == null)
                {
                    return null;
                }

                return
                    Current.Game
                        .GetComponent<
                            ArchiveRefugeeGroupComponent>();
            }
        }

        public static ArchiveRefugeeGroupState CreateGroup(
            IEnumerable<Pawn> pawns,
            Thing archive,
            Pawn carrier)
        {
            ArchiveRefugeeGroupComponent component =
                Component;

            if (component == null || pawns == null)
            {
                return null;
            }

            List<Pawn> pawnList = new List<Pawn>();

            foreach (Pawn pawn in pawns)
            {
                if (pawn != null)
                {
                    pawnList.Add(pawn);
                }
            }


            ArchiveRefugeeGroupState group =
                new ArchiveRefugeeGroupState();

            group.GroupId =
                "archive_refugees_" +
                Guid.NewGuid().ToString("N");

            group.Active = true;
            group.Completed = false;
            int currentTick = Find.TickManager.TicksGame;

            group.StayStartTick =
                currentTick;

            group.PlannedStayEndTick =
                currentTick + ArchiveRefugeeConstants
        .DefaultStayDurationTicks;
            group.StayEvent =
                ArchiveRefugeeStayEventGenerator.Generate(
                    group.StayStartTick,
                    group.PlannedStayEndTick);

            foreach (Pawn pawn in pawnList)
            {
                group.MemberPawnIds.Add(
                    pawn.thingIDNumber);
            }


            if (archive != null)
            {
                group.ArchiveThingId =
                    archive.thingIDNumber;
            }


            if (carrier != null)
            {
                group.CarrierPawnId =
                    carrier.thingIDNumber;
            }

            Pawn leader = ArchiveRefugeeLeaderSelector.SelectLeader(pawnList);

            if (leader != null)
            {
                group.LeaderPawnId = leader.thingIDNumber;

                Log.Message(
                    "[Li AI Chat] Refugee group leader: " +
                    leader.LabelShort +
                    ", carrier=" +
                    carrier.LabelShort +
                    ", samePerson=" +
                    (leader == carrier));
            }


            ArchiveRefugeeSituationGenerator.Generate(
                group,
                pawnList);


            component.Groups.Add(group);

            Log.Message(
                "[Li AI Chat] Archive refugee group registered: " +
                group.GroupId +
                ", members=" +
                group.MemberPawnIds.Count +
                ", situation=" +
                group.SituationType +
                ", hardship=" +
                group.TravelHardship +
                ", resources=" +
                group.ResourceCondition +
                ", ages=" +
                group.YoungestAge +
                "-" +
                group.OldestAge);

            return group;
        }

        public static ArchiveRefugeeGroupState GetGroup(
            string groupId)
        {
            ArchiveRefugeeGroupComponent component =
                Component;

            if (component == null ||
                string.IsNullOrEmpty(groupId))
            {
                return null;
            }

            for (int i = 0;
                 i < component.Groups.Count;
                 i++)
            {
                ArchiveRefugeeGroupState group =
                    component.Groups[i];

                if (group != null &&
                    group.GroupId == groupId)
                {
                    return group;
                }
            }

            return null;
        }

        public static ArchiveRefugeeGroupState
            GetGroupForPawn(Pawn pawn)
        {
            ArchiveRefugeeGroupComponent component =
                Component;

            if (component == null || pawn == null)
            {
                return null;
            }

            int pawnId =
                pawn.thingIDNumber;

            for (int i = 0;
                 i < component.Groups.Count;
                 i++)
            {
                ArchiveRefugeeGroupState group =
                    component.Groups[i];

                if (group == null)
                {
                    continue;
                }

                if (!group.Active)
                {
                    continue;
                }

                if (group.MemberPawnIds.Contains(
                    pawnId))
                {
                    return group;
                }
            }

            return null;
        }
    }
}