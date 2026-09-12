using HarmonyLib;
using LiAIChat.Archive;
using LiAIChat.Models;
using LiAIChat.State;
using RimWorld;
using Verse;

namespace LiAIChat.Patches
{
    [HarmonyPatch(typeof(Pawn), "ExitMap")]
    public static class PawnExitMapArchiveScholarPatch
    {
        public static void Postfix(
            Pawn __instance)
        {
            Pawn pawn = __instance;

            if (pawn == null)
            {
                return;
            }

            PawnAIState state =
                PawnAIStateManager.GetState(pawn);

            if (state == null ||
                state.ScholarStay == null)
            {
                return;
            }

            ArchiveScholarStayState stay =
                state.ScholarStay;

            // 必须已经完成约定的停留。
            // 否则某种提前离图不能算 Quest Success。
            if (!stay.Completed)
            {
                return;
            }

            // ExitMap 正常完成之后，Pawn 应该已经不在地图上。
            if (pawn.Spawned)
            {
                return;
            }

            if (pawn.Dead)
            {
                return;
            }

            if (stay.ExitSignalSent)
            {
                return;
            }

            if (string.IsNullOrEmpty(
                stay.ExitSignal))
            {
                return;
            }

            stay.ExitSignalSent = true;

            string exitSignal =
                stay.ExitSignal;

            Find.SignalManager.SendSignal(
                new Signal(
                    exitSignal,
                    pawn.Named("SUBJECT")));

            Log.Message(
                "[Li AI Chat] Archive Scholar exited map: " +
                pawn.LabelShort +
                ", signal=" +
                exitSignal);
        }
    }
}