using RimWorld;
using System.Collections.Generic;
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
            List<CivilizationKnowledgeNode> nodes = CivilizationKnowledgeDatabase.Nodes;

            CivilizationKnowledgeNode root = null;

            foreach (CivilizationKnowledgeNode node in nodes)
            {
                if (node.ParentId == null)
                {
                    root = node;
                    break;
                }
            }

            if (root == null)
            {
                return;
            }

            float centerX = rect.center.x;

            float topY = rect.y + 40f;

            Rect rootRect = new Rect(centerX - NodeWidth / 2f, topY, NodeWidth, NodeHeight);

            List<CivilizationKnowledgeNode> children = new List<CivilizationKnowledgeNode>();

            foreach (CivilizationKnowledgeNode node in nodes)
            {
                if (node.ParentId == root.Id)
                {
                    children.Add(node);
                }
            }

            Rect[] childRects = new Rect[children.Count];

            float totalWidth = children.Count * NodeWidth + (children.Count - 1) * HorizontalGap;

            float startX = centerX - totalWidth / 2f;

            float childY = rootRect.yMax + VerticalGap;

            for (int i = 0; i < children.Count; i++)
            {
                childRects[i] = new Rect(startX + i * (NodeWidth + HorizontalGap), childY, NodeWidth, NodeHeight);
            }

            DrawBranch(rootRect, childRects);
            DrawNode(rootRect, root.Label);

            for (int i = 0; i < children.Count; i++)
            {
                DrawNode(childRects[i], children[i].Label);
            }
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

        private void DrawBranch(Rect parentRect, Rect[] childRects)
        {
            if (childRects == null || childRects.Length == 0)
            {
                return;
            }

            Vector2 parentBottom = new Vector2(parentRect.center.x, parentRect.yMax);

            float branchY = parentRect.yMax + VerticalGap / 2f;

            Vector2 branchStart = new Vector2(parentBottom.x, branchY);

            // Parent → branch
            Widgets.DrawLine(parentBottom, branchStart, Color.gray, 2f);

            float leftX = childRects[0].center.x;
            float rightX = childRects[0].center.x;

            foreach (Rect childRect in childRects)
            {
                if (childRect.center.x < leftX)
                {
                    leftX = childRect.center.x;
                }

                if (childRect.center.x > rightX)
                {
                    rightX = childRect.center.x;
                }
            }

            // Horizontal branch
            Widgets.DrawLine(new Vector2(leftX, branchY), new Vector2(rightX, branchY), Color.gray, 2f);

            // Branch → children
            foreach (Rect childRect in childRects)
            {
                Vector2 childTop = new Vector2(childRect.center.x, childRect.yMin);
                Vector2 childBranchPoint = new Vector2(childRect.center.x, branchY);
                Widgets.DrawLine(childBranchPoint, childTop, Color.gray, 2f);
            }
        }
    }
}