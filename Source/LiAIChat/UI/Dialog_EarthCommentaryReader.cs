using LiAIChat.Commentary;
using System.Linq;
using UnityEngine;
using Verse;

namespace LiAIChat.UI
{
    public class Dialog_EarthCommentaryReader : Window
    {
        private readonly EarthCommentaryWork work;
        private Vector2 scroll;
        private string annotation = "";
        public override Vector2 InitialSize { get { return new Vector2(760f, 720f); } }
        public Dialog_EarthCommentaryReader(EarthCommentaryWork work)
        {
            this.work = work; doCloseX = true; doCloseButton = true; absorbInputAroundWindow = true;
        }
        public override void DoWindowContents(Rect rect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(rect.x, rect.y, rect.width, 32f), work.Title);
            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(rect.x, rect.y + 34f, rect.width, 24f), "作者：" + work.AuthorName + "　" + CommentaryService.Status(work));
            Rect bodyRect = new Rect(rect.x, rect.y + 64f, rect.width, rect.height - 178f);
            string text = string.IsNullOrWhiteSpace(work.Body) ? "这本手稿仍在撰写，尚无可阅读的正文。" : work.Body;
            foreach (EarthCommentaryEntry entry in work.Entries.Where(e => e != null))
                text += "\n\n" + (entry.Kind == "mysteriousAnnotation" ? "【神秘批注】" : "【作者回应】") + "\n" + entry.Text;
            float height = Text.CalcHeight(text, bodyRect.width - 18f) + 8f;
            Widgets.BeginScrollView(bodyRect, ref scroll, new Rect(0f, 0f, bodyRect.width - 18f, height));
            Widgets.Label(new Rect(0f, 0f, bodyRect.width - 18f, height), text);
            Widgets.EndScrollView();
            Rect input = new Rect(rect.x, rect.yMax - 104f, rect.width, 58f);
            annotation = Widgets.TextArea(input, annotation);
            GUI.enabled = CommentaryResearch.MarginaliaIsFinished && !work.Generating && !string.IsNullOrWhiteSpace(work.Body) && !string.IsNullOrWhiteSpace(annotation);
            if (Widgets.ButtonText(new Rect(rect.x + rect.width - 180f, rect.yMax - 38f, 180f, 32f), "留下神秘批注"))
            { CommentaryService.AddPlayerAnnotation(work, annotation); annotation = ""; }
            GUI.enabled = true;
            if (!CommentaryResearch.MarginaliaIsFinished)
                Widgets.Label(new Rect(rect.x, rect.yMax - 38f, 360f, 30f), "需完成研究：神秘批注学");
        }
    }
}
