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

        private const float CanvasWidth = 3000f;
        private const float CanvasHeight = 2000f;
        public override Vector2 RequestedTabSize
        {
            get { return new Vector2(1200f, 760f); }
        }

        private Vector2 canvasOffset = Vector2.zero;
        private bool canvasInitialized;
        private bool isDraggingCanvas;
        private Vector2 lastMousePosition;
        private string selectedNodeId;
        private bool mousePressedInViewport;
        private Vector2 mouseDownPosition;
        private const float DragThreshold = 6f;
        private readonly Dictionary<string, Rect> currentNodeRects = new Dictionary<string, Rect>();

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

            float detailPanelWidth =
                300f;

            float gap =
                12f;

            Rect viewportRect =
                new Rect(
                    0f,
                    45f,
                    inRect.width -
                    detailPanelWidth -
                    gap,
                    inRect.height - 45f);

            Rect detailRect =
                new Rect(
                    viewportRect.xMax +
                    gap,
                    45f,
                    detailPanelWidth,
                    inRect.height - 45f);

            DrawTreeViewport(
                viewportRect);

            DrawNodeDetailPanel(
                detailRect);
        }
        private CivilizationKnowledgeNode
    FindNodeById(
        string nodeId)
        {
            if (nodeId == null)
            {
                return null;
            }

            foreach (CivilizationKnowledgeNode node
                     in CivilizationKnowledgeDatabase.Nodes)
            {
                if (node.Id == nodeId)
                {
                    return node;
                }
            }

            return null;
        }
        private string GetParentLabel(
    CivilizationKnowledgeNode node)
        {
            if (node == null)
            {
                return "";
            }

            if (node.ParentId == null)
            {
                return "None";
            }

            CivilizationKnowledgeNode parent =
                FindNodeById(
                    node.ParentId);

            if (parent == null)
            {
                return node.ParentId;
            }

            return parent.Label;
        }
        private void DrawNodeDetailPanel(
    Rect rect)
        {
            Widgets.DrawMenuSection(
                rect);

            Rect innerRect =
                rect.ContractedBy(12f);

            if (selectedNodeId == null)
            {
                Text.Anchor =
                    TextAnchor.MiddleCenter;

                Widgets.Label(
                    innerRect,
                    "Select a knowledge node.");

                Text.Anchor =
                    TextAnchor.UpperLeft;

                return;
            }

            CivilizationKnowledgeNode node =
                FindNodeById(
                    selectedNodeId);

            if (node == null)
            {
                return;
            }

            float y =
                innerRect.y;

            Text.Font =
                GameFont.Medium;

            Widgets.Label(
                new Rect(
                    innerRect.x,
                    y,
                    innerRect.width,
                    35f),
                node.Label);

            y += 45f;

            Text.Font =
                GameFont.Small;

            Widgets.Label(
                new Rect(
                    innerRect.x,
                    y,
                    innerRect.width,
                    25f),
                "Id: " + node.Id);

            y += 28f;

            Widgets.Label(
                new Rect(
                    innerRect.x,
                    y,
                    innerRect.width,
                    25f),
                "Tier: " + node.Tier);

            y += 28f;

            Widgets.Label(
                new Rect(
                    innerRect.x,
                    y,
                    innerRect.width,
                    40f),
                "Parent: " +
                GetParentLabel(node));

            y += 45f;

            Widgets.DrawLineHorizontal(
                innerRect.x,
                y,
                innerRect.width);

            y += 12f;

            Widgets.Label(
                new Rect(
                    innerRect.x,
                    y,
                    innerRect.width,
                    25f),
                "Description");

            y += 28f;

            float descriptionHeight =
    Text.CalcHeight(
        node.Description,
        innerRect.width);

            Widgets.Label(
                new Rect(
                    innerRect.x,
                    y,
                    innerRect.width,
                    descriptionHeight),
                node.Description);
                node.Description);
        }

        private void DrawTreeViewport(
    Rect viewportRect)
        {
            Widgets.DrawMenuSection(
                viewportRect);

            if (!canvasInitialized)
            {
                canvasOffset =
                    new Vector2(
                        viewportRect.width / 2f -
                        CanvasWidth / 2f,
                        0f);

                canvasInitialized = true;
            }

            HandleCanvasPan(
                viewportRect);

            Widgets.BeginGroup(
                viewportRect);

            Rect canvasRect =
                new Rect(
                    canvasOffset.x,
                    canvasOffset.y,
                    CanvasWidth,
                    CanvasHeight);

            DrawKnowledgeTree(
                canvasRect);

            Widgets.EndGroup();
        }
        private void HandleCanvasPan(
    Rect viewportRect)
        {
            Event currentEvent =
                Event.current;

            Vector2 mousePosition =
                currentEvent.mousePosition;

            if (currentEvent.type ==
                    EventType.MouseDown &&
                currentEvent.button == 0 &&
                viewportRect.Contains(
                    mousePosition))
            {
                mousePressedInViewport = true;

                isDraggingCanvas = false;

                mouseDownPosition =
                    mousePosition;

                lastMousePosition =
                    mousePosition;

                return;
            }

            if (currentEvent.type ==
                    EventType.MouseDrag &&
                currentEvent.button == 0 &&
                mousePressedInViewport)
            {
                float dragDistance =
                    Vector2.Distance(
                        mouseDownPosition,
                        mousePosition);

                if (!isDraggingCanvas &&
                    dragDistance >= DragThreshold)
                {
                    isDraggingCanvas = true;
                }

                if (isDraggingCanvas)
                {
                    Vector2 delta =
                        mousePosition -
                        lastMousePosition;

                    canvasOffset +=
                        delta;

                    lastMousePosition =
                        mousePosition;

                    currentEvent.Use();
                }

                return;
            }

            if (currentEvent.type ==
                    EventType.MouseUp &&
                currentEvent.button == 0 &&
                mousePressedInViewport)
            {
                if (isDraggingCanvas)
                {
                    isDraggingCanvas = false;

                    mousePressedInViewport = false;

                    currentEvent.Use();

                    return;
                }

                HandleNodeClick(
                    viewportRect,
                    mousePosition);

                mousePressedInViewport =
                    false;
            }
        }
        private void HandleNodeClick(
    Rect viewportRect,
    Vector2 mousePosition)
        {
            Vector2 viewportPosition =
                mousePosition -
                viewportRect.position;

            Vector2 treePosition =
                ViewportToTreePosition(
                    viewportPosition);

            string nodeId =
                GetNodeAtPosition(
                    treePosition);

            if (nodeId == null)
            {
                selectedNodeId = null;

                return;
            }

            selectedNodeId =
                nodeId;
        }
        private void DrawKnowledgeTree(Rect rect)
        {
            currentNodeRects.Clear();
            List<CivilizationKnowledgeNode> nodes = CivilizationKnowledgeDatabase.Nodes;
            Dictionary<string, Rect> nodeRects = currentNodeRects;
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
        private string GetNodeAtPosition(
    Vector2 treePosition)
        {
            foreach (KeyValuePair<string, Rect> pair
                     in currentNodeRects)
            {
                if (pair.Value.Contains(
                        treePosition))
                {
                    return pair.Key;
                }
            }

            return null;
        }
        private Vector2 ViewportToTreePosition(
    Vector2 viewportPosition)
        {
            return viewportPosition -
                canvasOffset;
        }
        private float CalculateSubtreeWidth(CivilizationKnowledgeNode node, List<CivilizationKnowledgeNode> nodes)
        {
            List<CivilizationKnowledgeNode> children = GetChildren(nodes, node.Id);
            if (children.Count == 0)
            {
                return NodeWidth;
            }

            float childrenWidth = 0f;
            for (int i = 0; i < children.Count; i++)
            {
                childrenWidth += CalculateSubtreeWidth(children[i], nodes);

                if (i < children.Count - 1)
                {
                    childrenWidth += HorizontalGap;
                }
            }

            return Mathf.Max(NodeWidth, childrenWidth);
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

                bool selected = node.Id == selectedNodeId;
                DrawNode(nodeRect, node.Label, selected);
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

        private void LayoutChildren(
    CivilizationKnowledgeNode parent,
    List<CivilizationKnowledgeNode> nodes,
    Dictionary<string, Rect> nodeRects)
        {
            List<CivilizationKnowledgeNode> children =
                GetChildren(
                    nodes,
                    parent.Id);

            if (children.Count == 0)
            {
                return;
            }

            Rect parentRect;

            if (!nodeRects.TryGetValue(
                    parent.Id,
                    out parentRect))
            {
                return;
            }

            float totalWidth = 0f;

            foreach (CivilizationKnowledgeNode child
                     in children)
            {
                totalWidth +=
                    CalculateSubtreeWidth(
                        child,
                        nodes);
            }

            totalWidth +=
                (children.Count - 1) *
                HorizontalGap;

            float currentX =
                parentRect.center.x -
                totalWidth / 2f;

            float childY =
                parentRect.yMax +
                VerticalGap;

            foreach (CivilizationKnowledgeNode child
                     in children)
            {
                float subtreeWidth =
                    CalculateSubtreeWidth(
                        child,
                        nodes);

                float childCenterX =
                    currentX +
                    subtreeWidth / 2f;

                Rect childRect =
                    new Rect(
                        childCenterX -
                        NodeWidth / 2f,
                        childY,
                        NodeWidth,
                        NodeHeight);

                nodeRects[child.Id] =
                    childRect;

                LayoutChildren(
                    child,
                    nodes,
                    nodeRects);

                currentX +=
                    subtreeWidth +
                    HorizontalGap;
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
        private void DrawNode(
    Rect rect,
    string label,
    bool selected)
        {
            Widgets.DrawMenuSection(
                rect);

            if (selected)
            {
                Widgets.DrawHighlight(
                    rect);
            }

            TextAnchor oldAnchor =
                Text.Anchor;

            Text.Anchor =
                TextAnchor.MiddleCenter;

            Widgets.Label(
                rect.ContractedBy(6f),
                label);

            Text.Anchor =
                oldAnchor;
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