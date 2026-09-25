using LiAIChat.Archive;
using UnityEngine;
using Verse;

namespace LiAIChat.UI
{
    public class Dialog_RecoveredTextReader : Window
    {
        private Vector2 scroll;
        public override Vector2 InitialSize => new Vector2(760f, 660f);
        private readonly string textDefName;
        public Dialog_RecoveredTextReader(string textDefName) { this.textDefName = textDefName; doCloseX = true; absorbInputAroundWindow = true; }
        public override void DoWindowContents(Rect rect)
        {
            RecoveredDocumentState state = DocumentRecovery.GetState(textDefName);
            DocumentRecovery.DocumentLine line = DocumentRecovery.GetLine(textDefName);
            float contentHeight = CalculateContentHeight(line, state, rect.width - 28f);
            Rect viewRect = new Rect(0f, 0f, rect.width - 18f, contentHeight);
            Widgets.BeginScrollView(rect, ref scroll, viewRect);
            Widgets.Label(new Rect(0f, 0f, viewRect.width, 32f), line.Name + "：现代中文节选解说");
            float y = 42f;
            for (int i = 0; i < line.Titles.Length; i++)
            {
                bool unlocked = state.UnlockedSectionIds.Contains(DocumentRecovery.GetSectionId(textDefName, i));
                Widgets.Label(new Rect(0f, y, viewRect.width, 25f), (unlocked ? "◆ " : "◇ ") + (i + 1) + ". " + line.Titles[i]); y += 28f;
                if (unlocked) { float h = Text.CalcHeight(line.Bodies[i], viewRect.width - 28f); Widgets.Label(new Rect(18f, y, viewRect.width - 28f, h), line.Bodies[i]); y += h + 18f; }
                else { Widgets.Label(new Rect(18f, y, viewRect.width - 18f, 24f), "尚未复原。"); y += 30f; }
            }
            if (state.DeliveryTick > 0) Widgets.Label(new Rect(0f, y, viewRect.width, 24f), "译稿密钥正在复原中。");
            Widgets.EndScrollView();
        }

        private static float CalculateContentHeight(DocumentRecovery.DocumentLine line,
            RecoveredDocumentState state, float width)
        {
            float height = 42f;
            for (int i = 0; i < line.Titles.Length; i++)
            {
                height += 28f;
                if (state.UnlockedSectionIds.Contains(DocumentRecovery.GetSectionId(state.TextDefName, i)))
                    height += Text.CalcHeight(line.Bodies[i], width - 28f) + 18f;
                else height += 30f;
            }
            return height + (state.DeliveryTick > 0 ? 30f : 0f);
        }
    }
}
