using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using LiAIChat.Archive;
using RimWorld;
using Verse;

namespace LiAIChat.Civilization
{
    public class CivilizationTopicExtension : DefModExtension
    {
        public EarthTextDef requiredText;
        public string conclusion;
        public List<CivilizationTopicBonus> bonuses = new List<CivilizationTopicBonus>();
    }

    public class CivilizationTopicBonus
    {
        public string id;
        public string label;
        public CivilizationTopicEffect effect;
        public StatDef stat;
        public float offset;
        public float factor = 1f;
    }

    public class CivilizationTopicActiveBonus : IExposable
    {
        public string id;
        public string label;
        public CivilizationTopicEffect effect;
        public StatDef stat;
        public float offset;
        public float factor = 1f;

        public void ExposeData()
        {
            Scribe_Values.Look(ref id, "id");
            Scribe_Values.Look(ref label, "label");
            Scribe_Values.Look(ref effect, "effect");
            Scribe_Defs.Look(ref stat, "stat");
            Scribe_Values.Look(ref offset, "offset");
            Scribe_Values.Look(ref factor, "factor", 1f);
        }
    }

    public class CivilizationTopicState : IExposable
    {
        public ResearchProjectDef project;
        public StatDef stat;
        public string label;
        public CivilizationTopicEffect effect;
        public float offset;
        public float factor = 1f;
        public int expiresAt;
        public int availableAt;
        public bool progressReset;
        public string conclusion;
        public int completedAt = -1;
        public List<string> participants = new List<string>();
        public List<string> drawnBonuses = new List<string>();
        public List<CivilizationTopicActiveBonus> bonuses =
            new List<CivilizationTopicActiveBonus>();

        public void ExposeData()
        {
            Scribe_Defs.Look(ref project, "project");
            Scribe_Defs.Look(ref stat, "stat");
            Scribe_Values.Look(ref label, "label");
            Scribe_Values.Look(ref effect, "effect");
            Scribe_Values.Look(ref offset, "offset");
            Scribe_Values.Look(ref factor, "factor", 1f);
            Scribe_Values.Look(ref expiresAt, "expiresAt");
            Scribe_Values.Look(ref availableAt, "availableAt");
            Scribe_Values.Look(ref progressReset, "progressReset");
            Scribe_Values.Look(ref conclusion, "conclusion");
            Scribe_Values.Look(ref completedAt, "completedAt", -1);
            Scribe_Collections.Look(ref participants, "participants", LookMode.Value);
            Scribe_Collections.Look(ref drawnBonuses, "drawnBonuses", LookMode.Value);
            Scribe_Collections.Look(ref bonuses, "bonuses", LookMode.Deep);
            if (participants == null) participants = new List<string>();
            if (drawnBonuses == null) drawnBonuses = new List<string>();
            if (bonuses == null) bonuses = new List<CivilizationTopicActiveBonus>();
        }
    }

    public class CivilizationTopicGameComponent : GameComponent
    {
        private List<CivilizationTopicState> states = new List<CivilizationTopicState>();
        private ResearchProjectDef lastCompleted;
        private int consecutiveCompletions;
        private int librarySnapshotTick = -1;
        private HashSet<string> librarySnapshot = new HashSet<string>();
        private static readonly AccessTools.FieldRef<ResearchManager, Dictionary<ResearchProjectDef, float>>
            Progress = AccessTools.FieldRefAccess<ResearchManager, Dictionary<ResearchProjectDef, float>>("progress");

        public CivilizationTopicGameComponent(Verse.Game game) { }

        public static CivilizationTopicGameComponent Instance
        {
            get { return Current.Game == null ? null : Current.Game.GetComponent<CivilizationTopicGameComponent>(); }
        }

        public IEnumerable<CivilizationTopicState> Active
        {
            get { return states.Where(s => s != null && s.project != null && s.expiresAt > Find.TickManager.TicksGame); }
        }

