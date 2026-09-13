using HarmonyLib;
using Verse;
using LiAIChat.Events;

namespace LiAIChat.Patches
{
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.Kill))]
    public static class PawnDeathPatch
    {
        public static void Postfix(
            Pawn __instance, DamageInfo? dinfo)
        {
            if (__instance == null)
                return;

            if (!__instance.RaceProps.Humanlike)
                return;

            PawnDeathEventHandler.HandleDeath(
                __instance, dinfo);
        }
    }
}