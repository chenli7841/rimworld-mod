using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace LiAIChat.Civilization.UI
{
    public class Window_CivilizationKnowledgeTree : Window
    {
        private Vector2 scrollPosition = Vector2.zero;

        public override Vector2 InitialSize
        {
            get
            {
                return new Vector2(1000f, 720f);
            }
        }

        public Window_CivilizationKnowledgeTree()
        {
            doCloseX = true;
            draggable = true;
            resizeable = true;
            absorbInputAroundWindow = false;
        }

        public override void DoWindowContents(Rect inRect)
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

            Rect canvasOuterRect =
                new Rect(
                    0f,
                    45f,
                    inRect.width,
                    inRect.height - 45f);

            DrawTreeCanvas(canvasOuterRect);
        }

        private void DrawTreeCanvas(
            Rect outerRect)
        {
            List<CivilizationKnowledgeDef> defs =
                DefDatabase<CivilizationKnowledgeDef>
                    .AllDefsListForReading;

            if (defs == null ||
                defs.Count == 0)
            {
                Widgets.Label(
                    outerRect,
                    "No civilization knowledge nodes found.");

                return;
            }

            float minTreeX;
            float minTreeY;
            float maxTreeX;
            float maxTreeY;

            GetTreeBounds(
                defs,
                out minTreeX,
                out minTreeY,
                out maxTreeX,
                out maxTreeY);

            float canvasWidth =
                CivilizationKnowledgeTreeLayout.CanvasPadding * 2f
                +
                (maxTreeX - minTreeX)
                * CivilizationKnowledgeTreeLayout.ColumnSpacing
                +
                CivilizationKnowledgeTreeLayout.NodeWidth;

            float canvasHeight =
                CivilizationKnowledgeTreeLayout.CanvasPadding * 2f
                +
                (maxTreeY - minTreeY)
                * CivilizationKnowledgeTreeLayout.RowSpacing
                +
                CivilizationKnowledgeTreeLayout.NodeHeight;

            canvasWidth =
                Mathf.Max(
                    canvasWidth,
                    outerRect.width - 20f);

            canvasHeight =
                Mathf.Max(
                    canvasHeight,
                    outerRect.height - 20f);

            Rect viewRect =
                new Rect(
                    0f,
                    0f,
                    canvasWidth,
                    canvasHeight);

            Widgets.BeginScrollView(
                outerRect,
                ref scrollPosition,
                viewRect);
            DrawPrerequisiteLines(
                defs,
                minTreeX,
                minTreeY);
            DrawNodes(
                defs,
                minTreeX,
                minTreeY);

            Widgets.EndScrollView();
        }

        private void DrawNodes(
            List<CivilizationKnowledgeDef> defs,
            float minTreeX,
            float minTreeY)
        {
            foreach (CivilizationKnowledgeDef def
                in defs)
            {
                if (def == null)
                {
                    continue;
                }

                Rect nodeRect =
                    GetNodeRect(
                        def,
                        minTreeX,
                        minTreeY);

                DrawNode(
                    nodeRect,
                    def);
            }
        }

        private Rect GetNodeRect(
            CivilizationKnowledgeDef def,
            float minTreeX,
            float minTreeY)
        {
            float x =
                CivilizationKnowledgeTreeLayout.CanvasPadding
                +
                (def.treeX - minTreeX)
                * CivilizationKnowledgeTreeLayout.ColumnSpacing;

            float y =
                CivilizationKnowledgeTreeLayout.CanvasPadding
                +
                (def.treeY - minTreeY)
                * CivilizationKnowledgeTreeLayout.RowSpacing;

            return new Rect(
                x,
                y,
                CivilizationKnowledgeTreeLayout.NodeWidth,
                CivilizationKnowledgeTreeLayout.NodeHeight);
        }

        private void DrawNode(
    Rect rect,
    CivilizationKnowledgeDef def)
        {
            CivilizationKnowledgeNodeVisualState visualState =
                CivilizationKnowledgeNodeVisualUtility
                    .GetVisualState(def);

            DrawNodeBackground(
                rect,
                visualState);

            Rect innerRect =
                rect.ContractedBy(8f);

            Rect titleRect =
                new Rect(
                    innerRect.x,
                    innerRect.y,
                    innerRect.width,
                    42f);

            Rect statusRect =
                new Rect(
                    innerRect.x,
                    titleRect.yMax,
                    innerRect.width,
                    16f);

            Rect requirementRect =
                new Rect(
                    innerRect.x,
                    statusRect.yMax + 2f,
                    innerRect.width,
                    16f);

            string title =
                !string.IsNullOrEmpty(def.title)
                    ? def.title
                    : def.label;

            Text.Font =
                GameFont.Small;

            Text.Anchor =
                TextAnchor.MiddleCenter;

            Widgets.Label(
                titleRect,
                title);

            Text.Font =
                GameFont.Tiny;

            Widgets.Label(
                statusRect,
                CivilizationKnowledgeNodeVisualUtility
                    .GetStatusLabel(visualState));

            string requirementSummary =
                CivilizationKnowledgeNodeVisualUtility
                    .GetRequirementSummary(def);

            if (!string.IsNullOrEmpty(
                requirementSummary))
            {
                Widgets.Label(
                    requirementRect,
                    requirementSummary);
            }

            Text.Anchor =
                TextAnchor.UpperLeft;

            Text.Font =
                GameFont.Small;

            if (Mouse.IsOver(rect))
            {
                Widgets.DrawHighlight(rect);
            }

            string tooltip =
                !string.IsNullOrEmpty(def.description)
                    ? def.description
                    : def.label;

            TooltipHandler.TipRegion(
                rect,
                new TipSignal(tooltip));
        }

        private void DrawNodeBackground(
    Rect rect,
    CivilizationKnowledgeNodeVisualState state)
        {
            Color backgroundColor =
                GetNodeBackgroundColor(state);

            Color borderColor =
                GetNodeBorderColor(state);

            Widgets.DrawBoxSolid(
                rect,
                backgroundColor);

            Color oldColor = GUI.color;

            GUI.color = borderColor;

            Widgets.DrawBox(
                rect,
                2);

            GUI.color = oldColor;
        }
        private Color GetNodeBackgroundColor(
    CivilizationKnowledgeNodeVisualState state)
        {
            switch (state)
            {
                case CivilizationKnowledgeNodeVisualState.Active:
                    return new Color(
                        0.16f,
                        0.24f,
                        0.18f,
                        1f);

                case CivilizationKnowledgeNodeVisualState.Unstable:
                    return new Color(
                        0.28f,
                        0.22f,
                        0.10f,
                        1f);

                case CivilizationKnowledgeNodeVisualState.Dormant:
                    return new Color(
                        0.18f,
                        0.18f,
                        0.18f,
                        1f);

                case CivilizationKnowledgeNodeVisualState
                    .AwaitingReactivation:
                    return new Color(
                        0.18f,
                        0.19f,
                        0.28f,
                        1f);

                default:
                    return new Color(
                        0.10f,
                        0.10f,
                        0.10f,
                        1f);
            }
        }
        private Color GetNodeBorderColor(
    CivilizationKnowledgeNodeVisualState state)
        {
            switch (state)
            {
                case CivilizationKnowledgeNodeVisualState.Active:
                    return new Color(
                        0.40f,
                        0.80f,
                        0.48f,
                        1f);

                case CivilizationKnowledgeNodeVisualState.Unstable:
                    return new Color(
                        0.95f,
                        0.70f,
                        0.25f,
                        1f);

                case CivilizationKnowledgeNodeVisualState.Dormant:
                    return new Color(
                        0.42f,
                        0.42f,
                        0.42f,
                        1f);

                case CivilizationKnowledgeNodeVisualState
                    .AwaitingReactivation:
                    return new Color(
                        0.48f,
                        0.58f,
                        0.92f,
                        1f);

                default:
                    return new Color(
                        0.30f,
                        0.30f,
                        0.30f,
                        1f);
            }
        }

        private void GetTreeBounds(
            List<CivilizationKnowledgeDef> defs,
            out float minX,
            out float minY,
            out float maxX,
            out float maxY)
        {
            minX = 0f;
            minY = 0f;
            maxX = 0f;
            maxY = 0f;

            bool first =
                true;

            foreach (CivilizationKnowledgeDef def
                in defs)
            {
                if (def == null)
                {
                    continue;
                }

                if (first)
                {
                    minX = def.treeX;
                    maxX = def.treeX;

                    minY = def.treeY;
                    maxY = def.treeY;

                    first = false;

                    continue;
                }

                if (def.treeX < minX)
                {
                    minX = def.treeX;
                }

                if (def.treeX > maxX)
                {
                    maxX = def.treeX;
                }

                if (def.treeY < minY)
                {
                    minY = def.treeY;
                }

                if (def.treeY > maxY)
                {
                    maxY = def.treeY;
                }
            }
        }
        private void DrawPrerequisiteLines(
    List<CivilizationKnowledgeDef> defs,
    float minTreeX,
    float minTreeY)
        {
            foreach (CivilizationKnowledgeDef def
                in defs)
            {
                if (def == null)
                {
                    continue;
                }

                if (def.prerequisites == null)
                {
                    continue;
                }

                Rect childRect =
                    GetNodeRect(
                        def,
                        minTreeX,
                        minTreeY);

                foreach (CivilizationKnowledgeDef prerequisite
                    in def.prerequisites)
                {
                    if (prerequisite == null)
                    {
                        continue;
                    }

                    Rect prerequisiteRect =
                        GetNodeRect(
                            prerequisite,
                            minTreeX,
                            minTreeY);

                    DrawPrerequisiteConnection(
                        prerequisiteRect,
                        childRect,
                        GetConnectionColor(def));
                }
            }
        }
        private void DrawPrerequisiteConnection(
    Rect prerequisiteRect,
    Rect childRect,
    Color lineColor)
        {
            float lineThickness = 2f;

            float startX =
                prerequisiteRect.xMax;

            float startY =
                prerequisiteRect.center.y;

            float endX =
                childRect.x;

            float endY =
                childRect.center.y;

            float middleX =
                (startX + endX) * 0.5f;

            DrawHorizontalLine(
                startX,
                middleX,
                startY,
                lineThickness,
                lineColor);

            DrawVerticalLine(
                startY,
                endY,
                middleX,
                lineThickness,
                lineColor);

            DrawHorizontalLine(
                middleX,
                endX,
                endY,
                lineThickness,
                lineColor);
        }
        private void DrawHorizontalLine(
    float x1,
    float x2,
    float y,
    float thickness,
    Color color)
        {
            float minX =
                Mathf.Min(x1, x2);

            float width =
                Mathf.Abs(x2 - x1);

            Rect rect =
                new Rect(
                    minX,
                    y - thickness * 0.5f,
                    width,
                    thickness);

            Widgets.DrawBoxSolid(
                rect,
                color);
        }
        private void DrawVerticalLine(
    float y1,
    float y2,
    float x,
    float thickness,
    Color color)
        {
            float minY =
                Mathf.Min(y1, y2);

            float height =
                Mathf.Abs(y2 - y1);

            Rect rect =
                new Rect(
                    x - thickness * 0.5f,
                    minY,
                    thickness,
                    height);

            Widgets.DrawBoxSolid(
                rect,
                color);
        }
        private Color GetConnectionColor(
    CivilizationKnowledgeDef child)
        {
            if (child == null)
            {
                return new Color(
                    0.30f,
                    0.30f,
                    0.30f,
                    1f);
            }

            if (CivilizationKnowledgeManager
                .IsUnlocked(child))
            {
                return new Color(
                    0.55f,
                    0.65f,
                    0.55f,
                    1f);
            }

            return new Color(
                0.30f,
                0.30f,
                0.30f,
                1f);
        }
    }
}