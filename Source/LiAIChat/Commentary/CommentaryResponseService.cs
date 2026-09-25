using LiAIChat.Background;
using LiAIChat.Config;
using System.Linq;
using System.Net.Http;
using Verse;

namespace LiAIChat.Commentary
{
    public static class CommentaryResponseService
    {
        private static readonly HttpClient Client = new HttpClient();
        public static void CheckDueReplies()
        {
            if (!CommentaryResearch.MarginaliaIsFinished || Find.TickManager == null) return;
            foreach (EarthCommentaryWork work in CommentaryService.Works.Where(w => w != null && !w.Generating))
            foreach (EarthCommentaryEntry entry in work.Entries.Where(e => e != null && e.PendingReply && e.ReplyDueTick >= 0 && e.ReplyDueTick <= Find.TickManager.TicksGame).ToList())
            { entry.ReplyDueTick = -2; work.Generating = true; Generate(work, entry); return; }
        }
        private static async void Generate(EarthCommentaryWork work, EarthCommentaryEntry entry)
        {
            try
            {
                string prompt = "为 RimWorld 殖民者写中文作者回应。作者：" + work.AuthorName + "。评注：" + work.Body + "。未知存在的批注：" + entry.Text + "。回应须针对批注，表达惊奇、好奇、敬畏、感谢或怀疑之一，像私人后记。不要提及AI、玩家、提示词或游戏机制。只返回回应正文。";
                string topicMaterial = LiAIChat.Civilization.CivilizationTopicConclusions.Context(work.EarthTextDefName);
                if (!string.IsNullOrEmpty(topicMaterial))
                    prompt += "\n可参考的殖民地专题研讨素材（不代表作者已掌握全书，不要照抄游戏数值）：\n" + topicMaterial;
                string text = await new OpenAIEarthCommentaryGenerator(Client, Config.Config.OpenAI_API_KEY).GenerateAsync(prompt);
                MainThreadActionQueue.Enqueue(() => { work.Entries.Add(new EarthCommentaryEntry { Kind = "authorReply", Text = text ?? "", CreatedTick = Find.TickManager.TicksGame }); entry.PendingReply = false; work.Generating = false; LiAIChat.Events.ColonyEventLog.Record("回应批注", work.AuthorName + " 回应了神秘批注，并修订了“" + work.Title + "”。", 2, null, "commentary-reply:" + work.BookThingId + ":" + entry.CreatedTick); });
            }
            catch { MainThreadActionQueue.Enqueue(() => { entry.ReplyDueTick = Find.TickManager.TicksGame + 60000; work.Generating = false; }); }
        }
    }
}
