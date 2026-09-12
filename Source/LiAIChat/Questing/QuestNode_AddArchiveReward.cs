using LiAIChat.Archive;
using RimWorld;
using RimWorld.QuestGen;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace LiAIChat.Questing
{
    public class QuestNode_AddArchiveReward : QuestNode
    {
        public SlateRef<string> inSignal;
        public SlateRef<float> rewardValue;
        private SitePartWorker worker;

        protected override bool TestRunInt(Slate slate)
        {
            return slate != null;
        }

        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;

            if (slate == null)
            {
                Log.Error(
                    "[Li AI Chat] QuestGen.slate is null.");

                return;
            }

            Map map = slate.Get<Map>("map");

            if (map == null)
            {
                map = Find.AnyPlayerHomeMap;

                if (map == null)
                {
                    Log.Error(
                        "[Li AI Chat] Archive reward choice " +
                        "could not find any player home map.");

                    return;
                }
            }

            string rawSignal =
                inSignal.GetValue(slate);

            if (string.IsNullOrWhiteSpace(
                rawSignal))
            {
                Log.Error(
                    "[Li AI Chat] Archive reward choice " +
                    "has no completion signal.");

                return;
            }

            string rewardSignal =
                QuestGenUtility
                    .HardcodedSignalWithQuestID(
                        rawSignal);

            // =====================================================
            // Create Reward Choice
            // =====================================================

            QuestPart_Choice choicePart =
                new QuestPart_Choice();

            choicePart.inSignalChoiceUsed =
                rewardSignal;

            float value = rewardValue.GetValue(slate);

            if (value <= 0f)
            {
                value = 1800f;
            }

            // =====================================================
            // Choice A
            // Silver + Components
            // =====================================================

            List<Thing> rewardA = GenerateStandardReward(value);

            AddItemChoice(
                choicePart,
                rewardA,
                map,
                rewardSignal);

            // =====================================================
            // Choice B
            // Medicine
            // =====================================================

            List<Thing> rewardB = GenerateResourceReward(value);

            AddItemChoice(
                choicePart,
                rewardB,
                map,
                rewardSignal);

            // =====================================================
            // Choice C
            // Ancient Earth Archive
            // =====================================================

            Thing_AncientEarthArchiveFragment archive =
                AncientArchiveFactory.Create(
                    ArchiveSourceType.QuestReward,
                    "Received as a quest reward.");


            if (archive == null)
            {
                Log.Error(
                    "[Li AI Chat] Failed to create " +
                    "Ancient Earth Archive reward.");

                return;
            }

            List<Thing> rewardC = new List<Thing>();
            rewardC.Add(archive);
            AddItemChoice(
                choicePart,
                rewardC,
                map,
                rewardSignal);


            rewardC.Add(archive);

            

            // =====================================================
            // Add RewardChoice itself
            // =====================================================

            QuestGen.quest.AddPart(
                choicePart);

            Log.Message(
                "[Li AI Chat] Added quest reward choices. " +
                "Archive content: " +
                (archive.Content != null
                    ? archive.Content.defName
                    : "null"));
        }

        private static List<Thing> GenerateResourceReward(
    float rewardValue)
        {
            List<Thing> items =
                new List<Thing>();

            // 约 75% 奖励价值给 Silver
            float silverValue =
                rewardValue * 0.75f;

            int silverCount =
                Mathf.Max(
                    1,
                    Mathf.RoundToInt(
                        silverValue /
                        ThingDefOf.Silver.BaseMarketValue));

            Thing silver =
                ThingMaker.MakeThing(
                    ThingDefOf.Silver);

            silver.stackCount =
                silverCount;

            items.Add(
                silver);

            // 剩余约 25% 给工业组件
            float componentValue =
                rewardValue * 0.25f;

            int componentCount =
                Mathf.Max(
                    1,
                    Mathf.RoundToInt(
                        componentValue /
                        ThingDefOf.ComponentIndustrial.BaseMarketValue));

            Thing components =
                ThingMaker.MakeThing(
                    ThingDefOf.ComponentIndustrial);

            components.stackCount =
                componentCount;

            items.Add(
                components);

            return items;
        }

        private static List<Thing> GenerateStandardReward(
    float rewardValue)
        {
            Reward_Items reward =
                new Reward_Items();

            RewardsGeneratorParams parms =
                new RewardsGeneratorParams();

            float valueActuallyUsed;

            reward.InitFromValue(
                rewardValue,
                parms,
                out valueActuallyUsed);

            Log.Message(
                "[Li AI Chat] Generated standard reward. " +
                "Requested value=" +
                rewardValue +
                ", Actual value=" +
                valueActuallyUsed);

            return reward.items;
        }

        private static void AddItemChoice(
            QuestPart_Choice choicePart,
            List<Thing> items,
            Map map,
            string rewardSignal)
        {
            if (choicePart == null ||
                items == null ||
                items.Count == 0 ||
                map == null)
            {
                return;
            }

            QuestPart_Choice.Choice choice =
                new QuestPart_Choice.Choice();

            // =====================================================
            // UI reward description
            // =====================================================

            Reward_Items reward =
                new Reward_Items();

            reward.items.AddRange(
                items);

            choice.rewards.Add(
                reward);

            // =====================================================
            // Actual delivery
            // =====================================================

            QuestPart_DropPods dropPods =
                new QuestPart_DropPods();

            dropPods.inSignal =
                rewardSignal;

            dropPods.mapParent =
                map.Parent;

            dropPods.useTradeDropSpot =
                true;

            dropPods.Things =
                items;

            // This part must belong to the quest.
            QuestGen.quest.AddPart(
                dropPods);

            // And also belong to this Choice,
            // so RimWorld can remove it when another
            // reward option is selected.
            choice.questParts.Add(
                dropPods);

            choicePart.choices.Add(
                choice);
        }
    }
}