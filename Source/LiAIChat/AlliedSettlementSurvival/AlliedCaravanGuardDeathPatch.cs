using HarmonyLib;
using RimWorld;
using Verse;

namespace LiAIChat.AlliedSettlementSurvival
{
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.Kill))]
    public static class AlliedCaravanGuardDeathPatch
    {
        public static void Prefix(Pawn __instance, out AlliedCaravanGuardRecord __state)
        {
            __state = null;
            if (__instance?.lord?.LordJob is LordJob_TradeWithColony)
                __state = AlliedSettlementWorldComponent.Current?.FindActiveCaravanGuard(__instance);
        }

        public static void Postfix(Pawn __instance, AlliedCaravanGuardRecord __state)
        {
            if (__instance != null && __instance.Dead && __state != null)
                AlliedSettlementWorldComponent.Current?.RecordCaravanGuardDeath(__state);
        }
    }

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.ExitMap))]
    public static class AlliedCaravanGuardExitPatch
    {
        public static void Postfix(Pawn __instance)
        {
            if (__instance != null && !__instance.Dead)
                AlliedSettlementWorldComponent.Current?.ForgetCaravanGuard(__instance);
        }
    }
}
