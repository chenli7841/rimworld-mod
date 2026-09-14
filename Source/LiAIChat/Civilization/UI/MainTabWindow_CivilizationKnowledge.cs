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

        private float zoom = 1f;
        private const float MinZoom = 0.6f;
        private const float MaxZoom = 1.4f;
        private const float ZoomStep = 0.1f;

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

        private void HandleTreeZoom(
    Rect viewportRect)
        {
            Event currentEvent =
                Event.current;

            if (currentEvent == null)
            {
                return;
            }

            if (currentEvent.type !=
                EventType.ScrollWheel)
            {
                return;
            }

            Vector2 mousePosition =
                currentEvent.mousePosition;

            if (!viewportRect.Contains(
                    mousePosition))
            {
                return;
            }

            float oldZoom =
                zoom;

            if (currentEvent.delta.y < 0f)
            {
                zoom +=
                    ZoomStep;
            }
            else if (
                currentEvent.delta.y > 0f)
            {
                zoom -=
                    ZoomStep;
            }

            zoom =
                Mathf.Clamp(
                    zoom,
                    MinZoom,
                    MaxZoom);

            if (!Mathf.Approximately(
                    oldZoom,
                    zoom))
            {
                Vector2 mouseInViewport =
                    mousePosition -
                    viewportRect.position;

                AdjustCanvasOffsetForZoom(
                    mouseInViewport,
                    oldZoom,
                    zoom);
            }

            // 非常重要：
            // 阻止这个 scroll event 继续被 RimWorld
            // 背后的 CameraDriver 使用。
            currentEvent.Use();
        }
        private void AdjustCanvasOffsetForZoom(
    Vector2 mousePosition,
    float oldZoom,
    float newZoom)
        {
            if (oldZoom <= 0f)
            {
                return;
            }

            Vector2 worldPosition =
                (mousePosition -
                 canvasOffset) /
                oldZoom;

            canvasOffset =
                mousePosition -
                worldPosition *
                newZoom;
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

            HandleTreeZoom(
                viewportRect);

            HandleCanvasPan(
                viewportRect);

            Widgets.BeginGroup(
                viewportRect);

            DrawKnowledgeTree();

            Widgets.EndGroup();
        }

        private void DrawZoomControls(
    Rect viewportRect)
        {
            Rect resetRect =
                new Rect(
                    viewportRect.x + 8f,
                    viewportRect.y + 8f,
                    70f,
                    28f);

            if (Widgets.ButtonText(
                    resetRect,
                    "Reset"))
            {
                zoom =
                    1f;

                canvasOffset =
                    Vector2.zero;
            }


            string zoomText =
                Mathf.RoundToInt(
                    zoom * 100f) +
                "%";

            Rect zoomRect =
                new Rect(
                    resetRect.xMax + 8f,
                    resetRect.y,
                    60f,
                    28f);

            TextAnchor oldAnchor =
                Text.Anchor;

            Text.Anchor =
                TextAnchor.MiddleCenter;

            Widgets.Label(
                zoomRect,
                zoomText);

            Text.Anchor =
                oldAnchor;
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

            float x =
                canvasOffset.x +
                def.treeX *
                zoom;

            float y =
                canvasOffset.y +
                def.treeY *
                zoom;

            float width =
                NodeWidth *
                zoom;

            float height =
                NodeHeight *
                zoom;

            return new Rect(
                x,
                y,
                width,
                height);
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

                    CivilizationKnowledgeState prerequisiteState =
    CivilizationKnowledgeManager
        .GetState(
            prerequisite);

                    bool prerequisiteUnlocked =
                        prerequisiteState != null &&
                        prerequisiteState.Unlocked;

                    DrawConnection(
                        parentRect,
                        childRect,
                        prerequisiteUnlocked);
                }
            }
        }


        private void DrawConnection(
    Rect parentRect,
    Rect childRect,
    bool prerequisiteUnlocked)
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

            Color lineColor;

            if (prerequisiteUnlocked)
            {
                lineColor =
                    new Color(
                        0.70f,
                        0.70f,
                        0.70f,
                        1f);
            }
            else
            {
                lineColor =
                    new Color(
                        0.35f,
                        0.35f,
                        0.35f,
                        1f);
            }

            Widgets.DrawLine(
                start,
                firstCorner,
                lineColor,
                2f);

            Widgets.DrawLine(
                firstCorner,
                secondCorner,
                lineColor,
                2f);

            Widgets.DrawLine(
                secondCorner,
                end,
                lineColor,
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

            Color nodeColor =
                GetNodeStatusColor(
                    state);

            GUI.color =
                nodeColor;

            Widgets.DrawMenuSection(
                rect);

            GUI.color =
                oldColor;

            if (selected)
            {
                Widgets.DrawHighlight(
                    rect);
            }

            DrawNodeStatusMarker(
                rect,
                state);

            TextAnchor oldAnchor =
                Text.Anchor;

            Text.Anchor =
                TextAnchor.MiddleCenter;

            Widgets.Label(
                rect.ContractedBy(
                    8f * zoom),
                label);

            Text.Anchor =
                oldAnchor;

            TooltipHandler.TipRegion(
                rect,
                label +
                "\nStatus: " +
                GetKnowledgeStatusText(state));
        }

        private Color GetNodeStatusColor(
    CivilizationKnowledgeState state)
        {
            if (state == null ||
                !state.Unlocked)
            {
                return new Color(
                    0.45f,
                    0.45f,
                    0.45f,
                    1f);
            }

            if (state.AwaitingReactivation)
            {
                return new Color(
                    0.65f,
                    0.90f,
                    0.90f,
                    1f);
            }

            if (state.Dormant)
            {
                return new Color(
                    0.50f,
                    0.58f,
                    0.68f,
                    1f);
            }

            if (state.Unstable)
            {
                return new Color(
                    0.95f,
                    0.82f,
                    0.45f,
                    1f);
            }

            return Color.white;
        }

        private void DrawNodeStatusMarker(
    Rect rect,
    CivilizationKnowledgeState state)
        {
            string marker =
                GetNodeStatusMarker(
                    state);

            Rect markerRect =
                new Rect(
                    rect.xMax -
                        24f * zoom,
                    rect.y +
                        4f * zoom,
                    20f * zoom,
                    18f * zoom);

            TextAnchor oldAnchor =
                Text.Anchor;

            GameFont oldFont =
                Text.Font;

            Text.Anchor =
                TextAnchor.MiddleCenter;

            Text.Font =
                GameFont.Tiny;

            Widgets.Label(
                markerRect,
                marker);

            Text.Font =
                oldFont;

            Text.Anchor =
                oldAnchor;
        }

        private void DrawZoomIndicator(
    Rect viewportRect)
        {
            string text =
                "Zoom: " +
                Mathf.RoundToInt(
                    zoom * 100f) +
                "%";

            Rect rect =
                new Rect(
                    viewportRect.xMax - 100f,
                    viewportRect.y + 8f,
                    90f,
                    24f);

            TextAnchor oldAnchor =
                Text.Anchor;

            Text.Anchor =
                TextAnchor.MiddleRight;

            Widgets.Label(
                rect,
                text);

            Text.Anchor =
                oldAnchor;
        }

        private string GetNodeStatusMarker(
    CivilizationKnowledgeState state)
        {
            if (state == null ||
                !state.Unlocked)
            {
                return "L";
            }

            if (state.AwaitingReactivation)
            {
                return "R";
            }

            if (state.Dormant)
            {
                return "D";
            }

            if (state.Unstable)
            {
                return "U";
            }

            return "A";
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


            // ============================================================
            // Title
            // ============================================================

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


            // ============================================================
            // General Information
            // ============================================================

            Widgets.Label(
                new Rect(
                    innerRect.x,
                    y,
                    innerRect.width,
                    24f),
                "Status: " +
                GetKnowledgeStatusText(
                    state));

            y += 24f;

            Widgets.Label(
                new Rect(
                    innerRect.x,
                    y,
                    innerRect.width,
                    24f),
                "Category: " +
                def.category);

            y += 32f;


            Widgets.DrawLineHorizontal(
                innerRect.x,
                y,
                innerRect.width);

            y += 12f;


            // ============================================================
            // Prerequisites
            // ============================================================

            y =
                DrawPrerequisiteProgress(
                    innerRect,
                    y,
                    def);


            y += 10f;


            Widgets.DrawLineHorizontal(
                innerRect.x,
                y,
                innerRect.width);

            y += 12f;


            // ============================================================
            // Required Texts
            // ============================================================

            y =
                DrawRequiredTextProgress(
                    innerRect,
                    y,
                    def);


            y += 10f;


            Widgets.DrawLineHorizontal(
                innerRect.x,
                y,
                innerRect.width);

            y += 12f;


            // ============================================================
            // Overall Unlock Requirements
            // ============================================================

            DrawUnlockRequirementSummary(
                innerRect,
                y,
                def);
        }

        private float DrawPrerequisiteProgress(
    Rect innerRect,
    float y,
    CivilizationKnowledgeDef def)
        {
            Widgets.Label(
                new Rect(
                    innerRect.x,
                    y,
                    innerRect.width,
                    25f),
                "Prerequisites");

            y += 27f;


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

                y += 24f;

                return y;
            }


            List<CivilizationKnowledgeDef>
                missingPrerequisites =
                    CivilizationKnowledgeUtility
                        .GetMissingPrerequisites(
                            def);


            int total =
                0;

            int completed =
                0;


            foreach (
                CivilizationKnowledgeDef prerequisite
                in def.prerequisites)
            {
                if (prerequisite == null)
                {
                    continue;
                }

                total++;

                bool satisfied =
                    !missingPrerequisites.Contains(
                        prerequisite);

                if (satisfied)
                {
                    completed++;
                }


                string prefix =
                    satisfied
                        ? "✓ "
                        : "✗ ";


                Widgets.Label(
                    new Rect(
                        innerRect.x + 10f,
                        y,
                        innerRect.width - 10f,
                        22f),
                    prefix +
                    GetNodeLabel(
                        prerequisite));

                y += 22f;
            }


            y += 3f;


            Widgets.Label(
                new Rect(
                    innerRect.x + 10f,
                    y,
                    innerRect.width - 10f,
                    22f),
                "Progress: " +
                completed +
                " / " +
                total);

            y += 24f;


            return y;
        }

        private float DrawRequiredTextProgress(
    Rect innerRect,
    float y,
    CivilizationKnowledgeDef def)
        {
            Widgets.Label(
                new Rect(
                    innerRect.x,
                    y,
                    innerRect.width,
                    25f),
                "Required Texts");

            y += 27f;


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

                y += 24f;

                return y;
            }


            List<LiAIChat.Archive.EarthTextDef>
                missingTexts =
                    CivilizationKnowledgeUtility
                        .GetMissingRequiredTexts(
                            def);


            int total =
                0;

            int recovered =
                0;


            foreach (
                LiAIChat.Archive.EarthTextDef textDef
                in def.requiredTexts)
            {
                if (textDef == null)
                {
                    continue;
                }

                total++;

                bool hasText =
                    !missingTexts.Contains(
                        textDef);

                if (hasText)
                {
                    recovered++;
                }


                string prefix =
                    hasText
                        ? "✓ "
                        : "✗ ";


                Widgets.Label(
                    new Rect(
                        innerRect.x + 10f,
                        y,
                        innerRect.width - 10f,
                        22f),
                    prefix +
                    GetEarthTextLabel(
                        textDef));

                y += 22f;
            }


            y += 3f;


            Widgets.Label(
                new Rect(
                    innerRect.x + 10f,
                    y,
                    innerRect.width - 10f,
                    22f),
                "Progress: " +
                recovered +
                " / " +
                total);

            y += 24f;


            return y;
        }

        private string GetEarthTextLabel(
    LiAIChat.Archive.EarthTextDef textDef)
        {
            if (textDef == null)
            {
                return "";
            }

            if (!string.IsNullOrEmpty(
                    textDef.label))
            {
                return textDef.label;
            }

            return textDef.defName;
        }
        private void DrawUnlockRequirementSummary(
    Rect innerRect,
    float y,
    CivilizationKnowledgeDef def)
        {
            Widgets.Label(
                new Rect(
                    innerRect.x,
                    y,
                    innerRect.width,
                    25f),
                "Unlock Requirements");

            y += 28f;


            bool prerequisitesComplete =
                CivilizationKnowledgeUtility
                    .HasPrerequisites(
                        def);

            bool textsComplete =
                CivilizationKnowledgeUtility
                    .HasRequiredTexts(
                        def);


            string prerequisiteText =
                prerequisitesComplete
                    ? "Complete"
                    : "Incomplete";

            string textRequirementText =
                textsComplete
                    ? "Complete"
                    : "Incomplete";


            Widgets.Label(
                new Rect(
                    innerRect.x + 10f,
                    y,
                    innerRect.width - 10f,
                    22f),
                "Prerequisites: " +
                prerequisiteText);

            y += 22f;


            Widgets.Label(
                new Rect(
                    innerRect.x + 10f,
                    y,
                    innerRect.width - 10f,
                    22f),
                "Required Texts: " +
                textRequirementText);
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