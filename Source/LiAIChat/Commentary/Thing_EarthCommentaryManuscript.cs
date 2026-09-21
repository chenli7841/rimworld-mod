using Verse;

namespace LiAIChat.Commentary
{
    public class Thing_EarthCommentaryManuscript : Book
    {
        public override string LabelNoCount
        {
            get
            {
                EarthCommentaryWork work = CommentaryService.Get(thingIDNumber);
                return work == null || string.IsNullOrWhiteSpace(work.Title)
                    ? "未完成的地球文献评注"
                    : work.Title;
            }
        }

        public override string DescriptionDetailed
        {
            get
            {
                EarthCommentaryWork work = CommentaryService.Get(thingIDNumber);
                if (work == null) return def.description;
                return "作者：" + work.AuthorName + "\n" + CommentaryService.Status(work);
            }
        }
    }
}
