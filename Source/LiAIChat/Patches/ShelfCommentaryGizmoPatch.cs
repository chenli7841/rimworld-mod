using HarmonyLib;
using LiAIChat.Commentary;
using LiAIChat.Books;
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
            yield return new Command_Action
            {
                defaultLabel = "创建新书",
                defaultDesc = "消耗 100 张书写纸与 20 支笔，创建一本可自由编辑正文的新书。",
                action = () => PlayerBookEditorService.TryCreate(__instance)
            };
        }
    }
}
