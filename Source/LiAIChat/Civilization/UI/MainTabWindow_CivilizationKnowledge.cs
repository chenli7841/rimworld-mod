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
            Dictionary<string, Rect> nodeRects = new Dictionary<string, Rect>();
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

            Rect rootRect = new Rect(rect.center.x - NodeWidth / 2f, rect.y + 40f, NodeWidth, NodeHeight);
            nodeRects[root.Id] = rootRect;
            LayoutChildren(root, nodes, nodeRects);
            DrawConnections(nodes, nodeRects);
            DrawNodes(nodes, nodeRects);
        }

        private void DrawConnections(List<CivilizationKnowledgeNode> nodes, Dictionary<string, Rect> nodeRects)
        {
            foreach (CivilizationKnowledgeNode node in nodes)
            {
                if (node.ParentId == null)
                {
                    continue;
                }

                Rect parentRect;

                if (!nodeRects.TryGetValue(node.ParentId, out parentRect))
                {
                    continue;
                }

                Rect childRect;

                if (!nodeRects.TryGetValue(node.Id, out childRect))
                {
                    continue;
                }

                DrawConnection(parentRect, childRect);
            }
        }

        private void DrawConnection(Rect parentRect, Rect childRect)
        {
            Vector2 parentBottom = new Vector2(parentRect.center.x, parentRect.yMax);

            Vector2 childTop = new Vector2(childRect.center.x, childRect.yMin);

            float middleY = (parentBottom.y + childTop.y) / 2f;

            Vector2 point1 = new Vector2(parentBottom.x, middleY);

            Vector2 point2 = new Vector2(childTop.x, middleY);

            Widgets.DrawLine(parentBottom, point1, Color.gray, 2f);
            Widgets.DrawLine(point1, point2, Color.gray, 2f);
            Widgets.DrawLine(point2, childTop, Color.gray, 2f);
        }
        private void DrawNodes(List<CivilizationKnowledgeNode> nodes, Dictionary<string, Rect> nodeRects)
        {
            foreach (CivilizationKnowledgeNode node in nodes)
            {
                Rect nodeRect;

                if (!nodeRects.TryGetValue(node.Id, out nodeRect))
                {
                    continue;
                }

                DrawNode(nodeRect, node.Label);
            }
        }
        private List<CivilizationKnowledgeNode> GetNodesForTier(List<CivilizationKnowledgeNode> nodes, int tier)
        {
            List<CivilizationKnowledgeNode> result = new List<CivilizationKnowledgeNode>();

            foreach (CivilizationKnowledgeNode node in nodes)
            {
                if (node.Tier == tier)
                {
                    result.Add(node);
                }
            }

            return result;
        }

        private List<CivilizationKnowledgeNode> GetChildren(List<CivilizationKnowledgeNode> nodes, string parentId)
        {
            List<CivilizationKnowledgeNode> result = new List<CivilizationKnowledgeNode>();

            foreach (CivilizationKnowledgeNode node in nodes)
            {
                if (node.ParentId == parentId)
                {
                    result.Add(node);
                }
            }

            return result;
        }

        private void LayoutChildren(CivilizationKnowledgeNode parent, List<CivilizationKnowledgeNode> nodes, Dictionary<string, Rect> nodeRects)
        {
            List<CivilizationKnowledgeNode> children = GetChildren(nodes, parent.Id);

            if (children.Count == 0)
            {
                return;
            }

            Rect parentRect;

            if (!nodeRects.TryGetValue(parent.Id, out parentRect))
            {
                return;
            }

            float totalWidth = children.Count * NodeWidth + (children.Count - 1) * HorizontalGap;

            float startX = parentRect.center.x - totalWidth / 2f;

            float childY = parentRect.yMax + VerticalGap;

            for (int i = 0; i < children.Count; i++)
            {
                CivilizationKnowledgeNode child = children[i];

                Rect childRect = new Rect(startX + i * (NodeWidth + HorizontalGap), childY, NodeWidth, NodeHeight);

                nodeRects[child.Id] = childRect;

                LayoutChildren(child, nodes, nodeRects);
            }
        }

        private int GetMaxTier(List<CivilizationKnowledgeNode> nodes)
        {
            int maxTier = 0;

            foreach (CivilizationKnowledgeNode node in nodes)
            {
                if (node.Tier > maxTier)
                {
                    maxTier = node.Tier;
                }
            }

            return maxTier;
        }
        private void DrawNode(Rect rect, string label)
        {
            Widgets.DrawMenuSection(rect);
            TextAnchor oldAnchor = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(rect.ContractedBy(6f), label);
            Text.Anchor = oldAnchor;
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