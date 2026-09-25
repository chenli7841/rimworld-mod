using System.Collections.Generic;
using System.Linq;
using LiAIChat.Archive;
using Verse;

namespace LiAIChat.Civilization
{
    public static class CivilizationTopicConclusions
    {
        public static string Build(CivilizationTopicExtension topic, List<string> participants, string reward)
        {
            string title = string.IsNullOrEmpty(topic.requiredText.titleChinese)
                ? topic.requiredText.title : topic.requiredText.titleChinese;
            string names = participants.Count == 0 ? "本次研讨小组" : string.Join("、", participants.Take(4).ToArray())
                + (participants.Count > 4 ? "等 " + participants.Count + " 人" : "");
            var component = Current.Game.GetComponent<LiAIChat.Game.LiAIChatGameComponent>();
            var recovered = component?.RecoveredDocuments?
                .FirstOrDefault(s => s != null && s.TextDefName == topic.requiredText.defName);
            var line = DocumentRecovery.GetLine(topic.requiredText.defName);
            var titles = new List<string>();
            if (line != null && recovered != null && recovered.UnlockedSectionIds != null)
                for (int i = 0; i < line.Titles.Length; i++)
                    if (recovered.UnlockedSectionIds.Contains(DocumentRecovery.GetSectionId(topic.requiredText.defName, i)))
                        titles.Add(line.Titles[i]);
            string basis = titles.Count == 0 ? "以已识别馆藏作初步研讨，尚无已解锁的复原章节"
                : "结合已解锁章节「" + string.Join("」「", titles.Skip(System.Math.Max(0, titles.Count - 2)).ToArray()) + "」";
            return names + "围绕《" + title + "》，" + basis + "，形成结论："
                + topic.conclusion + " 本次实践成果：" + reward + "。";
        }

        public static string Context(string textDefName = null, bool includeStatus = true)
        {
            return CivilizationTopicGameComponent.Instance?.ConclusionContext(textDefName, includeStatus) ?? string.Empty;
        }
    }
}
