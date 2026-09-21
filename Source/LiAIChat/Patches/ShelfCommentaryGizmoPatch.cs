using HarmonyLib;
using LiAIChat.Commentary;
using LiAIChat.UI;
using System.Collections.Generic;
using Verse;

namespace LiAIChat.Patches
{
    [HarmonyPatch(typeof(Thing), nameof(Thing.GetGizmos))]
    public static class ShelfCommentaryGizmoPatch
    {
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Thing __instance)
        {
            foreach (Gizmo gizmo in __result) yield return gizmo;
            if (!CommentaryService.IsShelf(__instance)) yield break;
            yield return new Command_Action
            {
                defaultLabel = "浏览文献馆藏",
                defaultDesc = "查看这座书架中殖民者撰写的地球文献评注。",
                action = () => Find.WindowStack.Add(new Dialog_EarthCommentaryLibrary(__instance))
            };
        }
    }
}