        public IEnumerable<CivilizationTopicActiveBonus> ActiveBonuses
        {
            get { return Active.SelectMany(state => state.bonuses); }
        }

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref states, "civilizationTopicBonuses", LookMode.Deep);
            Scribe_Defs.Look(ref lastCompleted, "lastCompletedTopic");
            Scribe_Values.Look(ref consecutiveCompletions, "consecutiveTopicCompletions");
            if (states == null) states = new List<CivilizationTopicState>();
        }

        public CivilizationTopicState StateFor(ResearchProjectDef project)
        {
            return states.FirstOrDefault(s => s != null && s.project == project);
        }

        public bool HasRequiredText(EarthTextDef text)
        {
            if (text == null || Find.TickManager == null) return false;
            int now = Find.TickManager.TicksGame;
            // Multiple researchers and bench checks share one library scan per tick.
            // Refresh while paused too, so debug/UI changes cannot leave a stale gate.
            if (librarySnapshotTick != now || Find.TickManager.Paused)
            {
                librarySnapshot = new HashSet<string>(ColonyLibrary.GetAvailableTexts().Select(t => t.defName));
                librarySnapshotTick = now;
            }
            return librarySnapshot.Contains(text.defName);
        }

        public float Efficiency(ResearchProjectDef project)
        {
            return CivilizationTopicPolicy.ResearchEfficiency(lastCompleted == project, consecutiveCompletions);
        }

        public void RecordParticipant(ResearchProjectDef project, Pawn pawn)
        {
            if (pawn == null) return;
            CivilizationTopicState state = GetOrCreate(project);
            string name = pawn.LabelShort;
            if (!state.participants.Contains(name)) state.participants.Add(name);
        }

        private CivilizationTopicState GetOrCreate(ResearchProjectDef project)
        {
            CivilizationTopicState state = StateFor(project);
            if (state == null)
            {
                state = new CivilizationTopicState { project = project, progressReset = true };
                states.Add(state);
            }
            return state;
        }

        public override void FinalizeInit()
        {
            states.RemoveAll(s => s == null || s.project == null);
            foreach (CivilizationTopicState state in states)
            {
                MigrateLegacyBonus(state);
            }
            Expire();
            // Old saves may contain completed topics from before bonuses existed.
            // Reopen these without awarding a free, newly rolled bonus on load.
            foreach (ResearchProjectDef project in DefDatabase<ResearchProjectDef>.AllDefsListForReading)
                if (project.GetModExtension<CivilizationTopicExtension>() != null && project.IsFinished
                    && !states.Any(s => s.project == project)) Reset(project);
        }

        public override void GameComponentTick()
        {
            if (states.Count > 0) Expire();
        }

        private static void Reset(ResearchProjectDef project)
        {
            if (project == null || Find.ResearchManager == null) return;
            Progress(Find.ResearchManager).Remove(project);
        }

        private void Expire()
        {
            int now = Find.TickManager.TicksGame;
            for (int i = states.Count - 1; i >= 0; i--)
            {
                CivilizationTopicState state = states[i];
                if (state != null && state.project != null && (state.progressReset || state.expiresAt > now)) continue;
                if (state != null)
                {
                    Reset(state.project);
                    if (state.project != null)
                        Messages.Message(state.project.LabelCap + "：7 天加成已结束。冷却结束后可重新研究。", MessageTypeDefOf.NeutralEvent, false);
                }
                if (state == null || state.project == null) states.RemoveAt(i);
                else state.progressReset = true;
            }
        }

        private static void MigrateLegacyBonus(CivilizationTopicState state)
        {
            if (state.bonuses.Count > 0 || string.IsNullOrEmpty(state.label))
            {
                return;
            }

            state.bonuses.Add(new CivilizationTopicActiveBonus
            {
                label = state.label,
                effect = state.effect,
                stat = state.stat,
                offset = state.offset,
                factor = state.factor
            });
        }

        public void Complete(ResearchProjectDef project, Pawn researcher)
        {
            CivilizationTopicExtension topic = project.GetModExtension<CivilizationTopicExtension>();
            if (topic == null) return;
            CivilizationTopicState state = GetOrCreate(project);
            if (state.availableAt > Find.TickManager.TicksGame) return;
            List<CivilizationTopicBonus> choices = topic.bonuses.Where(b => b != null && !string.IsNullOrEmpty(b.id)
                && (b.effect != CivilizationTopicEffect.Stat || b.stat != null)).ToList();
            if (choices.Count == 0)
            {
                Log.Error("[Li AI Chat] No valid bonuses for " + project.defName);
                Reset(project);
                return;
            }
            List<CivilizationTopicBonus> selected =
                new List<CivilizationTopicBonus>();

            while (selected.Count < 3 && choices.Count > 0)
            {
                CivilizationTopicBonus bonus = choices.RandomElementByWeight(b =>
                    CivilizationTopicPolicy.RewardWeight(
                        state.drawnBonuses.Count(id => id == b.id)));
                selected.Add(bonus);
                choices.Remove(bonus);
            }

            RecordParticipant(project, researcher);
            state.bonuses.Clear();
            foreach (CivilizationTopicBonus bonus in selected)
            {
                state.bonuses.Add(new CivilizationTopicActiveBonus
                {
                    id = bonus.id,
                    label = bonus.label,
                    effect = bonus.effect,
                    stat = bonus.stat,
                    offset = bonus.offset,
                    factor = bonus.factor
                });
                state.drawnBonuses.Add(bonus.id);
            }
            state.completedAt = Find.TickManager.TicksGame;
            state.expiresAt = state.completedAt + CivilizationTopicPolicy.BonusDays * GenDate.TicksPerDay;
            state.availableAt = state.expiresAt + Rand.RangeInclusive(CivilizationTopicPolicy.MinimumCooldownDays,
                CivilizationTopicPolicy.MaximumCooldownDays) * GenDate.TicksPerDay;
            state.progressReset = false;
            string rewards = string.Join("；", selected.Select(b => b.label).ToArray());
            state.conclusion = CivilizationTopicConclusions.Build(topic, state.participants, rewards);
            state.participants.Clear();
            consecutiveCompletions = lastCompleted == project ? consecutiveCompletions + 1 : 1;
            lastCompleted = project;
            LiAIChat.Events.ColonyEventLog.Record("专题研究结论", state.conclusion, 3, researcher,
                "topic:" + project.defName + ":" + state.completedAt);
            int recovered = CivilizationTopicEffects.ApplyImmediateEffects(selected);
            Find.LetterStack.ReceiveLetter(project.LabelCap + "：研究结论", state.conclusion
                + "\n\n获得三项奖励：\n• " + string.Join("\n• ", selected.Select(b => b.label).ToArray())
                + (recovered > 0 ? "\n已解除 " + recovered + " 名殖民者的精神崩溃状态。" : string.Empty)
                + "\n持续 7 天；随后冷却 "
                + ((state.availableAt - state.expiresAt) / GenDate.TicksPerDay) + " 天。", LetterDefOf.PositiveEvent);
        }

        public string ConclusionContext(string textDefName = null, bool includeStatus = true)
        {
            var recent = states.Where(s => s != null && s.project != null && !string.IsNullOrEmpty(s.conclusion)
                && (textDefName == null || s.project.GetModExtension<CivilizationTopicExtension>()?.requiredText?.defName == textDefName))
                .OrderByDescending(s => s.completedAt).Take(4);
            return string.Join("\n", recent.Select(s => "- " + s.conclusion + (!includeStatus ? string.Empty
                : s.expiresAt > Find.TickManager.TicksGame ? "（本次效果仍在持续）" : "（历史结论，临时效果已结束）")).ToArray());
        }
    }

    public static class CivilizationTopicResearch
    {
        public static bool Allowed(ResearchProjectDef project)
        {
            CivilizationTopicExtension topic = project == null ? null : project.GetModExtension<CivilizationTopicExtension>();
            if (topic == null) return true;
            CivilizationTopicGameComponent component = CivilizationTopicGameComponent.Instance;
            CivilizationTopicState state = component == null ? null : component.StateFor(project);
            return component != null && (state == null || state.availableAt <= Find.TickManager.TicksGame)
                && HasRequiredText(project);
        }

        public static bool HasRequiredText(ResearchProjectDef project)
        {
            CivilizationTopicExtension topic = project == null
                ? null
                : project.GetModExtension<CivilizationTopicExtension>();

            if (topic == null)
            {
                return true;
            }

            CivilizationTopicGameComponent component =
                CivilizationTopicGameComponent.Instance;

            return component != null &&
                component.HasRequiredText(topic.requiredText);
        }

        public static bool HasAnyVisibleTopic()
        {
            return DefDatabase<ResearchProjectDef>.AllDefsListForReading.Any(
                project => project.tab != null &&
                    project.tab.defName == "LiAIChat_CivilizationTopics" &&
                    HasRequiredText(project));
        }
    }

    [StaticConstructorOnStartup]
    public static class CivilizationTopicStatSetup
    {
        static CivilizationTopicStatSetup()
        {
            foreach (StatDef stat in DefDatabase<ResearchProjectDef>.AllDefsListForReading
                .Select(p => p.GetModExtension<CivilizationTopicExtension>()).Where(t => t != null)
                .SelectMany(t => t.bonuses).Where(b => b.stat != null).Select(b => b.stat).Distinct())
            {
                if (stat.parts == null) stat.parts = new List<StatPart>();
                if (!stat.parts.Any(p => p is StatPart_CivilizationTopic))
                    stat.parts.Add(new StatPart_CivilizationTopic { parentStat = stat });
            }
        }
    }

    public class StatPart_CivilizationTopic : StatPart
    {
        private IEnumerable<CivilizationTopicActiveBonus> Bonuses(StatRequest req)
        {
            Pawn pawn = req.Thing as Pawn;
            CivilizationTopicGameComponent component = CivilizationTopicGameComponent.Instance;
            if (pawn == null || !pawn.IsColonistPlayerControlled || component == null)
                return Enumerable.Empty<CivilizationTopicActiveBonus>();
            return component.ActiveBonuses.Where(b => b.stat == parentStat);
        }

        public override void TransformValue(StatRequest req, ref float val)
        {
            foreach (CivilizationTopicActiveBonus bonus in Bonuses(req)) val = (val + bonus.offset) * bonus.factor;
        }

        public override string ExplanationPart(StatRequest req)
        {
            return string.Join("\n", Bonuses(req).Select(b => "文明专题 · " + b.label).ToArray());
        }
    }

    [HarmonyPatch(typeof(ResearchProjectDef), "get_CanStartNow")]
    public static class CivilizationTopicCanStartPatch
    {
        public static void Postfix(ResearchProjectDef __instance, ref bool __result)
        {
            if (__result) __result = CivilizationTopicResearch.Allowed(__instance);
        }
    }

    [HarmonyPatch(typeof(MainTabWindow_Research), "get_VisibleResearchProjects")]
    public static class CivilizationTopicVisibleProjectsPatch
    {
        public static void Postfix(ref List<ResearchProjectDef> __result)
        {
            if (__result == null)
            {
                return;
            }

            __result = __result.Where(
                CivilizationTopicResearch.HasRequiredText).ToList();
        }
    }

    // The research tab's layout path may draw its tab record directly instead
    // of the filtered VisibleResearchProjects list. IsHidden is the final
    // project-level gate used by both paths.
    [HarmonyPatch(typeof(ResearchProjectDef), "get_IsHidden")]
    public static class CivilizationTopicProjectHiddenPatch
    {
        public static void Postfix(ResearchProjectDef __instance, ref bool __result)
        {
            if (!__result && __instance != null &&
                __instance.GetModExtension<CivilizationTopicExtension>() != null)
            {
                __result = !CivilizationTopicResearch.HasRequiredText(__instance);
            }
        }
    }

    [HarmonyPatch(typeof(ResearchManager), "TabInfoVisible")]
    public static class CivilizationTopicTabVisibilityPatch
    {
        public static void Postfix(
            ResearchTabDef tab,
            ref bool __result)
        {
            if (tab != null &&
                tab.defName == "LiAIChat_CivilizationTopics")
            {
                __result = __result &&
                    CivilizationTopicResearch.HasAnyVisibleTopic();
            }
        }
    }

    [HarmonyPatch(typeof(ResearchProjectDef), "CanBeResearchedAt")]
    public static class CivilizationTopicBenchPatch
    {
        public static void Postfix(ResearchProjectDef __instance, ref bool __result)
        {
            if (__result) __result = CivilizationTopicResearch.Allowed(__instance);
        }
    }

    [HarmonyPatch(typeof(ResearchManager), "SetCurrentProject")]
    public static class CivilizationTopicSelectionPatch
    {
        public static bool Prefix(ResearchProjectDef proj) { return CivilizationTopicResearch.Allowed(proj); }
    }

    [HarmonyPatch(typeof(ResearchManager), "AddProgress")]
    public static class CivilizationTopicProgressPatch
    {
        public static bool Prefix(ResearchProjectDef proj, ref float amount, Pawn source)
        {
            if (!CivilizationTopicResearch.Allowed(proj)) return false;
            if (proj != null && proj.GetModExtension<CivilizationTopicExtension>() != null && amount > 0f)
            {
                var component = CivilizationTopicGameComponent.Instance;
                amount *= component.Efficiency(proj);
                component.RecordParticipant(proj, source);
            }
            return true;
        }
    }

    // ResearchPerformed writes directly into progress; it does not call AddProgress.
    [HarmonyPatch(typeof(ResearchManager), "ResearchPerformed")]
    public static class CivilizationTopicWorkPatch
    {
        public static bool Prefix(ResearchProjectDef ___currentProj, ref float amount, Pawn researcher)
        {
            return CivilizationTopicProgressPatch.Prefix(___currentProj, ref amount, researcher);
        }
    }

    [HarmonyPatch(typeof(ResearchManager), "FinishProject")]
    public static class CivilizationTopicCompletionPatch
    {
        public static bool Prefix(ResearchProjectDef proj, out bool __state)
        {
            // The normal caller has already reached Cost, so IsFinished is true here.
            __state = proj != null && proj.GetModExtension<CivilizationTopicExtension>() != null
                && CivilizationTopicResearch.Allowed(proj);
            return proj == null || proj.GetModExtension<CivilizationTopicExtension>() == null || __state;
        }

        public static void Postfix(ResearchProjectDef proj, Pawn researcher, bool __state)
        {
            if (__state && proj.IsFinished && CivilizationTopicGameComponent.Instance != null)
                CivilizationTopicGameComponent.Instance.Complete(proj, researcher);
        }
    }

    [HarmonyPatch(typeof(ResearchProjectDef), "get_Description")]
    public static class CivilizationTopicDescriptionPatch
    {
        public static void Postfix(ResearchProjectDef __instance, ref string __result)
        {
            CivilizationTopicExtension topic = __instance.GetModExtension<CivilizationTopicExtension>();
            if (topic == null || Current.Game == null) return;
            CivilizationTopicGameComponent component = CivilizationTopicGameComponent.Instance;
            __result += "\n\n馆藏条件：" + (component != null && component.HasRequiredText(topic.requiredText) ? "已满足" : "缺少已识别的对应文献")
                + "。需将文献保留在玩家基地地图上，研究期间失去馆藏会暂停进度。";
            CivilizationTopicState state = component == null ? null : component.StateFor(__instance);
            __result += "\n完成后不重复地随机获得下列三项，持续 7 天：\n" + string.Join("\n", topic.bonuses.Select(b => "• " + b.label).ToArray())
                + "\n结束后冷却 2–4 天。同书连研效率每轮乘以 80%（最低 40%），完成另一专题后重置。重复奖励的抽取权重降低。";
            if (state != null && state.expiresAt > Find.TickManager.TicksGame)
                __result += "\n当前加成：\n• " + string.Join("\n• ", state.bonuses.Select(b => b.label).ToArray()) + "\n剩余 "
                    + ((state.expiresAt - Find.TickManager.TicksGame) / (float)GenDate.TicksPerDay).ToString("0.0") + " 天。";
            else if (state != null && state.availableAt > Find.TickManager.TicksGame)
                __result += "\n冷却中：还需 " + ((state.availableAt - Find.TickManager.TicksGame) / (float)GenDate.TicksPerDay).ToString("0.0") + " 天。";
            if (component != null) __result += "\n本轮研究效率：" + component.Efficiency(__instance).ToStringPercent() + "。";
            if (state != null && !string.IsNullOrEmpty(state.conclusion)) __result += "\n上次研究结论：" + state.conclusion;
        }
    }
}
