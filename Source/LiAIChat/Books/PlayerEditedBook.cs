using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;

namespace LiAIChat.Books
{
    public class Thing_PlayerEditedBook : Book
    {
        public string BookTitle = "未命名新书";
        public string Body = "";
        public override void ExposeData() { base.ExposeData(); Scribe_Values.Look(ref BookTitle, "title", "未命名新书"); Scribe_Values.Look(ref Body, "body", ""); }
        public override string LabelNoCount => string.IsNullOrWhiteSpace(BookTitle) ? "未命名新书" : BookTitle;
        public override string DescriptionDetailed => "一本由殖民地保存的自编书籍。\n正文：" + (string.IsNullOrWhiteSpace(Body) ? "尚未写下内容。" : "已写入 " + Body.Length + " 字。");
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos()) yield return gizmo;
            yield return new Command_Action { defaultLabel = "编辑图书正文", defaultDesc = "打开图书编辑器，修改标题与不限字数的正文。", icon = UI.LiAIChatTextures.EditBook, action = () => Find.WindowStack.Add(new Dialog_PlayerBookEditor(this)) };
        }
    }

    public static class PlayerBookEditorService
    {
        public static bool TryCreate(Thing shelf)
        {
            if (shelf?.Map == null) return false;
            List<Thing> all = shelf.Map.listerThings.AllThings.ToList();
            List<Thing> paper = all.Where(t => t.def.defName == "LiAIChat_WritingPaper").ToList();
            List<Thing> pens = all.Where(t => t.def.defName == "LiAIChat_SteelPen" || t.def.defName == "LiAIChat_SilverPen" || t.def.defName == "LiAIChat_GoldPen").ToList();
            if (paper.Sum(t => t.stackCount) < 100 || pens.Sum(t => t.stackCount) < 20)
            {
                Messages.Message("创建新书需要 100 张书写纸和 20 支笔。", MessageTypeDefOf.RejectInput);
                return false;
            }
            ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail("LiAIChat_PlayerEditedBook");
            if (def == null)
            {
                Messages.Message("无法创建新书：书籍定义未能载入。", MessageTypeDefOf.RejectInput);
                return false;
            }

            Thing_PlayerEditedBook book = ThingMaker.MakeThing(def) as Thing_PlayerEditedBook;
            if (book == null)
            {
                Messages.Message("无法创建新书：书籍实例未能生成。", MessageTypeDefOf.RejectInput);
                return false;
            }

            IntVec3 placementCell;
            bool foundPlacement = CellFinder.TryFindRandomCellNear(
                shelf.Position,
                shelf.Map,
                4,
                cell => cell.Standable(shelf.Map) && !cell.Fogged(shelf.Map),
                out placementCell);
            if (!foundPlacement)
            {
                book.Destroy();
                Messages.Message("无法创建新书：书架周围没有可放置的位置。", MessageTypeDefOf.RejectInput);
                return false;
            }

            GenSpawn.Spawn(book, placementCell, shelf.Map);
            Consume(paper, 100);
            Consume(pens, 20);
            Find.WindowStack.Add(new Dialog_PlayerBookEditor(book));
            Messages.Message("已在书架附近创建空白新书，并消耗 100 张书写纸和 20 支笔。", MessageTypeDefOf.PositiveEvent);
            return true;
        }
        static void Consume(List<Thing> stacks, int amount)
        {
            foreach (Thing stack in stacks)
            {
                int take = Mathf.Min(amount, stack.stackCount);
                if (take == stack.stackCount) stack.Destroy(DestroyMode.Vanish); else stack.SplitOff(take).Destroy(DestroyMode.Vanish);
                amount -= take; if (amount == 0) return;
            }
        }
    }

    public class Dialog_PlayerBookEditor : Window
    {
        private static readonly FieldInfo ResizerField = typeof(Window).GetField(
            "resizer",
            BindingFlags.Instance | BindingFlags.NonPublic);

        readonly Thing_PlayerEditedBook book; Vector2 scroll; string title; string body;
        public override Vector2 InitialSize => new Vector2(620f, 760f);
        public Dialog_PlayerBookEditor(Thing_PlayerEditedBook book)
        {
            this.book = book; title = book.BookTitle ?? ""; body = book.Body ?? "";
            doCloseX = true; doCloseButton = true; absorbInputAroundWindow = true;
            forcePause = false; draggable = true; resizeable = true;
            closeOnAccept = false;
            WindowResizer resizer = new WindowResizer
            {
                minWindowSize = new Vector2(560f, 460f)
            };
            ResizerField?.SetValue(this, resizer);
        }

        public override void PostOpen()
        {
            base.PostOpen();
            float width = Mathf.Min(InitialSize.x, Verse.UI.screenWidth - 12f);
            float height = Mathf.Min(InitialSize.y, Verse.UI.screenHeight - 72f);
            windowRect = new Rect(6f, 56f, width, height);
        }

        public override void DoWindowContents(Rect rect)
        {
            Text.Font = GameFont.Medium; Widgets.Label(new Rect(rect.x, rect.y, rect.width, 30f), "图书编辑器"); Text.Font = GameFont.Small;
            Widgets.Label(new Rect(rect.x, rect.y + 38f, 55f, 24f), "书名："); title = Widgets.TextField(new Rect(rect.x + 58f, rect.y + 34f, rect.width - 58f, 30f), title);
            Widgets.Label(new Rect(rect.x, rect.y + 70f, rect.width, 24f), "正文（不限字数；按回车换行，自动保存于此书）：");
            Rect view = new Rect(rect.x, rect.y + 96f, rect.width, rect.height - 148f); float height = Mathf.Max(view.height - 18f, Text.CalcHeight(body + " ", view.width - 28f) + 24f);
            Widgets.BeginScrollView(view, ref scroll, new Rect(0f, 0f, view.width - 18f, height)); body = Widgets.TextArea(new Rect(0f, 0f, view.width - 18f, height), body); Widgets.EndScrollView();
            Widgets.Label(new Rect(rect.x, rect.yMax - 42f, 300f, 26f), "当前字数：" + body.Length);
            if (Widgets.ButtonText(new Rect(rect.x + rect.width - 160f, rect.yMax - 46f, 160f, 34f), "保存图书")) { book.BookTitle = string.IsNullOrWhiteSpace(title) ? "未命名新书" : title.Trim(); book.Body = body ?? ""; Messages.Message("图书已保存。", MessageTypeDefOf.PositiveEvent); }
        }
    }
}
