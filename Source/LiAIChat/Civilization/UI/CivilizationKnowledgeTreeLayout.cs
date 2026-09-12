using UnityEngine;

namespace LiAIChat.Civilization.UI
{
    public static class CivilizationKnowledgeTreeLayout
    {
        public const float NodeWidth = 190f;
        public const float NodeHeight = 72f;

        public const float ColumnSpacing = 250f;
        public const float RowSpacing = 120f;

        public const float CanvasPadding = 80f;

        public static Rect GetNodeRect(
            CivilizationKnowledgeDef def)
        {
            float x =
                CanvasPadding +
                def.treeX * ColumnSpacing;

            float y =
                CanvasPadding +
                def.treeY * RowSpacing;

            return new Rect(
                x,
                y,
                NodeWidth,
                NodeHeight);
        }
    }
}