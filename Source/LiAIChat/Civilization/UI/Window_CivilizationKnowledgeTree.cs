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
            Widgets.DrawMenuSection(rect);

            Rect innerRect =
                rect.ContractedBy(8f);

            string title =
                !string.IsNullOrEmpty(def.title)
                    ? def.title
                    : def.label;

            Text.Anchor =
                TextAnchor.MiddleCenter;

            Text.Font =
                GameFont.Small;

            Widgets.Label(
                innerRect,
                title);

            Text.Anchor =
                TextAnchor.UpperLeft;
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
    }
}