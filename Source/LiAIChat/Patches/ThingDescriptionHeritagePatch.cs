using HarmonyLib;
using LiAIChat.Heritage;
using Verse;

namespace LiAIChat.Patches
{
    [HarmonyPatch(typeof(Thing), "get_DescriptionDetailed")]
    public static class ThingDescriptionHeritagePatch
    {
        public static void Postfix(Thing __instance, ref string __result)
        {
            CivilizationHeritageWork work =
                CivilizationHeritageService.GetWork(__instance);

            if (work == null || work.Pending ||
                string.IsNullOrWhiteSpace(work.Description))
            {
                return;
            }

            string heading = string.IsNullOrWhiteSpace(work.Title)
                ? "文明遗产"
                : "文明遗产：《" + work.Title + "》";

            __result = (__result ?? "") +
                "\n\n" + heading +
                "\n" + work.Description +
                "\n创作者：" + work.CreatorName;
        }
    }
}
