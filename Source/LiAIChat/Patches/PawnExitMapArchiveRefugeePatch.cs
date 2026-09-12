using HarmonyLib;
using LiAIChat.Refugees;
using RimWorld;
using Verse;

namespace LiAIChat.Patches
{
    [HarmonyPatch(typeof(Pawn), "ExitMap")]
    public static class PawnExitMapArchiveRefugeePatch
    {
        public static void Postfix(
            Pawn __instance)
        {
            Pawn pawn =
                __instance;

            if (pawn == null)
            {
                return;
            }

            if (pawn.Spawned)
            {
                return;
            }

            if (pawn.Dead)
            {
                return;
            }


            ArchiveRefugeeGroupState group =
                ArchiveRefugeeGroupManager
                    .GetGroupForPawn(pawn);

            if (group == null)
            {
                return;
            }

            if (!group.Active)
            {
                return;
            }

            if (!group.DepartureStarted)
            {
                return;
            }

            if (group.FailureSignalSent)
            {
                return;
            }


            int pawnId =
                pawn.thingIDNumber;

            if (!group.ExitedPawnIds
                    .Contains(pawnId))
            {
                group.ExitedPawnIds.Add(
                    pawnId);

                Log.Message(
                    "[Li AI Chat] Refugee exited map: " +
                    pawn.LabelShort +
                    ", group=" +
                    group.GroupId +
                    ", exited=" +
                    group.ExitedPawnIds.Count +
                    "/" +
                    group.MemberPawnIds.Count);
            }


            TryCompleteGroup(group);
        }


        private static void TryCompleteGroup(
            ArchiveRefugeeGroupState group)
        {
            if (group == null)
            {
                return;
            }

            if (group.ExitSignalSent ||
                group.FailureSignalSent)
            {
                return;
            }


            for (int i = 0;
                 i < group.MemberPawnIds.Count;
                 i++)
            {
                int memberId =
                    group.MemberPawnIds[i];

                if (!group.ExitedPawnIds
                        .Contains(memberId))
                {
                    return;
                }
            }


            group.ExitSignalSent =
                true;

            group.Completed =
                true;

            group.Active =
                false;


            if (string.IsNullOrEmpty(
                group.ExitSignal))
            {
                Log.Warning(
                    "[Li AI Chat] Refugee group completed departure, " +
                    "but ExitSignal was empty.");

                return;
            }


            Find.SignalManager.SendSignal(
                new Signal(
                    group.ExitSignal));


            Log.Message(
                "[Li AI Chat] All refugees exited map: " +
                group.GroupId +
                ", signal=" +
                group.ExitSignal);
        }
    }
}