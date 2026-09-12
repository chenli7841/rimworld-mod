using HarmonyLib;
using LiAIChat.Models;
using LiAIChat.State;
using RimWorld;
using Verse;

namespace LiAIChat.Archive
{
    [HarmonyPatch(typeof(Pawn), "Kill")]
    public static class Pawn_Kill_ArchiveScholar_Patch
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

            if (!stay.Active)
            {
                return;
            }

            if (string.IsNullOrEmpty(
                stay.DeathSignal))
            {
                return;
            }

            stay.Active = false;
            stay.Completed = false;

            Find.SignalManager.SendSignal(
                new Signal(
                    stay.DeathSignal));

            Log.Message(
                "[Li AI Chat] Archive Scholar died: " +
                pawn.LabelShort +
                ", quest failure signal=" +
                stay.DeathSignal);
        }
    }
}