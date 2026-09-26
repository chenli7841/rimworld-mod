using HarmonyLib;
using LiAIChat.AI;
using LiAIChat.Background;
using LiAIChat.Config;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;

namespace LiAIChat.TelevisionRecipes
{
    public enum TelevisionRecipeBuff { Hunger, Rest, MentalShield, MoveSpeed, WorkSpeed, Healing, WeaponRange }

    public class TelevisionRecipeData : IExposable
    {
        public int slot;
        public string name;
        public string description;
        public List<string> ingredients = new List<string>();
        public List<TelevisionRecipeBuff> buffs = new List<TelevisionRecipeBuff>();
        public int durationTicks;
        public int buffTicks;
        public bool learned;
        public void ExposeData()
        {
            Scribe_Values.Look(ref slot, "slot"); Scribe_Values.Look(ref name, "name"); Scribe_Values.Look(ref description, "description");
            Scribe_Collections.Look(ref ingredients, "ingredients", LookMode.Value);
            Scribe_Collections.Look(ref buffs, "buffs", LookMode.Value);
            Scribe_Values.Look(ref durationTicks, "durationTicks"); Scribe_Values.Look(ref buffTicks, "buffTicks"); Scribe_Values.Look(ref learned, "learned");
            if (ingredients == null) ingredients = new List<string>(); if (buffs == null) buffs = new List<TelevisionRecipeBuff>();
        }
    }

    public class TelevisionRecipePawnBuff : IExposable
    {
        public int pawnId; public List<TelevisionRecipeBuff> buffs = new List<TelevisionRecipeBuff>(); public int expiresAt;
        public void ExposeData() { Scribe_Values.Look(ref pawnId, "pawnId"); Scribe_Collections.Look(ref buffs, "buffs", LookMode.Value); Scribe_Values.Look(ref expiresAt, "expiresAt"); if (buffs == null) buffs = new List<TelevisionRecipeBuff>(); }
    }

