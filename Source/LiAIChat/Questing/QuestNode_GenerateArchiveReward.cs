using System.Collections.Generic;
using RimWorld.QuestGen;
using Verse;
using LiAIChat.Archive;

namespace LiAIChat.Questing
{
    public class QuestNode_GenerateArchiveReward : QuestNode
    {
        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;

            Thing_AncientEarthArchiveFragment archive =
                AncientArchiveFactory.Create(
                    ArchiveSourceType.QuestReward,
                    "Recovered as a quest reward.");

            var rewards =
                new List<Thing>
                {
                    archive
                };

            slate.Set(
                "archiveReward",
                rewards);

            Log.Message(
                "[Li AI Chat] Archive quest reward generated: "
                + archive.Content?.title);
        }

        protected override bool TestRunInt(
            Slate slate)
        {
            return true;
        }
    }
}