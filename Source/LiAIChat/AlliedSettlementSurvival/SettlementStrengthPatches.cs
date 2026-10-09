using System.Collections.Generic;
using HarmonyLib;
using RimWorld.Planet;
using Verse;

namespace LiAIChat.AlliedSettlementSurvival
{
    [HarmonyPatch(typeof(Settlement), nameof(Settlement.GetInspectString))]
    public static class SettlementStrengthInspectPatch
    {
        public static void Postfix(Settlement __instance, ref string __result)
        {
            AlliedSettlementWorldComponent component = AlliedSettlementWorldComponent.Current;
            AlliedSettlementState state = component?.Get(__instance);
            if (state == null) return;
            string status = state.strength <= 0f ? "LiASS_Pending" : component.Eligible(__instance) ? "LiASS_Active" : "LiASS_Frozen";
            __result += "\n" + "LiASS_Strength".Translate(state.strength.ToString("0.0")) + " — " + status.Translate();
        }
    }

    [HarmonyPatch(typeof(Settlement), nameof(Settlement.GetGizmos))]
    public static class SettlementStrengthDebugPatch
    {
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Settlement __instance)
        {
            foreach (Gizmo gizmo in __result) yield return gizmo;
            AlliedSettlementState state = AlliedSettlementWorldComponent.Current?.Get(__instance);
            if (!Prefs.DevMode || state == null) yield break;
            yield return new Command_Action { defaultLabel = "DEV: Strength -10", action = () => state.strength = SettlementStrengthPolicy.Clamp(state.strength - 10f) };
            yield return new Command_Action { defaultLabel = "DEV: Strength +10", action = () => state.strength = SettlementStrengthPolicy.Clamp(state.strength + 10f) };
        }
    }
}
