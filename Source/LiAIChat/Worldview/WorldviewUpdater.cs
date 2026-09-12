using System;
using LiAIChat.Models;

namespace LiAIChat.Worldview
{
    public static class WorldviewUpdater
    {
        public static AppliedWorldviewChange Apply(
            WorldviewState worldview,
            WorldviewChange proposedChange)
        {
            AppliedWorldviewChange applied =
                new AppliedWorldviewChange();

            applied.BeliefInGodDelta =
                ApplyStandardChange(
                    worldview.BeliefInGod,
                    proposedChange.BeliefInGodDelta);

            worldview.BeliefInGod =
                Clamp01(
                    worldview.BeliefInGod +
                    applied.BeliefInGodDelta);


            applied.BeliefInObjectiveMoralityDelta =
                ApplyStandardChange(
                    worldview.BeliefInObjectiveMorality,
                    proposedChange.BeliefInObjectiveMoralityDelta);

            worldview.BeliefInObjectiveMorality =
                Clamp01(
                    worldview.BeliefInObjectiveMorality +
                    applied.BeliefInObjectiveMoralityDelta);


            applied.TrustInChristianityDelta =
                ApplyStandardChange(
                    worldview.TrustInChristianity,
                    proposedChange.TrustInChristianityDelta);

            worldview.TrustInChristianity =
                Clamp01(
                    worldview.TrustInChristianity +
                    applied.TrustInChristianityDelta);


            applied.KnowledgeOfChristianityDelta =
                ApplyKnowledgeChange(
                    worldview.KnowledgeOfChristianity,
                    proposedChange.KnowledgeOfChristianityDelta);

            worldview.KnowledgeOfChristianity =
                Clamp01(
                    worldview.KnowledgeOfChristianity +
                    applied.KnowledgeOfChristianityDelta);


            applied.IntellectualResistanceDelta =
                ApplyStandardChange(
                    worldview.IntellectualResistance,
                    proposedChange.IntellectualResistanceDelta);

            worldview.IntellectualResistance =
                Clamp01(
                    worldview.IntellectualResistance +
                    applied.IntellectualResistanceDelta);


            applied.EmotionalResistanceDelta =
                ApplyStandardChange(
                    worldview.EmotionalResistance,
                    proposedChange.EmotionalResistanceDelta);

            worldview.EmotionalResistance =
                Clamp01(
                    worldview.EmotionalResistance +
                    applied.EmotionalResistanceDelta);


            applied.SpiritualInterestDelta =
                ApplyStandardChange(
                    worldview.SpiritualInterest,
                    proposedChange.SpiritualInterestDelta);

            worldview.SpiritualInterest =
                Clamp01(
                    worldview.SpiritualInterest +
                    applied.SpiritualInterestDelta);

            return applied;
        }


        private static float ApplyStandardChange(
            float currentValue,
            float proposedDelta)
        {
            float normalized =
                NormalizeDirectorDelta(
                    proposedDelta);

            float scaled =
                normalized *
                WorldviewChangeRules
                    .MaxAppliedDeltaPerConversation;

            scaled *=
                GetExtremeValueResistance(
                    currentValue,
                    scaled);

            return RemoveTinyDelta(
                scaled);
        }


        private static float ApplyKnowledgeChange(
            float currentValue,
            float proposedDelta)
        {
            if (proposedDelta <= 0f)
            {
                return 0f;
            }

            float normalized =
                NormalizeDirectorDelta(
                    proposedDelta);

            float scaled =
                normalized *
                WorldviewChangeRules
                    .MaxKnowledgeDeltaPerConversation;

            scaled *=
                GetExtremeValueResistance(
                    currentValue,
                    scaled);

            return RemoveTinyDelta(
                scaled);
        }


        private static float NormalizeDirectorDelta(
            float delta)
        {
            float limit =
                WorldviewChangeRules
                    .DirectorDeltaLimit;

            if (delta < -limit)
                delta = -limit;

            if (delta > limit)
                delta = limit;

            return delta / limit;
        }


        private static float GetExtremeValueResistance(
            float currentValue,
            float delta)
        {
            if (delta > 0f &&
                currentValue >
                WorldviewChangeRules
                    .ExtremeResistanceStart)
            {
                float remaining =
                    1f - currentValue;

                float fullRange =
                    1f -
                    WorldviewChangeRules
                        .ExtremeResistanceStart;

                return remaining / fullRange;
            }

            if (delta < 0f &&
                currentValue <
                1f -
                WorldviewChangeRules
                    .ExtremeResistanceStart)
            {
                float distanceFromZero =
                    currentValue;

                float fullRange =
                    1f -
                    WorldviewChangeRules
                        .ExtremeResistanceStart;

                return distanceFromZero / fullRange;
            }

            return 1f;
        }


        private static float RemoveTinyDelta(
            float delta)
        {
            if (Math.Abs(delta) <
                WorldviewChangeRules
                    .MinimumMeaningfulDelta)
            {
                return 0f;
            }

            return delta;
        }


        private static float Clamp01(
            float value)
        {
            if (value < 0f)
                return 0f;

            if (value > 1f)
                return 1f;

            return value;
        }
    }
}