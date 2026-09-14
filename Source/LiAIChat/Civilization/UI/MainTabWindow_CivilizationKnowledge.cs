using RimWorld;
using UnityEngine;
using Verse;

namespace LiAIChat.Civilization.UI
{
    public class MainTabWindow_CivilizationKnowledge : MainTabWindow
    {
        private const float NodeWidth = 180f;
        private const float NodeHeight = 70f;

        private const float HorizontalGap = 120f;
        private const float VerticalGap = 90f;
        public override Vector2 RequestedTabSize
        {
            get
            {
                return new Vector2(
                    1200f,
                    760f);
            }
        }

        public override void DoWindowContents(
            Rect inRect)
        {
            Text.Font = GameFont.Medium;

            Widgets.Label(
                new Rect(
                    0f,
                    0f,
                    inRect.width,
                    35f),
                "Civilization Knowledge");

            Text.Font = GameFont.Small;

            Rect treeRect = new Rect(0f, 45f, inRect.width, inRect.height - 45f);

            DrawKnowledgeTree(treeRect);
        }
        private void DrawKnowledgeTree(Rect rect)
        {
            float centerX = rect.x + rect.width / 2f;
            float topY = rect.y + 40f;

            Rect rootRect = new Rect(centerX - NodeWidth / 2f, topY, NodeWidth, NodeHeight);
            Rect philosophyRect = new Rect(rootRect.x - NodeWidth - HorizontalGap, rootRect.y + NodeHeight + VerticalGap, NodeWidth, NodeHeight);
            Rect scienceRect = new Rect(rootRect.x + NodeWidth + HorizontalGap, rootRect.y + NodeHeight + VerticalGap, NodeWidth, NodeHeight);

            DrawConnection(rootRect, philosophyRect);
            DrawConnection(rootRect, scienceRect);;
            DrawNode(rootRect, "Ancient Earth\nFoundations");
            DrawNode(philosophyRect, "Philosophy");
            DrawNode(scienceRect, "Science");
        }
        private void DrawNode(Rect rect, string label)
        {
            Widgets.DrawMenuSection(rect);
            TextAnchor oldAnchor = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(rect.ContractedBy(6f), label);
            Text.Anchor = oldAnchor;
        }

        private void DrawConnection(Rect parentRect, Rect childRect)
        {
            Vector2 parentBottomCenter = new Vector2(parentRect.center.x, parentRect.yMax);
            Vector2 childTopCenter = new Vector2(childRect.center.x, childRect.y);
            float middleY = (parentBottomCenter.y + childTopCenter.y) / 2f;

            Vector2 firstCorner = new Vector2(parentBottomCenter.x, middleY);
            Vector2 secondCorner = new Vector2(childTopCenter.x, middleY);

            Widgets.DrawLine(parentBottomCenter, firstCorner, Color.gray, 2f);
            Widgets.DrawLine(firstCorner, secondCorner, Color.gray, 2f);
            Widgets.DrawLine(secondCorner, childTopCenter, Color.gray, 2f);
        }
    }
}