    public class TelevisionRecipeGameComponent : GameComponent
    {
        private const int MaxActive = 5, CacheTarget = 3, SlotCount = 8, CheckInterval = 2500;
        private static readonly HttpClient Client = new HttpClient();
        private List<TelevisionRecipeData> recipes = new List<TelevisionRecipeData>();
        private List<TelevisionRecipePawnBuff> pawnBuffs = new List<TelevisionRecipePawnBuff>();
        private int lastCheck; private bool generationPending;
        public TelevisionRecipeGameComponent(Verse.Game game) { }
        public static TelevisionRecipeGameComponent Instance { get { return Current.Game == null ? null : Current.Game.GetComponent<TelevisionRecipeGameComponent>(); } }
        public override void ExposeData()
        {
            Scribe_Collections.Look(ref recipes, "televisionRecipes", LookMode.Deep); Scribe_Collections.Look(ref pawnBuffs, "televisionRecipePawnBuffs", LookMode.Deep);
            Scribe_Values.Look(ref generationPending, "televisionRecipeGenerationPending", false);
            if (recipes == null) recipes = new List<TelevisionRecipeData>(); if (pawnBuffs == null) pawnBuffs = new List<TelevisionRecipePawnBuff>();
        }
        public override void FinalizeInit()
        {
            base.FinalizeInit();
            // Cached entries made before recipe prose existed are safe to replace.
            recipes.RemoveAll(r => !r.learned && string.IsNullOrWhiteSpace(r.description));
            SanitizeIngredients();
            generationPending = false;
            RefreshDefs();
        }
        public override void GameComponentTick()
        {
            int now = Find.TickManager.TicksGame; if (now - lastCheck < CheckInterval) return; lastCheck = now;
            Expire(now); if (SanitizeIngredients()) RefreshDefs(); TryTeachFromTelevision(); EnsureCache();
        }
        private void Expire(int now)
        {
            bool changed = recipes.RemoveAll(r => r.learned && r.durationTicks <= now) > 0;
            pawnBuffs.RemoveAll(b => b.expiresAt <= now);
            foreach (TelevisionRecipePawnBuff buff in pawnBuffs.Where(b => b.buffs.Contains(TelevisionRecipeBuff.Healing)))
            {
                Pawn pawn = PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists.FirstOrDefault(p => p.thingIDNumber == buff.pawnId);
                Hediff scar = pawn == null ? null : pawn.health.hediffSet.hediffs.FirstOrDefault(h => h.IsPermanent() && h.Severity > 0.01f);
                if (scar != null) scar.Severity = Mathf.Max(0.01f, scar.Severity - 0.015f);
            }
            if (changed) { RefreshDefs(); Messages.Message("一份电视菜谱已经过期，已从烹饪列表移除。", MessageTypeDefOf.NeutralEvent, false); }
        }
        private void TryTeachFromTelevision()
        {
            if (recipes.Count(r => r.learned) >= MaxActive) return;
            TelevisionRecipeData recipe = recipes.FirstOrDefault(r => !r.learned); if (recipe == null) return;
            Pawn viewer = PawnsFinder.AllMaps_FreeColonists.FirstOrDefault(p => p.CurJob != null && p.CurJob.def != null && p.CurJob.def.defName == "WatchTelevision");
            if (viewer == null || !Rand.Chance(0.30f)) return;
            recipe.learned = true; recipe.durationTicks = Find.TickManager.TicksGame + Rand.RangeInclusive(12, 18) * GenDate.TicksPerDay;
            RefreshDefs();
            Find.LetterStack.ReceiveLetter("看电视学会了新菜谱", viewer.LabelShort + " 在电视节目中学会了“" + recipe.name + "”。全殖民地现在都能烹饪它；有效期为 " + (recipe.durationTicks - Find.TickManager.TicksGame) / GenDate.TicksPerDay + " 天。", LetterDefOf.PositiveEvent, viewer);
        }
        private void EnsureCache()
        {
            if (generationPending || recipes.Count(r => !r.learned) >= CacheTarget) return;
            generationPending = true;
            List<string> foods = RawFoodDefNames();
            int needed = CacheTarget - recipes.Count(r => !r.learned); string gameId = RuntimeHelpers.GameId(this);
            _ = GenerateAsync(gameId, needed, foods);
        }
        private async Task GenerateAsync(string gameId, int count, List<string> foods)
        {
            try
            {
                string prompt = "为RimWorld殖民地电视烹饪节目生成" + count + "道限时菜谱。每道只输出一行，严格格式：中文菜名|英文defName食材,英文defName食材|buff,buff|中文描述。菜名必须4到8个汉字。描述约200个汉字，生动、具体、令人食欲大开，须自然提到该菜绑定的具体buff效果和它们的短暂性；不可包含换行或竖线。食材从以下现有物资选择，2到3种且合理：" + string.Join(",", foods) + "。buff仅可用 hunger,rest,mental,move,work,healing,range；每道1到2个。";
                string body = "{\"model\":\"gpt-5.6-luna\",\"input\":" + Json(prompt) + ",\"max_output_tokens\":500}";
                using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/responses"))
                { request.Headers.Add("Authorization", "Bearer " + Config.Config.OpenAI_API_KEY); request.Content = new StringContent(body, Encoding.UTF8, "application/json"); using (HttpResponseMessage response = await Client.SendAsync(request).ConfigureAwait(false)) { string output = OpenAIResponseParser.ExtractOutputText(await response.Content.ReadAsStringAsync().ConfigureAwait(false)); MainThreadActionQueue.Enqueue(() => ApplyGenerated(gameId, Parse(output, foods, count))); } }
            }
            catch (Exception ex) { Log.Warning("[Li AI Chat] TV recipe generation failed: " + ex.Message); MainThreadActionQueue.Enqueue(() => { if (RuntimeHelpers.IsCurrent(gameId)) generationPending = false; }); }
        }
        private void ApplyGenerated(string gameId, List<TelevisionRecipeData> added)
        {
            if (!RuntimeHelpers.IsCurrent(gameId)) return; generationPending = false;
            foreach (TelevisionRecipeData recipe in added) if (recipes.Count(r => !r.learned) < CacheTarget) { recipe.slot = FreeSlot(); recipe.buffTicks = Rand.RangeInclusive(24, 48) * 2500; if (recipe.slot >= 0) recipes.Add(recipe); }
            RefreshDefs();
        }
        private int FreeSlot() { for (int i = 0; i < SlotCount; i++) if (!recipes.Any(r => r.slot == i)) return i; return -1; }
        private static List<TelevisionRecipeData> Parse(string text, List<string> foods, int count)
        {
            List<TelevisionRecipeData> result = new List<TelevisionRecipeData>();
            foreach (string line in (text ?? "").Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string[] p = line.Trim().Trim('-', ' ', '*').Split('|'); if (p.Length < 2) continue;
                string name = p[0].Trim(); if (name.Length < 4 || name.Length > 8) continue;
                List<string> ing = p[1].Split(',').Select(x => x.Trim()).Where(foods.Contains).Distinct().Take(3).ToList(); if (ing.Count < 2) continue;
                TelevisionRecipeData r = new TelevisionRecipeData { name = name, ingredients = ing };
                if (p.Length > 2) foreach (string value in p[2].Split(',')) { TelevisionRecipeBuff b; if (TryBuff(value.Trim(), out b) && !r.buffs.Contains(b)) r.buffs.Add(b); }
                if (r.buffs.Count == 0) r.buffs.Add(TelevisionRecipeBuff.MoveSpeed);
                r.description = p.Length > 3 ? p[3].Trim() : null;
                result.Add(r); if (result.Count >= count) break;
            }
            while (result.Count < count && foods.Count >= 2) result.Add(new TelevisionRecipeData { name = new[] { "菠萝披萨", "海鲜炒饭", "鱼香肉丝", "香草炖肉", "莓果派对" }[result.Count % 5], ingredients = foods.Skip(result.Count).Take(2).ToList(), buffs = new List<TelevisionRecipeBuff> { TelevisionRecipeBuff.WorkSpeed }, description = "热气从锅沿轻轻升起，丰润的食材在浓香中翻滚，入口后让人暂时忘却荒野的粗粝。饱足的暖意会让工作动作更利落；这份电视节目带来的小小力量只会陪伴一段时间，趁热享用吧。" });
            return result;
        }
        private static bool TryBuff(string value, out TelevisionRecipeBuff buff) { switch (value.ToLowerInvariant()) { case "hunger": buff = TelevisionRecipeBuff.Hunger; return true; case "rest": buff = TelevisionRecipeBuff.Rest; return true; case "mental": buff = TelevisionRecipeBuff.MentalShield; return true; case "move": buff = TelevisionRecipeBuff.MoveSpeed; return true; case "work": buff = TelevisionRecipeBuff.WorkSpeed; return true; case "healing": buff = TelevisionRecipeBuff.Healing; return true; case "range": buff = TelevisionRecipeBuff.WeaponRange; return true; default: buff = TelevisionRecipeBuff.MoveSpeed; return false; } }
        private static string Json(string s) { return "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n") + "\""; }
        private static bool IsRawFood(ThingDef def)
        {
            if (def == null || def.thingCategories == null)
            {
                return false;
            }

            ThingCategoryDef rawFood =
                DefDatabase<ThingCategoryDef>.GetNamedSilentFail("FoodRaw");

            return rawFood != null && def.thingCategories.Any(category =>
                IsCategoryOrChildOf(category, rawFood));
        }

