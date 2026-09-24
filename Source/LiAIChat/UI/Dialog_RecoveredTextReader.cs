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
            Widgets.Label(new Rect(rect.x, rect.y, rect.width, 32f), line.Name + "：现代中文节选解说");
            float y = 42f;
            for (int i = 0; i < line.Titles.Length; i++)
            {
                bool unlocked = state.UnlockedSectionIds.Contains(DocumentRecovery.GetSectionId(textDefName, i));
                Widgets.Label(new Rect(rect.x, y, rect.width, 25f), (unlocked ? "◆ " : "◇ ") + (i + 1) + ". " + line.Titles[i]); y += 28f;
                if (unlocked) { float h = Text.CalcHeight(line.Bodies[i], rect.width - 28f); Widgets.Label(new Rect(rect.x + 18f, y, rect.width - 28f, h), line.Bodies[i]); y += h + 18f; }
                else { Widgets.Label(new Rect(rect.x + 18f, y, rect.width - 18f, 24f), "尚未复原。"); y += 30f; }
            }
            if (state.DeliveryTick > 0) Widgets.Label(new Rect(rect.x, rect.height - 60f, rect.width, 24f), "译稿密钥正在复原中。");
        }
    }
}
