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
                Widgets.Label(inRect, "Civilization knowledge is unavailable for this pawn.");
                return;
            }

            Text.Font = GameFont.Medium;
            Widgets.Label(
                new Rect(inRect.x, inRect.y, inRect.width, 32f),
                pawn.LabelShort + " — Civilization Knowledge");

            Text.Font = GameFont.Small;
            int knownTextCount = GetKnownTexts(state).Count;
            Widgets.Label(
                new Rect(inRect.x, inRect.y + 34f, inRect.width, 22f),
                "Individual knowledge, used by dialogue and future cultural creations. " +
                knownTextCount + " Earth texts recorded.");

            float tabsY = inRect.y + 64f;
            DrawTab(new Rect(inRect.x, tabsY, 120f, 28f), ProfileTab.Overview, "Overview");
            DrawTab(new Rect(inRect.x + 126f, tabsY, 120f, 28f), ProfileTab.Topics, "Topics");
            DrawTab(new Rect(inRect.x + 252f, tabsY, 120f, 28f), ProfileTab.Texts, "Texts");

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

            Widgets.Label(new Rect(rect.x, y, rect.width, 24f), "Civilization Domains");
            y += 30f;

            y = DrawDomainBar(rect, y, "Ancient Earth history", state.Knowledge.EarthHistoryKnowledge);
            y = DrawDomainBar(rect, y, "Philosophy", state.Knowledge.PhilosophyKnowledge);
            y = DrawDomainBar(rect, y, "Religion", state.Knowledge.ReligiousKnowledge);
            y = DrawDomainBar(rect, y, "Political thought", state.Knowledge.PoliticsKnowledge);
            y = DrawDomainBar(rect, y, "Science", state.Knowledge.ScienceKnowledge);

            y += 12f;
            Widgets.DrawLineHorizontal(rect.x, y, rect.width);
            y += 12f;

            List<KnowledgeTopic> topics = GetKnownTopics(state).Take(3).ToList();
            Widgets.Label(new Rect(rect.x, y, rect.width, 24f), "Strongest Current Influences");
            y += 28f;

            if (topics.Count == 0)
            {
                Widgets.Label(new Rect(rect.x + 10f, y, rect.width - 10f, 24f), "No specific Ancient Earth topics are known yet.");
                return;
            }

            foreach (KnowledgeTopic topic in topics)
            {
                Widgets.Label(
                    new Rect(rect.x + 10f, y, rect.width - 10f, 24f),
                    KnowledgeTopicCatalog.GetDisplayName(topic.TopicId) +
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

            Widgets.Label(new Rect(rect.x, y, rect.width, 24f), "Known Topics");
            y += 28f;

            if (topics.Count == 0)
            {
                Widgets.Label(new Rect(rect.x + 10f, y, rect.width - 10f, 24f), "No specific topics have been learned.");
                return;
            }

            foreach (KnowledgeTopic topic in topics)
            {
                Widgets.Label(
                    new Rect(rect.x, y, rect.width - 130f, 22f),
                    KnowledgeTopicCatalog.GetDisplayName(topic.TopicId));
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

            Widgets.Label(new Rect(rect.x, y, rect.width, 24f), "Earth Text Study Record");
            y += 28f;

            if (texts.Count == 0)
            {
                Widgets.Label(new Rect(rect.x + 10f, y, rect.width - 10f, 24f), "This pawn has not studied a specific Earth text yet.");
                return;
            }

            foreach (EarthTextDef text in texts)
            {
                float familiarity = state.GetEarthTextFamiliarity(text.defName);
                string title = string.IsNullOrWhiteSpace(text.title) ? text.label : text.title;
                string author = string.IsNullOrWhiteSpace(text.author) ? "Unknown author" : text.author;

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
                    .ThenBy(topic => KnowledgeTopicCatalog.GetDisplayName(topic.TopicId))
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
                return "Deeply understood";
            }

            if (familiarity >= 0.80f)
            {
                return "Very familiar";
            }

            if (familiarity >= 0.60f)
            {
                return "Familiar";
            }

            if (familiarity >= 0.35f)
            {
                return "Some understanding";
            }

            if (familiarity >= 0.15f)
            {
                return "Basic familiarity";
            }

            return "Barely familiar";
        }
    }
}
