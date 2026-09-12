using HarmonyLib;
using LiAIChat.Archive;
using LiAIChat.Models;
using LiAIChat.State;
using RimWorld;
using Verse;

namespace LiAIChat.Patches
{
    [HarmonyPatch(typeof(Pawn), "Kill")]
    public static class PawnKillArchiveScholarPatch
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
                PawnAIStateManager.GetState(
                    pawn);

            if (state == null ||
                state.ScholarStay == null)
            {
                return;
            }

            ArchiveScholarStayState stay =
                state.ScholarStay;

            if (!stay.Active)
            {
                return;
            }

            if (stay.Completed)
            {
                return;
            }

            if (string.IsNullOrEmpty(
                stay.DeathSignal))
            {
                return;
            }

            // 防止重复触发。
            stay.Active = false;

            string deathSignal =
                stay.DeathSignal;

            Find.SignalManager.SendSignal(
                new Signal(
                    deathSignal));

            Log.Message(
                "[Li AI Chat] Archive Scholar died during stay: " +
                pawn.LabelShort +
                ", sent signal=" +
                deathSignal);
        }
    }
}