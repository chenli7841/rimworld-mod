using LiAIChat.Archive;
using LiAIChat.Knowledge;
using LiAIChat.Models;
using LiAIChat.State;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace LiAIChat.UI
{
    public class Dialog_PawnCivilizationProfile : Window
    {
        private enum ProfileTab
        {
            Overview,
            Topics,
            Texts
        }

        private readonly Pawn pawn;
        private ProfileTab selectedTab = ProfileTab.Overview;
        private Vector2 scrollPosition = Vector2.zero;

        public override Vector2 InitialSize
        {
            get { return new Vector2(700f, 650f); }
        }

        public Dialog_PawnCivilizationProfile(Pawn pawn)
        {
            this.pawn = pawn;
            doCloseX = true;
            doCloseButton = true;
            closeOnClickedOutside = false;
            absorbInputAroundWindow = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            PawnAIState state = PawnAIStateManager.GetState(pawn);

            if (pawn == null || state == null || state.Knowledge == null)
            {
                Widgets.Label(inRect, "该殖民者的文明知识不可用。 ");
                return;
            }

            Text.Font = GameFont.Medium;
            Widgets.Label(
                new Rect(inRect.x, inRect.y, inRect.width, 32f),
                pawn.LabelShort + " — 文明知识");

            Text.Font = GameFont.Small;
            int knownTextCount = GetKnownTexts(state).Count;
            Widgets.Label(
                new Rect(inRect.x, inRect.y + 34f, inRect.width, 22f),
                "个人知识会影响对话与未来的文化创作。已记录 " +
                knownTextCount + " 本远古地球文献。");

            float tabsY = inRect.y + 64f;
            DrawTab(new Rect(inRect.x, tabsY, 120f, 28f), ProfileTab.Overview, "总览");
            DrawTab(new Rect(inRect.x + 126f, tabsY, 120f, 28f), ProfileTab.Topics, "主题");
            DrawTab(new Rect(inRect.x + 252f, tabsY, 120f, 28f), ProfileTab.Texts, "文献");

            Rect outerRect = new Rect(
                inRect.x,
                tabsY + 40f,
                inRect.width,
                inRect.height - 126f);

            float contentHeight = CalculateContentHeight(state, outerRect.width - 16f);
            Rect viewRect = new Rect(0f, 0f, outerRect.width - 16f, contentHeight);

            Widgets.BeginScrollView(outerRect, ref scrollPosition, viewRect);

            if (selectedTab == ProfileTab.Overview)
            {
                DrawOverview(viewRect, state);
            }
            else if (selectedTab == ProfileTab.Topics)
            {
                DrawTopics(viewRect, state);
            }
            else
            {
                DrawTexts(viewRect, state);
            }

            Widgets.EndScrollView();
        }

        private void DrawTab(Rect rect, ProfileTab tab, string label)
        {
            bool active = selectedTab == tab;
            Color oldColor = GUI.color;

            if (active)
            {
                GUI.color = new Color(0.72f, 0.55f, 0.25f);
            }

            if (Widgets.ButtonText(rect, label))
            {
                selectedTab = tab;
                scrollPosition = Vector2.zero;
            }

            GUI.color = oldColor;
        }

        private float CalculateContentHeight(PawnAIState state, float width)
        {
            if (selectedTab == ProfileTab.Overview)
            {
                return 360f;
            }

            if (selectedTab == ProfileTab.Topics)
            {
                return Mathf.Max(120f, 44f + GetKnownTopics(state).Count * 48f);
            }

            return Mathf.Max(120f, 44f + GetKnownTexts(state).Count * 64f);
        }

        private void DrawOverview(Rect rect, PawnAIState state)
        {
            float y = 0f;

            Widgets.Label(new Rect(rect.x, y, rect.width, 24f), "文明知识领域");
            y += 30f;

            y = DrawDomainBar(rect, y, "远古地球历史", state.Knowledge.EarthHistoryKnowledge);
            y = DrawDomainBar(rect, y, "哲学", state.Knowledge.PhilosophyKnowledge);
            y = DrawDomainBar(rect, y, "宗教", state.Knowledge.ReligiousKnowledge);
            y = DrawDomainBar(rect, y, "政治思想", state.Knowledge.PoliticsKnowledge);
            y = DrawDomainBar(rect, y, "科学", state.Knowledge.ScienceKnowledge);

            y += 12f;
            Widgets.DrawLineHorizontal(rect.x, y, rect.width);
            y += 12f;

            List<KnowledgeTopic> topics = GetKnownTopics(state).Take(3).ToList();
            Widgets.Label(new Rect(rect.x, y, rect.width, 24f), "当前最强的知识影响");
            y += 28f;

            if (topics.Count == 0)
            {
                Widgets.Label(new Rect(rect.x + 10f, y, rect.width - 10f, 24f), "尚未掌握具体的远古地球主题。 ");
                return;
            }

            foreach (KnowledgeTopic topic in topics)
            {
                Widgets.Label(
                    new Rect(rect.x + 10f, y, rect.width - 10f, 24f),
                    KnowledgeTopicCatalog.GetChineseDisplayName(topic.TopicId) +
                    " — " + GetFamiliarityLabel(topic.Familiarity));
                y += 26f;
            }
        }

        private float DrawDomainBar(Rect rect, float y, string label, float value)
        {
            value = Mathf.Clamp01(value);
            Widgets.Label(new Rect(rect.x, y, rect.width - 60f, 22f), label);
            Text.Anchor = TextAnchor.UpperRight;
            Widgets.Label(new Rect(rect.x + rect.width - 56f, y, 56f, 22f), value.ToStringPercent());
            Text.Anchor = TextAnchor.UpperLeft;
            y += 22f;
            Widgets.FillableBar(new Rect(rect.x, y, rect.width, 12f), value);
            return y + 24f;
        }

        private void DrawTopics(Rect rect, PawnAIState state)
        {
            float y = 0f;
            List<KnowledgeTopic> topics = GetKnownTopics(state);

            Widgets.Label(new Rect(rect.x, y, rect.width, 24f), "已了解主题");
            y += 28f;

            if (topics.Count == 0)
            {
                Widgets.Label(new Rect(rect.x + 10f, y, rect.width - 10f, 24f), "尚未学习具体主题。 ");
                return;
            }

            foreach (KnowledgeTopic topic in topics)
            {
                Widgets.Label(
                    new Rect(rect.x, y, rect.width - 130f, 22f),
                    KnowledgeTopicCatalog.GetChineseDisplayName(topic.TopicId));
                Text.Anchor = TextAnchor.UpperRight;
                Widgets.Label(
                    new Rect(rect.x + rect.width - 126f, y, 126f, 22f),
                    GetFamiliarityLabel(topic.Familiarity) + " " + topic.Familiarity.ToStringPercent());
                Text.Anchor = TextAnchor.UpperLeft;
                y += 22f;
                Widgets.FillableBar(new Rect(rect.x, y, rect.width, 12f), topic.Familiarity);
                y += 26f;
            }
        }

        private void DrawTexts(Rect rect, PawnAIState state)
        {
            float y = 0f;
            List<EarthTextDef> texts = GetKnownTexts(state);

            Widgets.Label(new Rect(rect.x, y, rect.width, 24f), "远古地球文献研读记录");
            y += 28f;

            if (texts.Count == 0)
            {
                Widgets.Label(new Rect(rect.x + 10f, y, rect.width - 10f, 24f), "该殖民者尚未研读具体的远古地球文献。 ");
                return;
            }

            foreach (EarthTextDef text in texts)
            {
                float familiarity = state.GetEarthTextFamiliarity(text.defName);
                string title = !string.IsNullOrWhiteSpace(text.titleChinese)
                    ? text.titleChinese
                    : (string.IsNullOrWhiteSpace(text.title) ? text.label : text.title);
                string author = string.IsNullOrWhiteSpace(text.author) ? "作者不详" : text.author;

                Widgets.Label(new Rect(rect.x, y, rect.width, 22f), title);
                y += 22f;
                Widgets.Label(new Rect(rect.x + 10f, y, rect.width - 10f, 20f), author + " · " + GetFamiliarityLabel(familiarity));
                y += 20f;
                Widgets.FillableBar(new Rect(rect.x + 10f, y, rect.width - 10f, 12f), familiarity);
                y += 22f;
            }
        }

        private static List<KnowledgeTopic> GetKnownTopics(PawnAIState state)
        {
            return state.Knowledge.KnownTopics == null
                ? new List<KnowledgeTopic>()
                : state.Knowledge.KnownTopics
                    .Where(topic => topic != null && topic.Familiarity > 0.01f)
                    .OrderByDescending(topic => topic.Familiarity)
                    .ThenBy(topic => KnowledgeTopicCatalog.GetChineseDisplayName(topic.TopicId))
                    .ToList();
        }

        private static List<EarthTextDef> GetKnownTexts(PawnAIState state)
        {
            return DefDatabase<EarthTextDef>.AllDefsListForReading
                .Where(text => text != null && state.GetEarthTextFamiliarity(text.defName) > 0.01f)
                .OrderByDescending(text => state.GetEarthTextFamiliarity(text.defName))
                .ThenBy(text => text.title ?? text.label)
                .ToList();
        }

        private static string GetFamiliarityLabel(float familiarity)
        {
            if (familiarity >= 0.99f)
            {
                return "深入掌握";
            }

            if (familiarity >= 0.80f)
            {
                return "非常熟悉";
            }

            if (familiarity >= 0.60f)
            {
                return "较为熟悉";
            }

            if (familiarity >= 0.35f)
            {
                return "有所了解";
            }

            if (familiarity >= 0.15f)
            {
                return "初步了解";
            }

            return "略有耳闻";
        }
    }
}
