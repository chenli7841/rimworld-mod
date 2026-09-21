using LiAIChat.Commentary;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace LiAIChat.UI
{
    public class Dialog_EarthCommentaryLibrary : Window
    {
        private readonly Thing shelf;
        private Vector2 scroll;
        public override Vector2 InitialSize { get { return new Vector2(620f, 520f); } }
        public Dialog_EarthCommentaryLibrary(Thing shelf)
        {
            this.shelf = shelf; doCloseX = true; doCloseButton = true; absorbInputAroundWindow = true;
        }
        public override void DoWindowContents(Rect rect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(rect.x, rect.y, rect.width, 32f), "文献馆藏");
            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(rect.x, rect.y + 34f, rect.width, 24f), "选择书架上的手稿以阅读其内容与批注。 ");
            List<Thing_EarthCommentaryManuscript> books = CommentaryService.GetManuscriptsOnShelf(shelf);
            if (books.Count == 0)
            {
                Widgets.Label(new Rect(rect.x, rect.y + 76f, rect.width, 30f), "这座书架目前没有地球文献评注手稿。 ");
                return;
            }
            Rect outer = new Rect(rect.x, rect.y + 66f, rect.width, rect.height - 72f);
            Rect view = new Rect(0f, 0f, outer.width - 16f, books.Count * 58f);
            Widgets.BeginScrollView(outer, ref scroll, view);
            for (int i = 0; i < books.Count; i++)
            {
                Thing_EarthCommentaryManuscript book = books[i];
                EarthCommentaryWork work = CommentaryService.Get(book.thingIDNumber);
                Rect row = new Rect(0f, i * 58f, view.width, 52f);
                if (Widgets.ButtonText(row, book.LabelNoCount + "\n" + (work == null ? "" : "作者：" + work.AuthorName + "　" + CommentaryService.Status(work))))
                    Find.WindowStack.Add(new Dialog_EarthCommentaryReader(work));
            }
            Widgets.EndScrollView();
        }
    }
}
