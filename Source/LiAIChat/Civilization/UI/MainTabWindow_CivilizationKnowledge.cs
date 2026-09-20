using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using LiAIChat.Archive;

namespace LiAIChat.Civilization.UI
{
    public class MainTabWindow_CivilizationKnowledge : MainTabWindow
    {
        private enum TreeZoomLevel
        {
            Overview,
            Compact,
            Detailed
        }

        private enum KnowledgeStatusFilter
        {
            All,
            Locked,
            Active,
            GracePeriod,
            Unstable,
            Dormant,
            AwaitingReactivation
        }
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

        private string selectedEarthTextId;

        private string searchText =
            string.Empty;

        private CivilizationKnowledgeCategory?
            categoryFilter;

        private KnowledgeStatusFilter statusFilter =
            KnowledgeStatusFilter.All;

        private Vector2 treeViewportSize =
            Vector2.zero;

        private Vector2 detailScrollPosition =
            Vector2.zero;

        private readonly Dictionary<string, Rect>
            currentNodeRects =
                new Dictionary<string, Rect>();

        private float zoom = 1f;
        private const float MinZoom =
    0.35f;
        private const float MaxZoom = 1.4f;
        private const float ZoomStep = 0.1f;
        private const float FitPadding = 50f;

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
                    260f,
                    35f),
                "Civilization Knowledge");

            Text.Font =
                GameFont.Small;

            float detailPanelWidth =
                300f;

            float gap =
                12f;

            float headerHeight =
                78f;

            DrawNavigationControls(
                new Rect(
                    275f,
                    0f,
                    inRect.width - 275f,
                    34f));

            Rect viewportRect =
                new Rect(
                    0f,
                    headerHeight,
                    inRect.width -
                    detailPanelWidth -
                    gap,
                    inRect.height -
                    headerHeight);

            Rect detailRect =
                new Rect(
                    viewportRect.xMax +
                    gap,
                    headerHeight,
                    detailPanelWidth,
                    inRect.height -
                    headerHeight);
            
            DrawTreeViewport(
                viewportRect);

            DrawNodeDetailPanel(
                detailRect);
        }

        private void DrawNavigationControls(
            Rect rect)
        {
            float x =
                rect.x;

            Rect searchRect =
                new Rect(
                    x,
                    rect.y + 2f,
                    190f,
                    28f);

            searchText =
                Widgets.TextField(
                    searchRect,
                    searchText ?? string.Empty);

            x = searchRect.xMax + 6f;

            if (Widgets.ButtonText(
                    new Rect(
                        x,
                        rect.y + 2f,
                        58f,
                        28f),
                    "Find"))
            {
                SelectFirstSearchResult();
            }

            x += 64f;

            string categoryLabel =
                categoryFilter.HasValue
                    ? categoryFilter.Value.ToString()
                    : "All categories";

            if (Widgets.ButtonText(
                    new Rect(
                        x,
                        rect.y + 2f,
                        135f,
                        28f),
                    categoryLabel))
            {
                OpenCategoryFilterMenu();
            }

            x += 141f;

            if (Widgets.ButtonText(
                    new Rect(
                        x,
                        rect.y + 2f,
                        145f,
                        28f),
                    GetStatusFilterLabel(
                        statusFilter)))
            {
                OpenStatusFilterMenu();
            }

            x += 151f;

            if (Widgets.ButtonText(
                    new Rect(
                        x,
                        rect.y + 2f,
                        78f,
                        28f),
                    "Overview"))
            {
                selectedNodeId =
                    null;

                selectedEarthTextId =
                    null;

                detailScrollPosition =
                    Vector2.zero;
            }
        }

        private void OpenCategoryFilterMenu()
        {
            List<FloatMenuOption> options =
                new List<FloatMenuOption>();

            options.Add(
                new FloatMenuOption(
                    "All categories",
                    delegate
                    {
                        categoryFilter = null;
                        OnFilterChanged();
                    }));

            foreach (CivilizationKnowledgeCategory category
                in System.Enum.GetValues(
                    typeof(CivilizationKnowledgeCategory)))
            {
                CivilizationKnowledgeCategory selectedCategory =
                    category;

                options.Add(
                    new FloatMenuOption(
                        category.ToString(),
                        delegate
                        {
                            categoryFilter =
                                selectedCategory;

                            OnFilterChanged();
                        }));
            }

            Find.WindowStack.Add(
                new FloatMenu(options));
        }

        private void OpenStatusFilterMenu()
        {
            List<FloatMenuOption> options =
                new List<FloatMenuOption>();

            foreach (KnowledgeStatusFilter filter
                in System.Enum.GetValues(
                    typeof(KnowledgeStatusFilter)))
            {
                KnowledgeStatusFilter selectedFilter =
                    filter;

                options.Add(
                    new FloatMenuOption(
                        GetStatusFilterLabel(filter),
                        delegate
                        {
                            statusFilter =
                                selectedFilter;

                            OnFilterChanged();
                        }));
            }

            Find.WindowStack.Add(
                new FloatMenu(options));
        }

        private string GetStatusFilterLabel(
            KnowledgeStatusFilter filter)
        {
            switch (filter)
            {
                case KnowledgeStatusFilter.GracePeriod:
                    return "Grace period";

                case KnowledgeStatusFilter.AwaitingReactivation:
                    return "Awaiting reactivation";

                case KnowledgeStatusFilter.All:
                    return "All statuses";

                default:
                    return filter.ToString();
            }
        }

        private void OnFilterChanged()
        {
            selectedNodeId =
                null;

            selectedEarthTextId =
                null;

            detailScrollPosition =
                Vector2.zero;

            canvasInitialized =
                false;
        }

        private List<CivilizationKnowledgeDef>
            GetFilteredKnowledgeDefs()
        {
            List<CivilizationKnowledgeDef> result =
                new List<CivilizationKnowledgeDef>();

            List<CivilizationKnowledgeDef> defs =
                DefDatabase<CivilizationKnowledgeDef>
                    .AllDefsListForReading;

            if (defs == null)
            {
                return result;
            }

            foreach (CivilizationKnowledgeDef def
                in defs)
            {
                if (def == null)
                {
                    continue;
                }

                if (categoryFilter.HasValue &&
                    def.category !=
                        categoryFilter.Value)
                {
                    continue;
                }

                if (!MatchesStatusFilter(def))
                {
                    continue;
                }

                result.Add(def);
            }

            return result;
        }

        private bool MatchesStatusFilter(
            CivilizationKnowledgeDef def)
        {
            if (statusFilter ==
                KnowledgeStatusFilter.All)
            {
                return true;
            }

            CivilizationKnowledgeState state =
                CivilizationKnowledgeManager
                    .GetState(def);

            switch (statusFilter)
            {
                case KnowledgeStatusFilter.Locked:
                    return state == null ||
                        !state.Unlocked;

                case KnowledgeStatusFilter.Active:
                    return state != null &&
                        state.Unlocked &&
                        !state.Unstable &&
                        !state.Dormant &&
                        !state.AwaitingReactivation &&
                        !CivilizationKnowledgeUtility
                            .IsUnlockedButIncomplete(def);

                case KnowledgeStatusFilter.GracePeriod:
                    return state != null &&
                        state.Unlocked &&
                        !state.Unstable &&
                        !state.Dormant &&
                        !state.AwaitingReactivation &&
                        CivilizationKnowledgeUtility
                            .IsUnlockedButIncomplete(def);

                case KnowledgeStatusFilter.Unstable:
                    return state != null &&
                        state.Unlocked &&
                        state.Unstable;

                case KnowledgeStatusFilter.Dormant:
                    return state != null &&
                        state.Unlocked &&
                        state.Dormant;

                case KnowledgeStatusFilter.AwaitingReactivation:
                    return state != null &&
                        state.Unlocked &&
                        state.AwaitingReactivation;

                default:
                    return true;
            }
        }

        private void SelectFirstSearchResult()
        {
            string query =
                searchText == null
                    ? string.Empty
                    : searchText.Trim();

            if (query.Length == 0)
            {
                return;
            }

            foreach (CivilizationKnowledgeDef def
                in GetFilteredKnowledgeDefs())
            {
                if (!KnowledgeMatchesSearch(
                        def,
                        query))
                {
                    continue;
                }

                SelectAndCenterNode(def);
                return;
            }

            Messages.Message(
                "No civilization knowledge matches \"" +
                query +
                "\" in the current filters.",
                MessageTypeDefOf.RejectInput);
        }

        private bool KnowledgeMatchesSearch(
            CivilizationKnowledgeDef def,
            string query)
        {
            if (def == null)
            {
                return false;
            }

            return ContainsIgnoreCase(
                    def.defName,
                    query) ||
                ContainsIgnoreCase(
                    def.label,
                    query) ||
                ContainsIgnoreCase(
                    def.title,
                    query) ||
                ContainsIgnoreCase(
                    def.titleChinese,
                    query) ||
                ContainsIgnoreCase(
                    def.description,
                    query) ||
                ContainsIgnoreCase(
                    def.descriptionChinese,
                    query);
        }

        private bool ContainsIgnoreCase(
            string value,
            string query)
        {
            return !string.IsNullOrEmpty(value) &&
                value.IndexOf(
                    query,
                    System.StringComparison
                        .OrdinalIgnoreCase) >= 0;
        }

        private void SelectAndCenterNode(
            CivilizationKnowledgeDef def)
        {
            if (def == null)
            {
                return;
            }

            bool hiddenByCategory =
                categoryFilter.HasValue &&
                def.category !=
                    categoryFilter.Value;

            bool hiddenByStatus =
                !MatchesStatusFilter(def);

            if (hiddenByCategory ||
                hiddenByStatus)
            {
                categoryFilter =
                    null;

                statusFilter =
                    KnowledgeStatusFilter.All;
            }

            selectedNodeId =
                def.defName;

            selectedEarthTextId =
                null;

            detailScrollPosition =
                Vector2.zero;

            if (zoom < 0.8f)
            {
                zoom = 0.8f;
            }

            if (treeViewportSize.x <= 0f ||
                treeViewportSize.y <= 0f)
            {
                return;
            }

            Vector2 nodeCenter =
                new Vector2(
                    def.treeX +
                        NodeWidth * 0.5f,
                    def.treeY +
                        NodeHeight * 0.5f);

            canvasOffset =
                treeViewportSize * 0.5f -
                nodeCenter * zoom;
        }

        private Rect GetKnowledgeTreeBounds()
        {
            List<CivilizationKnowledgeDef> defs =
                GetFilteredKnowledgeDefs();

            if (defs == null ||
                defs.Count == 0)
            {
                return Rect.zero;
            }

            bool hasNode =
                false;

            float minX =
                0f;

            float minY =
                0f;

            float maxX =
                0f;

            float maxY =
                0f;

            foreach (
                CivilizationKnowledgeDef def
                in defs)
            {
                if (def == null)
                {
                    continue;
                }

                float left =
                    def.treeX;

                float top =
                    def.treeY;

                float right =
                    def.treeX +
                    NodeWidth;

                float bottom =
                    def.treeY +
                    NodeHeight;

                if (!hasNode)
                {
                    minX =
                        left;

                    minY =
                        top;

                    maxX =
                        right;

                    maxY =
                        bottom;

                    hasNode =
                        true;

                    continue;
                }

                minX =
                    Mathf.Min(
                        minX,
                        left);

                minY =
                    Mathf.Min(
                        minY,
                        top);

                maxX =
                    Mathf.Max(
                        maxX,
                        right);

                maxY =
                    Mathf.Max(
                        maxY,
                        bottom);
            }

            if (!hasNode)
            {
                return Rect.zero;
            }

            return Rect.MinMaxRect(
                minX,
                minY,
                maxX,
                maxY);
        }

        private TreeZoomLevel GetTreeZoomLevel()
        {
            if (zoom < 0.50f)
            {
                return TreeZoomLevel.Overview;
            }

            if (zoom < 0.80f)
            {
                return TreeZoomLevel.Compact;
            }

            return TreeZoomLevel.Detailed;
        }

        private void FitTreeToViewport(
    Rect viewportRect)
        {
            Rect treeBounds =
                GetKnowledgeTreeBounds();

            if (treeBounds.width <= 0f ||
                treeBounds.height <= 0f)
            {
                return;
            }

            float availableWidth =
                viewportRect.width -
                FitPadding * 2f;

            float availableHeight =
                viewportRect.height -
                FitPadding * 2f;

            if (availableWidth <= 0f ||
                availableHeight <= 0f)
            {
                return;
            }

            float zoomX =
                availableWidth /
                treeBounds.width;

            float zoomY =
                availableHeight /
                treeBounds.height;

            float fitZoom =
                Mathf.Min(
                    zoomX,
                    zoomY);

            zoom =
                Mathf.Clamp(
                    fitZoom,
                    MinZoom,
                    MaxZoom);

            Vector2 treeCenter =
                treeBounds.center;

            Vector2 viewportCenter =
                new Vector2(
                    viewportRect.width /
                        2f,
                    viewportRect.height /
                        2f);

            canvasOffset =
                viewportCenter -
                treeCenter *
                zoom;
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

            if (GetTreeControlsRect(
                    viewportRect)
                .Contains(
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
            treeViewportSize =
                viewportRect.size;

            Widgets.DrawMenuSection(
                viewportRect);

            if (!canvasInitialized)
            {
                FitTreeToViewport(
                    viewportRect);

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

            if (currentNodeRects.Count == 0)
            {
                TextAnchor oldAnchor =
                    Text.Anchor;

                Text.Anchor =
                    TextAnchor.MiddleCenter;

                Widgets.Label(
                    new Rect(
                        0f,
                        0f,
                        viewportRect.width,
                        viewportRect.height),
                    "No knowledge nodes match the current filters.");

                Text.Anchor =
                    oldAnchor;
            }

            Widgets.EndGroup();

            DrawTreeControls(
                viewportRect);
        }

        private void DrawTreeControls(
    Rect viewportRect)
        {
            Rect fitRect =
                new Rect(
                    viewportRect.x + 10f,
                    viewportRect.y + 10f,
                    90f,
                    28f);

            if (Widgets.ButtonText(
                    fitRect,
                    "Fit Tree"))
            {
                FitTreeToViewport(
                    viewportRect);
            }

            string zoomText =
                Mathf.RoundToInt(
                    zoom * 100f) +
                "% " +
                GetTreeZoomLevel();

            Rect zoomRect =
                new Rect(
                    fitRect.xMax + 8f,
                    fitRect.y,
                    120f,
                    fitRect.height);

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

        private Rect GetTreeControlsRect(
    Rect viewportRect)
        {
            return new Rect(
                viewportRect.x + 6f,
                viewportRect.y + 6f,
                235f,
                38f);
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
                Rect controlsRect =
                    GetTreeControlsRect(
                        viewportRect);

                if (controlsRect.Contains(
                        mousePosition))
                {
                    return;
                }

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

                selectedEarthTextId =
                    null;

                detailScrollPosition =
                    Vector2.zero;

                return;
            }

            if (selectedNodeId != nodeId)
            {
                detailScrollPosition =
                    Vector2.zero;
            }

            selectedNodeId =
                nodeId;

            selectedEarthTextId =
                null;
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
                GetFilteredKnowledgeDefs();

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

            float lineWidth =
    GetConnectionLineWidth();

            Widgets.DrawLine(
                start,
                firstCorner,
                lineColor,
                lineWidth);

            Widgets.DrawLine(
                firstCorner,
                secondCorner,
                lineColor,
                lineWidth);

            Widgets.DrawLine(
                secondCorner,
                end,
                lineColor,
                lineWidth);
        }

        private string GetNodeTooltip(
    string label,
    CivilizationKnowledgeDef def,
    CivilizationKnowledgeState state)
        {
            return label +
                "\nStatus: " +
                GetKnowledgeStatusText(
                    def,
                    state);
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
                    def,
                    GetNodeLabel(def),
                    selected,
                    state);
            }
        }


        private void DrawNode(
    Rect rect,
    CivilizationKnowledgeDef def,
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

            TreeZoomLevel zoomLevel =
                GetTreeZoomLevel();

            switch (zoomLevel)
            {
                case TreeZoomLevel.Detailed:

                    DrawDetailedNodeContent(
                        rect,
                        label,
                        state);

                    break;


                case TreeZoomLevel.Compact:

                    DrawCompactNodeContent(
                        rect,
                        label);

                    break;


                case TreeZoomLevel.Overview:

                    DrawOverviewNodeContent(
                        rect,
                        state);

                    break;
            }

            TooltipHandler.TipRegion(
                rect,
                GetNodeTooltip(
                    label,
                    def,
                    state));
        }

        private void DrawDetailedNodeContent(
    Rect rect,
    string label,
    CivilizationKnowledgeState state)
        {
            DrawNodeStatusMarker(
                rect,
                state);

            TextAnchor oldAnchor =
                Text.Anchor;

            GameFont oldFont =
                Text.Font;

            Text.Anchor =
                TextAnchor.MiddleCenter;

            Text.Font =
                GameFont.Small;

            Rect labelRect =
                rect.ContractedBy(
                    8f * zoom);

            Widgets.Label(
                labelRect,
                label);

            Text.Font =
                oldFont;

            Text.Anchor =
                oldAnchor;
        }

        private void DrawCompactNodeContent(
    Rect rect,
    string label)
        {
            TextAnchor oldAnchor =
                Text.Anchor;

            GameFont oldFont =
                Text.Font;

            Text.Anchor =
                TextAnchor.MiddleCenter;

            Text.Font =
                GameFont.Tiny;

            Rect labelRect =
                rect.ContractedBy(
                    4f);

            Widgets.Label(
                labelRect,
                label);

            Text.Font =
                oldFont;

            Text.Anchor =
                oldAnchor;
        }

        private void DrawOverviewNodeContent(
    Rect rect,
    CivilizationKnowledgeState state)
        {
            string marker =
                GetNodeStatusMarker(
                    state);

            TextAnchor oldAnchor =
                Text.Anchor;

            GameFont oldFont =
                Text.Font;

            Text.Anchor =
                TextAnchor.MiddleCenter;

            Text.Font =
                GameFont.Tiny;

            Widgets.Label(
                rect,
                marker);

            Text.Font =
                oldFont;

            Text.Anchor =
                oldAnchor;
        }

        private float GetConnectionLineWidth()
        {
            TreeZoomLevel zoomLevel =
                GetTreeZoomLevel();

            switch (zoomLevel)
            {
                case TreeZoomLevel.Overview:
                    return 1f;

                case TreeZoomLevel.Compact:
                    return 1.5f;

                default:
                    return 2f;
            }
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

            if (!string.IsNullOrEmpty(
                    selectedEarthTextId))
            {
                DrawEarthTextDetailPanel(
                    innerRect);

                return;
            }

            if (string.IsNullOrEmpty(
                    selectedNodeId))
            {
                DrawCivilizationOverview(
                    innerRect);

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

            Rect scrollOutRect =
                innerRect;

            float viewWidth =
                Mathf.Max(
                    1f,
                    scrollOutRect.width - 16f);

            Rect viewRect =
                new Rect(
                    0f,
                    0f,
                    viewWidth,
                    CalculateDetailContentHeight(
                        viewWidth,
                        def,
                        state));

            Widgets.BeginScrollView(
                scrollOutRect,
                ref detailScrollPosition,
                viewRect);

            float y =
                viewRect.y;


            // ============================================================
            // Title
            // ============================================================

            Text.Font =
                GameFont.Medium;

            Widgets.Label(
                new Rect(
                    viewRect.x,
                    y,
                    viewRect.width,
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
                    viewRect.x,
                    y,
                    viewRect.width,
                    24f),
                "Status: " +
                GetKnowledgeStatusText(
                    def,
                    state));

            y += 24f;

            Widgets.Label(
                new Rect(
                    viewRect.x,
                    y,
                    viewRect.width,
                    24f),
                "Category: " +
                def.category);

            y += 32f;

            y =
                DrawStatusExplanation(
                    viewRect,
                    y,
                    def,
                    state);

            y += 8f;

            y =
                DrawKnowledgeDescription(
                    viewRect,
                    y,
                    def);

            y += 12f;

            y =
                DrawGameplayEffects(
                    viewRect,
                    y,
                    def);

            y += 12f;


            Widgets.DrawLineHorizontal(
                viewRect.x,
                y,
                viewRect.width);

            y += 12f;


            // ============================================================
            // Prerequisites
            // ============================================================

            y =
                DrawPrerequisiteProgress(
                    viewRect,
                    y,
                    def);


            y += 10f;


            Widgets.DrawLineHorizontal(
                viewRect.x,
                y,
                viewRect.width);

            y += 12f;


            // ============================================================
            // Required Texts
            // ============================================================

            y =
                DrawRequiredTextProgress(
                    viewRect,
                    y,
                    def);


            y += 10f;


            Widgets.DrawLineHorizontal(
                viewRect.x,
                y,
                viewRect.width);

            y += 12f;


            // ============================================================
            // Overall Unlock Requirements
            // ============================================================

            y =
                DrawUnlockRequirementSummary(
                    viewRect,
                    y,
                    def);

            y += 12f;

            DrawReactivationControls(
                viewRect,
                y,
                def,
                state);

            Widgets.EndScrollView();
        }

        private void DrawCivilizationOverview(
            Rect rect)
        {
            List<CivilizationKnowledgeDef> defs =
                DefDatabase<CivilizationKnowledgeDef>
                    .AllDefsListForReading;

            List<ColonyLibraryEntry> library =
                ColonyLibraryIndex.Build();

            float viewHeight =
                390f +
                library.Count * 30f +
                System.Enum.GetValues(
                    typeof(CivilizationKnowledgeCategory))
                    .Length * 24f;

            Rect viewRect =
                new Rect(
                    0f,
                    0f,
                    Mathf.Max(
                        1f,
                        rect.width - 16f),
                    Mathf.Max(
                        rect.height,
                        viewHeight));

            Widgets.BeginScrollView(
                rect,
                ref detailScrollPosition,
                viewRect);

            float y =
                0f;

            Text.Font =
                GameFont.Medium;

            Widgets.Label(
                new Rect(
                    0f,
                    y,
                    viewRect.width,
                    32f),
                "Civilization Overview");

            y += 40f;

            Text.Font =
                GameFont.Small;

            int total =
                0;

            int reconstructed =
                0;

            int active =
                0;

            if (defs != null)
            {
                foreach (CivilizationKnowledgeDef def
                    in defs)
                {
                    if (def == null)
                    {
                        continue;
                    }

                    total++;

                    if (CivilizationKnowledgeManager
                        .IsUnlocked(def))
                    {
                        reconstructed++;
                    }

                    if (CivilizationKnowledgeManager
                        .IsActive(def))
                    {
                        active++;
                    }
                }
            }

            y = DrawOverviewProgressBar(
                viewRect,
                y,
                "Reconstructed",
                reconstructed,
                total);

            y = DrawOverviewProgressBar(
                viewRect,
                y,
                "Currently active",
                active,
                total);

            y += 12f;

            Widgets.DrawLineHorizontal(
                0f,
                y,
                viewRect.width);

            y += 12f;

            Widgets.Label(
                new Rect(
                    0f,
                    y,
                    viewRect.width,
                    24f),
                "Category Progress");

            y += 28f;

            foreach (CivilizationKnowledgeCategory category
                in System.Enum.GetValues(
                    typeof(CivilizationKnowledgeCategory)))
            {
                int categoryTotal =
                    0;

                int categoryUnlocked =
                    0;

                if (defs != null)
                {
                    foreach (CivilizationKnowledgeDef def
                        in defs)
                    {
                        if (def == null ||
                            def.category != category)
                        {
                            continue;
                        }

                        categoryTotal++;

                        if (CivilizationKnowledgeManager
                            .IsUnlocked(def))
                        {
                            categoryUnlocked++;
                        }
                    }
                }

                if (categoryTotal == 0)
                {
                    continue;
                }

                y = DrawDetailLine(
                    viewRect,
                    y,
                    category +
                    ": " +
                    categoryUnlocked +
                    " / " +
                    categoryTotal);
            }

            y += 10f;

            Widgets.DrawLineHorizontal(
                0f,
                y,
                viewRect.width);

            y += 12f;

            Widgets.Label(
                new Rect(
                    0f,
                    y,
                    viewRect.width,
                    24f),
                "Colony Library — " +
                library.Count +
                " recovered works");

            y += 28f;

            if (library.Count == 0)
            {
                y = DrawDetailLine(
                    viewRect,
                    y,
                    "No identified Earth texts are currently stored at a player home.");
            }
            else
            {
                foreach (ColonyLibraryEntry entry
                    in library)
                {
                    if (entry == null ||
                        entry.Text == null)
                    {
                        continue;
                    }

                    if (Widgets.ButtonText(
                            new Rect(
                                0f,
                                y,
                                viewRect.width,
                                26f),
                            GetEarthTextLabel(
                                entry.Text) +
                            "  ×" +
                            entry.CopyCount))
                    {
                        selectedEarthTextId =
                            entry.Text.defName;

                        detailScrollPosition =
                            Vector2.zero;
                    }

                    y += 30f;
                }
            }

            Widgets.EndScrollView();
        }

        private float DrawOverviewProgressBar(
            Rect rect,
            float y,
            string label,
            int current,
            int total)
        {
            Widgets.Label(
                new Rect(
                    rect.x,
                    y,
                    rect.width,
                    22f),
                label +
                ": " +
                current +
                " / " +
                total);

            y += 22f;

            float fillPercent =
                total <= 0
                    ? 0f
                    : current /
                        (float)total;

            Widgets.FillableBar(
                new Rect(
                    rect.x,
                    y,
                    rect.width,
                    12f),
                Mathf.Clamp01(
                    fillPercent));

            return y + 20f;
        }

        private void DrawEarthTextDetailPanel(
            Rect rect)
        {
            EarthTextDef textDef =
                DefDatabase<EarthTextDef>
                    .GetNamedSilentFail(
                        selectedEarthTextId);

            if (textDef == null)
            {
                selectedEarthTextId =
                    null;

                return;
            }

            List<CivilizationKnowledgeDef> related =
                GetKnowledgeDefsForText(
                    textDef);

            float descriptionHeight =
                string.IsNullOrWhiteSpace(
                    textDef.shortDescription)
                    ? 0f
                    : Text.CalcHeight(
                        textDef.shortDescription,
                        rect.width - 16f);

            Rect viewRect =
                new Rect(
                    0f,
                    0f,
                    Mathf.Max(
                        1f,
                        rect.width - 16f),
                    Mathf.Max(
                        rect.height,
                        390f +
                        descriptionHeight +
                        related.Count * 34f));

            Widgets.BeginScrollView(
                rect,
                ref detailScrollPosition,
                viewRect);

            float y =
                0f;

            if (Widgets.ButtonText(
                    new Rect(
                        0f,
                        y,
                        70f,
                        26f),
                    "Back"))
            {
                selectedEarthTextId =
                    null;

                detailScrollPosition =
                    Vector2.zero;
            }

            y += 36f;

            Text.Font =
                GameFont.Medium;

            string title =
                !string.IsNullOrWhiteSpace(
                    textDef.title)
                    ? textDef.title
                    : textDef.label;

            float titleHeight =
                Mathf.Max(
                    32f,
                    Text.CalcHeight(
                        title,
                        viewRect.width));

            Widgets.Label(
                new Rect(
                    0f,
                    y,
                    viewRect.width,
                    titleHeight),
                title);

            y += titleHeight + 4f;

            Text.Font =
                GameFont.Small;

            if (!string.IsNullOrWhiteSpace(
                    textDef.titleChinese) &&
                textDef.titleChinese != title)
            {
                y = DrawDetailLine(
                    viewRect,
                    y,
                    textDef.titleChinese);
            }

            y = DrawDetailLine(
                viewRect,
                y,
                "Author: " +
                (string.IsNullOrWhiteSpace(
                    textDef.author)
                    ? "Unknown"
                    : textDef.author));

            y = DrawDetailLine(
                viewRect,
                y,
                "Date: " +
                textDef.YearDisplay);

            y = DrawDetailLine(
                viewRect,
                y,
                "Library copies: " +
                ColonyLibrary.GetCopyCount(
                    textDef));

            y = DrawDetailLine(
                viewRect,
                y,
                "Topic: " +
                (string.IsNullOrWhiteSpace(
                    textDef.primaryTopicId)
                    ? "Unclassified"
                    : textDef.primaryTopicId));

            if (!string.IsNullOrWhiteSpace(
                    textDef.shortDescription))
            {
                y += 10f;

                float height =
                    Text.CalcHeight(
                        textDef.shortDescription,
                        viewRect.width);

                Widgets.Label(
                    new Rect(
                        0f,
                        y,
                        viewRect.width,
                        height),
                    textDef.shortDescription);

                y += height;
            }

            y += 14f;

            Widgets.DrawLineHorizontal(
                0f,
                y,
                viewRect.width);

            y += 12f;

            Widgets.Label(
                new Rect(
                    0f,
                    y,
                    viewRect.width,
                    24f),
                "Required By");

            y += 28f;

            if (related.Count == 0)
            {
                y = DrawDetailLine(
                    viewRect,
                    y,
                    "No civilization knowledge node currently requires this text.");
            }
            else
            {
                foreach (CivilizationKnowledgeDef def
                    in related)
                {
                    if (Widgets.ButtonText(
                            new Rect(
                                0f,
                                y,
                                viewRect.width,
                                28f),
                            GetNodeLabel(def)))
                    {
                        SelectAndCenterNode(def);
                    }

                    y += 34f;
                }
            }

            Widgets.EndScrollView();
        }

        private List<CivilizationKnowledgeDef>
            GetKnowledgeDefsForText(
                EarthTextDef textDef)
        {
            List<CivilizationKnowledgeDef> result =
                new List<CivilizationKnowledgeDef>();

            if (textDef == null)
            {
                return result;
            }

            List<CivilizationKnowledgeDef> defs =
                DefDatabase<CivilizationKnowledgeDef>
                    .AllDefsListForReading;

            if (defs == null)
            {
                return result;
            }

            foreach (CivilizationKnowledgeDef def
                in defs)
            {
                if (def == null ||
                    def.requiredTexts == null)
                {
                    continue;
                }

                if (def.requiredTexts.Contains(
                        textDef))
                {
                    result.Add(def);
                }
            }

            return result;
        }

        private float CalculateDetailContentHeight(
            float width,
            CivilizationKnowledgeDef def,
            CivilizationKnowledgeState state)
        {
            int prerequisiteCount =
                def != null &&
                def.prerequisites != null
                    ? def.prerequisites.Count
                    : 0;

            int requiredTextCount =
                def != null &&
                def.requiredTexts != null
                    ? def.requiredTexts.Count
                    : 0;

            float descriptionHeight =
                def != null &&
                !string.IsNullOrWhiteSpace(
                    def.description)
                    ? Text.CalcHeight(
                        def.description,
                        width)
                    : 0f;

            float stateHeight =
                state != null &&
                state.Unlocked &&
                state.MissingSinceTick >= 0
                    ? 92f
                    : 48f;

            int effectCount =
                CivilizationKnowledgeEffectUtility
                    .GetEffectDescriptions(def)
                    .Count;

            float effectHeight =
                effectCount > 0
                    ? 36f +
                        effectCount * 24f +
                        (CivilizationKnowledgeManager
                            .IsActive(def)
                                ? 0f
                                : 24f)
                    : 0f;

            return Mathf.Max(
                640f +
                descriptionHeight +
                prerequisiteCount * 24f +
                requiredTextCount * 38f +
                stateHeight +
                effectHeight,
                680f);
        }

        private float DrawKnowledgeDescription(
            Rect rect,
            float y,
            CivilizationKnowledgeDef def)
        {
            Widgets.Label(
                new Rect(
                    rect.x,
                    y,
                    rect.width,
                    25f),
                "Description");

            y += 27f;

            string description =
                def == null
                    ? null
                    : def.description;

            if (string.IsNullOrWhiteSpace(
                    description))
            {
                description =
                    "No description available.";
            }

            float height =
                Mathf.Max(
                    22f,
                    Text.CalcHeight(
                        description,
                        rect.width - 10f));

            Widgets.Label(
                new Rect(
                    rect.x + 10f,
                    y,
                    rect.width - 10f,
                    height),
                description);

            return y + height;
        }

        private float DrawGameplayEffects(
            Rect rect,
            float y,
            CivilizationKnowledgeDef def)
        {
            List<string> descriptions =
                CivilizationKnowledgeEffectUtility
                    .GetEffectDescriptions(def);

            if (descriptions.Count == 0)
            {
                return y;
            }

            Widgets.Label(
                new Rect(
                    rect.x,
                    y,
                    rect.width,
                    25f),
                "Gameplay Effects");

            y += 27f;

            foreach (string description
                in descriptions)
            {
                y = DrawDetailLine(
                    rect,
                    y,
                    "• " + description);
            }

            if (!CivilizationKnowledgeManager
                    .IsActive(def))
            {
                y = DrawDetailLine(
                    rect,
                    y,
                    "Effects are inactive until this knowledge is Active.");
            }

            return y;
        }

        private float DrawStatusExplanation(
            Rect rect,
            float y,
            CivilizationKnowledgeDef def,
            CivilizationKnowledgeState state)
        {
            string explanation;

            if (state == null ||
                !state.Unlocked)
            {
                bool hasPrerequisites =
                    CivilizationKnowledgeUtility
                        .HasPrerequisites(def);

                bool hasTexts =
                    CivilizationKnowledgeUtility
                        .HasRequiredTexts(def);

                if (!hasPrerequisites &&
                    !hasTexts)
                {
                    explanation =
                        "Locked: prerequisites and required texts are missing.";
                }
                else if (!hasPrerequisites)
                {
                    explanation =
                        "Locked: one or more prerequisite knowledge nodes are missing.";
                }
                else if (!hasTexts)
                {
                    explanation =
                        "Locked: recover the missing required texts.";
                }
                else
                {
                    explanation =
                        "Ready to be reconstructed.";
                }
            }
            else if (state.AwaitingReactivation)
            {
                explanation =
                    "The required texts have been restored. Reactivation is required before this knowledge becomes active again.";
            }
            else if (state.Dormant)
            {
                explanation =
                    "Dormant: required texts have been absent beyond the dormancy threshold.";
            }
            else if (state.Unstable)
            {
                explanation =
                    "Unstable: required texts are missing and the grace period has expired.";
            }
            else if (CivilizationKnowledgeUtility
                .IsUnlockedButIncomplete(def))
            {
                explanation =
                    "Grace period: required texts are missing, but this knowledge remains temporarily active.";
            }
            else
            {
                explanation =
                    "Active: the colony library currently supports this knowledge.";
            }

            float explanationHeight =
                Mathf.Max(
                    22f,
                    Text.CalcHeight(
                        explanation,
                        rect.width));

            Widgets.Label(
                new Rect(
                    rect.x,
                    y,
                    rect.width,
                    explanationHeight),
                explanation);

            y += explanationHeight;

            if (state == null ||
                !state.Unlocked ||
                state.MissingSinceTick < 0)
            {
                return y;
            }

            float missingDays =
                CivilizationKnowledgeUtility
                    .GetMissingDurationDays(def);

            float graceRemaining =
                Mathf.Max(
                    0f,
                    def.gracePeriodDays -
                    missingDays);

            float dormantRemaining =
                Mathf.Max(
                    0f,
                    def.dormantAfterDays -
                    missingDays);

            y += 4f;

            y = DrawDetailLine(
                rect,
                y,
                "Required texts missing for " +
                missingDays.ToString("0.0") +
                " days.");

            y = DrawDetailLine(
                rect,
                y,
                "Grace remaining: " +
                graceRemaining.ToString("0.0") +
                " days.");

            y = DrawDetailLine(
                rect,
                y,
                "Dormancy in: " +
                dormantRemaining.ToString("0.0") +
                " days.");

            return y;
        }

        private float DrawDetailLine(
            Rect rect,
            float y,
            string text)
        {
            Widgets.Label(
                new Rect(
                    rect.x + 10f,
                    y,
                    rect.width - 10f,
                    22f),
                text);

            return y + 22f;
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


                if (Widgets.ButtonText(
                        new Rect(
                            innerRect.x + 10f,
                            y,
                            innerRect.width - 10f,
                            26f),
                        prefix +
                        GetNodeLabel(
                            prerequisite)))
                {
                    SelectAndCenterNode(
                        prerequisite);
                }

                y += 30f;
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

                int copyCount =
                    LiAIChat.Archive.ColonyLibrary
                        .GetCopyCount(
                            textDef);

                if (hasText)
                {
                    recovered++;
                }


                string prefix =
                    hasText
                        ? "✓ "
                        : "✗ ";

                string ownership =
                    hasText
                        ? " — Owned ×" +
                            copyCount
                        : " — Missing";


                if (Widgets.ButtonText(
                        new Rect(
                            innerRect.x + 10f,
                            y,
                            innerRect.width - 10f,
                            32f),
                        prefix +
                        GetEarthTextLabel(
                            textDef) +
                        ownership))
                {
                    selectedEarthTextId =
                        textDef.defName;

                    detailScrollPosition =
                        Vector2.zero;
                }

                y += 38f;
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

            string title =
                !string.IsNullOrWhiteSpace(
                    textDef.title)
                    ? textDef.title
                    : (!string.IsNullOrWhiteSpace(
                        textDef.label)
                        ? textDef.label
                        : textDef.defName);

            if (string.IsNullOrWhiteSpace(
                    textDef.author))
            {
                return title;
            }

            return textDef.author +
                " — " +
                title;
        }
        private float DrawUnlockRequirementSummary(
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

            return y + 22f;
        }

        private void DrawReactivationControls(
            Rect rect,
            float y,
            CivilizationKnowledgeDef def,
            CivilizationKnowledgeState state)
        {
            if (state == null ||
                !state.AwaitingReactivation)
            {
                return;
            }

            bool canReactivate =
                CivilizationKnowledgeUtility
                    .CanReactivate(def);

            if (!canReactivate)
            {
                Widgets.Label(
                    new Rect(
                        rect.x,
                        y,
                        rect.width,
                        44f),
                    "Restore all required texts before reactivation.");

                return;
            }

            if (!Widgets.ButtonText(
                    new Rect(
                        rect.x,
                        y,
                        rect.width,
                        32f),
                    "Reactivate Knowledge"))
            {
                return;
            }

            if (CivilizationKnowledgeManager
                .Reactivate(def))
            {
                Messages.Message(
                    "Civilization knowledge reactivated: " +
                    GetNodeLabel(def),
                    MessageTypeDefOf.PositiveEvent);
            }
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
            CivilizationKnowledgeDef def,
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

            if (CivilizationKnowledgeUtility
                .IsUnlockedButIncomplete(def))
            {
                return "Grace Period";
            }

            return "Active";
        }
    }
}
