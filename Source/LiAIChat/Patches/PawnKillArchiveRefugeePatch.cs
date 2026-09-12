using HarmonyLib;
using LiAIChat.Refugees;
using RimWorld;
using Verse;

namespace LiAIChat.Patches
{
    [HarmonyPatch(typeof(Pawn), "Kill")]
    public static class PawnKillArchiveRefugeePatch
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

            if (group.Completed)
            {
                return;
            }

            if (group.FailureSignalSent)
            {
                return;
            }


            group.FailureSignalSent =
                true;

            group.Active =
                false;


            if (string.IsNullOrEmpty(
                group.FailureSignal))
            {
                Log.Warning(
                    "[Li AI Chat] Refugee died but FailureSignal was empty.");

                return;
            }


            Find.SignalManager.SendSignal(
                new Signal(
                    group.FailureSignal,
                    pawn.Named("SUBJECT")));


            Log.Message(
                "[Li AI Chat] Archive Refugee died: " +
                pawn.LabelShort +
                ", group=" +
                group.GroupId +
                ", signal=" +
                group.FailureSignal);
        }
    }
}