        private static bool IsCategoryOrChildOf(
            ThingCategoryDef category,
            ThingCategoryDef ancestor)
        {
            for (ThingCategoryDef current = category;
                current != null;
                current = current.parent)
            {
                if (current == ancestor)
                {
                    return true;
                }
            }

            return false;
        }
        private static List<string> RawFoodDefNames()
        {
            return Find.Maps.SelectMany(m => m.listerThings.AllThings).Select(t => t.def).Where(IsRawFood).Select(d => d.defName)
                .Concat(Find.Maps.SelectMany(m => m.mapPawns.AllPawnsSpawned).Where(p => p.RaceProps != null && IsRawFood(p.RaceProps.meatDef)).Select(p => p.RaceProps.meatDef.defName))
                .Distinct().Take(24).ToList();
        }
        private bool SanitizeIngredients()
        {
            List<string> allowed = RawFoodDefNames();
            if (allowed.Count == 0) return false;
            bool changed = false;
            foreach (TelevisionRecipeData recipe in recipes)
            {
                List<string> before = recipe.ingredients;
                recipe.ingredients = recipe.ingredients.Where(defName => allowed.Contains(defName) && IsRawFood(DefDatabase<ThingDef>.GetNamedSilentFail(defName))).Distinct().ToList();
                foreach (string replacement in allowed.Where(defName => !recipe.ingredients.Contains(defName)))
                {
                    if (recipe.ingredients.Count >= 2) break;
                    recipe.ingredients.Add(replacement);
                }
                if (!before.SequenceEqual(recipe.ingredients)) changed = true;
            }
            return changed;
        }
        public TelevisionRecipeData RecipeForProduct(ThingDef def) { return recipes.FirstOrDefault(r => r.learned && TelevisionRecipeUtility.ProductDef(r.slot) == def); }
        public bool HasBuff(Pawn pawn, TelevisionRecipeBuff buff) { return pawn != null && pawnBuffs.Any(b => b.pawnId == pawn.thingIDNumber && b.expiresAt > Find.TickManager.TicksGame && b.buffs.Contains(buff)); }
        public void ApplyMeal(Pawn pawn, ThingDef product) { TelevisionRecipeData r = RecipeForProduct(product); if (pawn == null || r == null) return; pawnBuffs.RemoveAll(b => b.pawnId == pawn.thingIDNumber); pawnBuffs.Add(new TelevisionRecipePawnBuff { pawnId = pawn.thingIDNumber, buffs = new List<TelevisionRecipeBuff>(r.buffs), expiresAt = Find.TickManager.TicksGame + r.buffTicks }); }
        public void RefreshDefs() { TelevisionRecipeUtility.Refresh(recipes); }
    }

    public static class RuntimeHelpers { public static string GameId(TelevisionRecipeGameComponent c) { return Current.Game.GetHashCode().ToString(); } public static bool IsCurrent(string id) { return Current.Game != null && Current.Game.GetHashCode().ToString() == id; } }
    public static class TelevisionRecipeUtility
    {
        public static ThingDef ProductDef(int slot) { return DefDatabase<ThingDef>.GetNamedSilentFail("LiAIChat_TelevisionMeal" + (char)('A' + slot)); }
        public static RecipeDef RecipeDef(int slot) { return DefDatabase<RecipeDef>.GetNamedSilentFail("LiAIChat_TelevisionRecipe" + (slot + 1)); }
        public static void Refresh(List<TelevisionRecipeData> recipes)
        {
            List<RecipeDef> televisionRecipes = Enumerable.Range(0, 8).Select(RecipeDef).Where(r => r != null).ToList();
            foreach (ThingDef table in DefDatabase<ThingDef>.AllDefsListForReading.Where(t =>
                t.recipeMaker != null && t.recipes != null &&
                t.recipes.Any(recipe => recipe != null && recipe.workSkill == SkillDefOf.Cooking)))
            {
                if (table.recipes == null) table.recipes = new List<RecipeDef>();
                // RecipeDef.AvailableNow is not consulted by every bill-menu path.
                // Keep locked cache slots out of the worktable list itself.
                table.recipes.RemoveAll(televisionRecipes.Contains);
                foreach (TelevisionRecipeData active in recipes.Where(r => r.learned && r.durationTicks > Find.TickManager.TicksGame).Take(5))
                {
                    RecipeDef televisionRecipe = RecipeDef(active.slot);
                    if (televisionRecipe != null) table.recipes.Add(televisionRecipe);
                }
                // AllRecipes keeps its own cache, separate from ThingDef.recipes.
                AccessTools.Field(typeof(ThingDef), "allRecipesCached").SetValue(table, null);
            }
            for (int slot = 0; slot < 8; slot++)
            {
                TelevisionRecipeData data = recipes.FirstOrDefault(r => r.slot == slot && r.learned); RecipeDef recipe = RecipeDef(slot); ThingDef product = ProductDef(slot); if (recipe == null || product == null) continue;
                int daysLeft = data == null || Find.TickManager == null ? 0 : Mathf.CeilToInt((data.durationTicks - Find.TickManager.TicksGame) / (float)GenDate.TicksPerDay);
                recipe.label = data == null ? "电视菜谱（未解锁）" : data.name + " ×2"; recipe.description = data == null ? "观看电视节目后可能学会。" : (string.IsNullOrWhiteSpace(data.description) ? "限时电视菜谱。" : data.description + "\n\n") + "厨艺 12 级可制作。剩余有效期：" + daysLeft + " 天。食用后 " + BuffText(data.buffs) + "。"; product.label = recipe.label;
                // Def.LabelCap is memoized after the bill menu first opens.
                // Clear it whenever a runtime recipe receives its generated name.
                AccessTools.Field(typeof(Def), "cachedLabelCap").SetValue(recipe, default(TaggedString));
                AccessTools.Field(typeof(Def), "cachedLabelCap").SetValue(product, default(TaggedString));
                AccessTools.Field(typeof(ThingDef), "descriptionDetailedCached").SetValue(product, null);
                recipe.ingredients = data == null ? new List<IngredientCount>() : data.ingredients.Select((defName, i) => Ingredient(defName, i == 0 ? 10 : 5)).Where(x => x != null).ToList();
                recipe.workSkill = SkillDefOf.Cooking; recipe.skillRequirements = new List<SkillRequirement> { new SkillRequirement { skill = SkillDefOf.Cooking, minLevel = 12 } };
            }
        }
        private static string BuffText(List<TelevisionRecipeBuff> buffs) { return string.Join("、", buffs.Select(b => new Dictionary<TelevisionRecipeBuff, string> { { TelevisionRecipeBuff.Hunger, "饥饿下降减缓" }, { TelevisionRecipeBuff.Rest, "休息下降减缓" }, { TelevisionRecipeBuff.MentalShield, "不会精神崩溃" }, { TelevisionRecipeBuff.MoveSpeed, "移动更快" }, { TelevisionRecipeBuff.WorkSpeed, "工作更快" }, { TelevisionRecipeBuff.Healing, "旧伤缓慢恢复" }, { TelevisionRecipeBuff.WeaponRange, "射程提高" } }[b])); }
        private static IngredientCount Ingredient(string defName, int count) { ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(defName); if (def == null) return null; IngredientCount result = new IngredientCount(); result.filter = new ThingFilter(); result.filter.SetAllow(def, true); result.SetBaseCount(count); return result; }
    }

    [HarmonyPatch(typeof(Thing), "Ingested")]
    public static class TelevisionRecipeIngestPatch { public static void Postfix(Thing __instance, Pawn ingester) { TelevisionRecipeGameComponent.Instance?.ApplyMeal(ingester, __instance.def); } }
    [HarmonyPatch(typeof(Need_Food), "NeedInterval")]
    public static class TelevisionRecipeHungerPatch { public static void Prefix(Need_Food __instance, out float __state) { __state = __instance.CurLevel; } public static void Postfix(Need_Food __instance, Pawn ___pawn, float __state) { if (TelevisionRecipeGameComponent.Instance?.HasBuff(___pawn, TelevisionRecipeBuff.Hunger) == true && __instance.CurLevel < __state) __instance.CurLevel = __state + (__instance.CurLevel - __state) * 0.6f; } }
    [HarmonyPatch(typeof(Need_Rest), "NeedInterval")]
    public static class TelevisionRecipeRestPatch { public static void Prefix(Need_Rest __instance, out float __state) { __state = __instance.CurLevel; } public static void Postfix(Need_Rest __instance, Pawn ___pawn, float __state) { if (TelevisionRecipeGameComponent.Instance?.HasBuff(___pawn, TelevisionRecipeBuff.Rest) == true && __instance.CurLevel < __state) __instance.CurLevel = __state + (__instance.CurLevel - __state) * 0.6f; } }
    [HarmonyPatch(typeof(Verse.AI.MentalStateHandler), "TryStartMentalState")]
    public static class TelevisionRecipeMentalPatch { public static bool Prefix(Pawn ___pawn) { return TelevisionRecipeGameComponent.Instance?.HasBuff(___pawn, TelevisionRecipeBuff.MentalShield) != true; } }
    [HarmonyPatch(typeof(VerbProperties), "AdjustedRange")]
    public static class TelevisionRecipeRangePatch { public static void Postfix(Thing attacker, ref float __result) { if (TelevisionRecipeGameComponent.Instance?.HasBuff(attacker as Pawn, TelevisionRecipeBuff.WeaponRange) == true) __result *= 1.15f; } }
    [HarmonyPatch(typeof(Pawn_PathFollower), "CostToMoveIntoCell", new Type[] { typeof(IntVec3) })]
    public static class TelevisionRecipeMovePatch { public static void Postfix(Pawn ___pawn, ref float __result) { if (TelevisionRecipeGameComponent.Instance?.HasBuff(___pawn, TelevisionRecipeBuff.MoveSpeed) == true) __result /= 1.15f; } }
    [HarmonyPatch(typeof(StatWorker), "GetValueUnfinalized")]
    public static class TelevisionRecipeWorkPatch { public static void Postfix(StatDef ___stat, StatRequest req, ref float __result) { Pawn pawn = req.Thing as Pawn; if (___stat != null && ___stat.defName == "WorkSpeedGlobal" && TelevisionRecipeGameComponent.Instance?.HasBuff(pawn, TelevisionRecipeBuff.WorkSpeed) == true) __result *= 1.15f; } }
    [HarmonyPatch(typeof(RecipeDef), "get_AvailableNow")]
    public static class TelevisionRecipeVisibilityPatch
    {
        public static void Postfix(RecipeDef __instance, ref bool __result)
        {
            if (__instance != null && __instance.defName.StartsWith("LiAIChat_TelevisionRecipe"))
                __result = __result && TelevisionRecipeGameComponent.Instance != null && TelevisionRecipeGameComponent.Instance.RecipeForProduct(__instance.products.First().thingDef) != null;
        }
    }
    [HarmonyPatch(typeof(ThingDef), "get_AllRecipes")]
    public static class TelevisionRecipeAllRecipesPatch
    {
        public static void Postfix(ref List<RecipeDef> __result)
        {
            if (__result == null) return;
            TelevisionRecipeGameComponent component = TelevisionRecipeGameComponent.Instance;
            __result = __result.Where(recipe => recipe == null || !recipe.defName.StartsWith("LiAIChat_TelevisionRecipe") ||
                (component != null && recipe.products != null && recipe.products.Count > 0 && component.RecipeForProduct(recipe.products[0].thingDef) != null)).ToList();
        }
    }
}
