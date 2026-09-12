using System.Collections.Generic;
using LiAIChat.Archive;
using UnityEngine;
using Verse;

namespace LiAIChat.Civilization
{
    public class Window_CivilizationKnowledge : Window
    {
        private Vector2 scrollPosition = Vector2.zero;

        public override Vector2 InitialSize
        {
            get
            {
                return new Vector2(700f, 650f);
            }
        }

        public Window_CivilizationKnowledge()
        {
            doCloseX = true;
            doCloseButton = true;
            closeOnClickedOutside = false;
            absorbInputAroundWindow = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;

            Widgets.Label(
                new Rect(
                    inRect.x,
                    inRect.y,
                    inRect.width,
                    35f),
                "Civilization Knowledge");

            Text.Font = GameFont.Small;

            Rect outerRect =
                new Rect(
                    inRect.x,
                    inRect.y + 45f,
                    inRect.width,
                    inRect.height - 90f);

            List<CivilizationKnowledgeDef> defs =
                DefDatabase<CivilizationKnowledgeDef>
                    .AllDefsListForReading;

            float contentHeight =
                CalculateContentHeight(defs);

            Rect viewRect =
                new Rect(
                    0f,
                    0f,
                    outerRect.width - 16f,
                    contentHeight);

            Widgets.BeginScrollView(
                outerRect,
                ref scrollPosition,
                viewRect);

            float y = 0f;

            foreach (CivilizationKnowledgeDef def
                in defs)
            {
                y = DrawKnowledgeNode(
                    viewRect,
                    y,
                    def);
            }

            Widgets.EndScrollView();
        }

        private float CalculateContentHeight(
            List<CivilizationKnowledgeDef> defs)
        {
            if (defs == null)
            {
                return 100f;
            }

            float height = 0f;

            foreach (CivilizationKnowledgeDef def
                in defs)
            {
                if (def == null)
                {
                    continue;
                }

                int requiredCount =
                    def.requiredTexts != null
                        ? def.requiredTexts.Count
                        : 0;

                height +=
                    90f +
                    requiredCount * 28f;
            }

            return Mathf.Max(
                height,
                100f);
        }

        private float DrawKnowledgeNode(
            Rect viewRect,
            float y,
            CivilizationKnowledgeDef def)
        {
            if (def == null)
            {
                return y;
            }

            bool unlocked =
                CivilizationKnowledgeManager
                    .IsUnlocked(def);

            Rect titleRect =
                new Rect(
                    0f,
                    y,
                    viewRect.width,
                    26f);

            Text.Font = GameFont.Medium;

            Widgets.Label(
                titleRect,
                def.label);

            Text.Font = GameFont.Small;

            y += 30f;

            Widgets.Label(
                new Rect(
                    15f,
                    y,
                    viewRect.width - 15f,
                    24f),
                "Status: "
                + (unlocked
                    ? "Unlocked"
                    : "Locked"));

            y += 30f;

            Widgets.Label(
                new Rect(
                    15f,
                    y,
                    viewRect.width - 15f,
                    24f),
                "Required Texts:");

            y += 26f;

            if (def.requiredTexts != null)
            {
                foreach (EarthTextDef text
                    in def.requiredTexts)
                {
                    bool available =
                        ColonyLibrary
                            .HasText(text);

                    string prefix =
                        available
                            ? "✓ "
                            : "✗ ";

                    string display =
                        prefix
                        + GetTextDisplayName(text);

                    Widgets.Label(
                        new Rect(
                            35f,
                            y,
                            viewRect.width - 35f,
                            24f),
                        display);

                    y += 28f;
                }
            }

            y += 20f;

            Widgets.DrawLineHorizontal(
                0f,
                y,
                viewRect.width);

            y += 20f;

            return y;
        }

        private string GetTextDisplayName(
            EarthTextDef text)
        {
            if (text == null)
            {
                return "Unknown text";
            }

            string author =
                string.IsNullOrWhiteSpace(text.author)
                    ? "Unknown author"
                    : text.author;

            string title =
                string.IsNullOrWhiteSpace(text.title)
                    ? text.label
                    : text.title;

            return author
                   + " — "
                   + title;
        }
    }
}