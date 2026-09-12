using HarmonyLib;
using Verse;
using LiAIChat.Archive;

namespace LiAIChat.Patches
{
    [HarmonyPatch(
        typeof(Map),
        nameof(Map.FinalizeInit))]
    public static class MapFinalizeInitPatch
    {
        [HarmonyPostfix]
        public static void Postfix(
            Map __instance)
        {
            AncientRuinArchiveSpawner
                .TrySpawnArchive(__instance);
        }
    }
}