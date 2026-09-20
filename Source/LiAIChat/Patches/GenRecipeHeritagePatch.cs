using HarmonyLib;
using LiAIChat.Heritage;
using Verse;

namespace LiAIChat.Patches
{
    [HarmonyPatch(typeof(GenRecipe), "PostProcessProduct")]
    public static class GenRecipeHeritagePatch
    {
        public static void Postfix(Thing __result, Pawn __1)
        {
            CivilizationHeritageService.TryGenerate(__1, __result);
        }
    }
}
