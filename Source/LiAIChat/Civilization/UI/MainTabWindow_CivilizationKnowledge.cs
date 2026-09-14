using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace LiAIChat.Civilization.UI
{
    public class MainTabWindow_CivilizationKnowledge
        : MainTabWindow
    {
        private const float NodeWidth = 180f;
        private const float NodeHeight = 70f;

        private const float CanvasWidth = 3000f;
        private const float CanvasHeight = 2000f;

        private const float DragThreshold = 6f;

        private Vector2 canvasOffset =
            Vector2.zero;

        private bool canvasInitialized;

        private bool isDraggingCanvas;

        private Vector2 lastMousePosition;

        private bool mousePressedInViewport;

        private Vector2 mouseDownPosition;

        private string selectedNodeId;

        private readonly Dictionary<string, Rect>
            currentNodeRects =
                new Dictionary<string, Rect>();


        public override Vector2 RequestedTabSize
        {
            get
            {
                return new Vector2(
                    1200f,
                    760f);
            }
        }


        // ============================================================
        // Main Window
        // ============================================================

        public override void DoWindowContents(
            Rect inRect)
        {
            Text.Font =
                GameFont.Medium;

            Widgets.Label(
                new Rect(
                    0f,
                    0f,
                    inRect.width,
                    35f),
                "Civilization Knowledge");

            Text.Font =
                GameFont.Small;

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
                    inRect.height -
                    45f);

            Rect detailRect =
                new Rect(
                    viewportRect.xMax +
                    gap,
                    45f,
                    detailPanelWidth,
                    inRect.height -
                    45f);

            DrawTreeViewport(
                viewportRect);

            DrawNodeDetailPanel(
                detailRect);
        }


        // ============================================================
        // Tree Viewport
        // ============================================================

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
                        20f);

                canvasInitialized =
                    true;
            }

            HandleCanvasPan(
                viewportRect);

            Widgets.BeginGroup(
                viewportRect);

            DrawKnowledgeTree();

            Widgets.EndGroup();
        }


        // ============================================================
        // Mouse Interaction
        // ============================================================

        private void HandleCanvasPan(
            Rect viewportRect)
        {
            Event currentEvent =
                Event.current;

            if (currentEvent == null)
            {
                return;
            }

            Vector2 mousePosition =
                currentEvent.mousePosition;

            if (currentEvent.type ==
                    EventType.MouseDown &&
                currentEvent.button == 0 &&
                viewportRect.Contains(
                    mousePosition))
            {
                mousePressedInViewport =
                    true;

                isDraggingCanvas =
                    false;

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
                if (!isDraggingCanvas)
                {
                    float distance =
                        Vector2.Distance(
                            mouseDownPosition,
                            mousePosition);

                    if (distance >=
                        DragThreshold)
                    {
                        isDraggingCanvas =
                            true;

                        lastMousePosition =
                            mousePosition;
                    }
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
                bool wasDragging =
                    isDraggingCanvas;

                mousePressedInViewport =
                    false;

                isDraggingCanvas =
                    false;

                if (wasDragging)
                {
                    currentEvent.Use();
                    return;
                }

                HandleNodeClick(
                    viewportRect,
                    mousePosition);
            }
        }


        private void HandleNodeClick(
            Rect viewportRect,
            Vector2 mousePosition)
        {
            Vector2 viewportPosition =
                mousePosition -
                viewportRect.position;

            string nodeId =
                GetNodeAtPosition(
                    viewportPosition);

            if (nodeId == null)
            {
                selectedNodeId =
                    null;

                return;
            }

            selectedNodeId =
                nodeId;
        }


        private string GetNodeAtPosition(
            Vector2 viewportPosition)
        {
            foreach (
                KeyValuePair<string, Rect> pair
                in currentNodeRects)
            {
                if (pair.Value.Contains(
                        viewportPosition))
                {
                    return pair.Key;
                }
            }

            return null;
        }


        // ============================================================
        // Knowledge Tree
        // ============================================================

        private void DrawKnowledgeTree()
        {
            currentNodeRects.Clear();

            List<CivilizationKnowledgeDef> defs =
                DefDatabase<
                    CivilizationKnowledgeDef>
                .AllDefsListForReading;

            if (defs == null)
            {
                return;
            }

            foreach (
                CivilizationKnowledgeDef def
                in defs)
            {
                if (def == null)
                {
                    continue;
                }

                Rect nodeRect =
                    CreateNodeRect(
                        def);

                currentNodeRects[
                    def.defName] =
                    nodeRect;
            }

            DrawConnections(
                defs);

            DrawNodes(
                defs);
        }


        private Rect CreateNodeRect(
            CivilizationKnowledgeDef def)
        {
            if (def == null)
            {
                return Rect.zero;
            }

            /*
             * treeX / treeY are coordinates
             * inside the large knowledge canvas.
             *
             * canvasOffset moves the complete tree
             * around inside the visible viewport.
             */

            float x =
                canvasOffset.x +
                def.treeX;

            float y =
                canvasOffset.y +
                def.treeY;

            return new Rect(
                x,
                y,
                NodeWidth,
                NodeHeight);
        }


        // ============================================================
        // Connections
        // ============================================================

        private void DrawConnections(
            List<CivilizationKnowledgeDef> defs)
        {
            foreach (
                CivilizationKnowledgeDef def
                in defs)
            {
                if (def == null ||
                    def.prerequisites == null)
                {
                    continue;
                }

                Rect childRect;

                if (!currentNodeRects
                    .TryGetValue(
                        def.defName,
                        out childRect))
                {
                    continue;
                }

                foreach (
                    CivilizationKnowledgeDef prerequisite
                    in def.prerequisites)
                {
                    if (prerequisite == null)
                    {
                        continue;
                    }

                    Rect parentRect;

                    if (!currentNodeRects
                        .TryGetValue(
                            prerequisite.defName,
                            out parentRect))
                    {
                        continue;
                    }

                    DrawConnection(
                        parentRect,
                        childRect);
                }
            }
        }


        private void DrawConnection(
            Rect parentRect,
            Rect childRect)
        {
            Vector2 start =
                new Vector2(
                    parentRect.center.x,
                    parentRect.yMax);

            Vector2 end =
                new Vector2(
                    childRect.center.x,
                    childRect.yMin);

            float middleY =
                start.y +
                (end.y - start.y) /
                2f;

            Vector2 firstCorner =
                new Vector2(
                    start.x,
                    middleY);

            Vector2 secondCorner =
                new Vector2(
                    end.x,
                    middleY);

            Widgets.DrawLine(
                start,
                firstCorner,
                Color.gray,
                2f);

            Widgets.DrawLine(
                firstCorner,
                secondCorner,
                Color.gray,
                2f);

            Widgets.DrawLine(
                secondCorner,
                end,
                Color.gray,
                2f);
        }


        // ============================================================
        // Nodes
        // ============================================================

        private void DrawNodes(
            List<CivilizationKnowledgeDef> defs)
        {
            foreach (
                CivilizationKnowledgeDef def
                in defs)
            {
                if (def == null)
                {
                    continue;
                }

                Rect nodeRect;

                if (!currentNodeRects
                    .TryGetValue(
                        def.defName,
                        out nodeRect))
                {
                    continue;
                }

                bool selected =
                    def.defName ==
                    selectedNodeId;

                CivilizationKnowledgeState state =
                    CivilizationKnowledgeManager
                        .GetState(
                            def);

                DrawNode(
                    nodeRect,
                    GetNodeLabel(def),
                    selected,
                    state);
            }
        }


        private void DrawNode(
            Rect rect,
            string label,
            bool selected,
            CivilizationKnowledgeState state)
        {
            Color oldColor =
                GUI.color;

            bool locked =
                state == null ||
                !state.Unlocked;

            if (locked)
            {
                GUI.color =
                    new Color(
                        0.55f,
                        0.55f,
                        0.55f,
                        1f);
            }

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
                rect.ContractedBy(
                    6f),
                label);

            Text.Anchor =
                oldAnchor;

            GUI.color =
                oldColor;
        }


        private string GetNodeLabel(
            CivilizationKnowledgeDef def)
        {
            if (def == null)
            {
                return "";
            }

            if (!string.IsNullOrEmpty(
                    def.title))
            {
                return def.title;
            }

            return def.defName;
        }


        // ============================================================
        // Detail Panel
        // ============================================================

        private void DrawNodeDetailPanel(
            Rect rect)
        {
            Widgets.DrawMenuSection(
                rect);

            Rect innerRect =
                rect.ContractedBy(
                    12f);

            if (string.IsNullOrEmpty(
                    selectedNodeId))
            {
                TextAnchor oldAnchor =
                    Text.Anchor;

                Text.Anchor =
                    TextAnchor.MiddleCenter;

                Widgets.Label(
                    innerRect,
                    "Select a knowledge node.");

                Text.Anchor =
                    oldAnchor;

                return;
            }

            CivilizationKnowledgeDef def =
                FindKnowledgeDefById(
                    selectedNodeId);

            if (def == null)
            {
                Widgets.Label(
                    innerRect,
                    "Knowledge definition not found.");

                return;
            }

            CivilizationKnowledgeState state =
                CivilizationKnowledgeManager
                    .GetState(
                        def);

            float y =
                innerRect.y;


            // --------------------------------------------------------
            // Title
            // --------------------------------------------------------

            Text.Font =
                GameFont.Medium;

            Widgets.Label(
                new Rect(
                    innerRect.x,
                    y,
                    innerRect.width,
                    35f),
                GetNodeLabel(
                    def));

            y += 42f;

            Text.Font =
                GameFont.Small;


            // --------------------------------------------------------
            // Status
            // --------------------------------------------------------

            string status =
                GetKnowledgeStatusText(
                    state);

            Widgets.Label(
                new Rect(
                    innerRect.x,
                    y,
                    innerRect.width,
                    25f),
                "Status: " +
                status);

            y += 27f;


            // --------------------------------------------------------
            // ID
            // --------------------------------------------------------

            Widgets.Label(
                new Rect(
                    innerRect.x,
                    y,
                    innerRect.width,
                    25f),
                "ID: " +
                def.defName);

            y += 27f;


            // --------------------------------------------------------
            // Category
            // --------------------------------------------------------

            Widgets.Label(
                new Rect(
                    innerRect.x,
                    y,
                    innerRect.width,
                    25f),
                "Category: " +
                def.category);

            y += 34f;


            // --------------------------------------------------------
            // Separator
            // --------------------------------------------------------

            Widgets.DrawLineHorizontal(
                innerRect.x,
                y,
                innerRect.width);

            y += 12f;


            // --------------------------------------------------------
            // Prerequisites
            // --------------------------------------------------------

            Widgets.Label(
                new Rect(
                    innerRect.x,
                    y,
                    innerRect.width,
                    25f),
                "Prerequisites");

            y += 25f;

            if (def.prerequisites == null ||
                def.prerequisites.Count == 0)
            {
                Widgets.Label(
                    new Rect(
                        innerRect.x + 10f,
                        y,
                        innerRect.width - 10f,
                        22f),
                    "None");

                y += 22f;
            }
            else
            {
                foreach (
                    CivilizationKnowledgeDef prerequisite
                    in def.prerequisites)
                {
                    if (prerequisite == null)
                    {
                        continue;
                    }

                    Widgets.Label(
                        new Rect(
                            innerRect.x + 10f,
                            y,
                            innerRect.width - 10f,
                            22f),
                        "• " +
                        GetNodeLabel(
                            prerequisite));

                    y += 22f;
                }
            }


            y += 8f;


            // --------------------------------------------------------
            // Required Texts
            // --------------------------------------------------------

            Widgets.Label(
                new Rect(
                    innerRect.x,
                    y,
                    innerRect.width,
                    25f),
                "Required Texts");

            y += 25f;

            if (def.requiredTexts == null ||
                def.requiredTexts.Count == 0)
            {
                Widgets.Label(
                    new Rect(
                        innerRect.x + 10f,
                        y,
                        innerRect.width - 10f,
                        22f),
                    "None");

                y += 22f;
            }
            else
            {
                foreach (var textDef
                         in def.requiredTexts)
                {
                    if (textDef == null)
                    {
                        continue;
                    }

                    string textName =
                        textDef.label;

                    if (string.IsNullOrEmpty(
                            textName))
                    {
                        textName =
                            textDef.defName;
                    }

                    Widgets.Label(
                        new Rect(
                            innerRect.x + 10f,
                            y,
                            innerRect.width - 10f,
                            22f),
                        "• " +
                        textName);

                    y += 22f;
                }
            }


            y += 8f;


            // --------------------------------------------------------
            // Stability
            // --------------------------------------------------------

            Widgets.Label(
                new Rect(
                    innerRect.x,
                    y,
                    innerRect.width,
                    25f),
                "Grace Period: " +
                def.gracePeriodDays +
                " days");

            y += 24f;

            Widgets.Label(
                new Rect(
                    innerRect.x,
                    y,
                    innerRect.width,
                    25f),
                "Dormant After: " +
                def.dormantAfterDays +
                " days");
        }


        // ============================================================
        // Lookup
        // ============================================================

        private CivilizationKnowledgeDef
            FindKnowledgeDefById(
                string defName)
        {
            if (string.IsNullOrEmpty(
                    defName))
            {
                return null;
            }

            return DefDatabase<
                    CivilizationKnowledgeDef>
                .GetNamedSilentFail(
                    defName);
        }


        private string GetKnowledgeStatusText(
            CivilizationKnowledgeState state)
        {
            if (state == null ||
                !state.Unlocked)
            {
                return "Locked";
            }

            if (state.AwaitingReactivation)
            {
                return "Awaiting Reactivation";
            }

            if (state.Dormant)
            {
                return "Dormant";
            }

            if (state.Unstable)
            {
                return "Unstable";
            }

            return "Active";
        }
    }
}