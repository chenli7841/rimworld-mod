using LiAIChat.Archive;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace LiAIChat.Civilization
{
    public static class CivilizationKnowledgeUtility
    {
        public static bool HasRequiredTexts(
            CivilizationKnowledgeDef knowledgeDef)
        {
            if (knowledgeDef == null)
            {
                return false;
            }

            return ColonyLibrary.HasAllTexts(
                knowledgeDef.requiredTexts);
        }

        public static List<EarthTextDef>
            GetMissingRequiredTexts(
                CivilizationKnowledgeDef knowledgeDef)
        {
            if (knowledgeDef == null)
            {
                return new List<EarthTextDef>();
            }

            return ColonyLibrary.GetMissingTexts(
                knowledgeDef.requiredTexts);
        }
        public static bool IsMissingRequiredTexts(
    CivilizationKnowledgeDef knowledgeDef)
        {
            if (knowledgeDef == null)
            {
                return false;
            }

            return !HasRequiredTexts(
                knowledgeDef);
        }
        public static bool IsUnlockedButIncomplete(
    CivilizationKnowledgeDef knowledgeDef)
        {
            if (knowledgeDef == null)
            {
                return false;
            }

            if (!CivilizationKnowledgeManager
                .IsUnlocked(knowledgeDef))
            {
                return false;
            }

            return IsMissingRequiredTexts(
                knowledgeDef);
        }
        public static int GetMissingDurationTicks(
    CivilizationKnowledgeDef knowledgeDef)
        {
            if (knowledgeDef == null)
            {
                return 0;
            }

            CivilizationKnowledgeState state =
                CivilizationKnowledgeManager
                    .GetState(knowledgeDef);

            if (state == null ||
                state.MissingSinceTick < 0)
            {
                return 0;
            }

            if (Find.TickManager == null)
            {
                return 0;
            }

            int duration =
                Find.TickManager.TicksGame -
                state.MissingSinceTick;

            return duration < 0
                ? 0
                : duration;
        }
        public static float GetMissingDurationDays(
    CivilizationKnowledgeDef knowledgeDef)
        {
            int ticks =
                GetMissingDurationTicks(
                    knowledgeDef);

            return ticks /
                (float)GenDate.TicksPerDay;
        }
        public static int GetGracePeriodTicks(
    CivilizationKnowledgeDef knowledgeDef)
        {
            if (knowledgeDef == null)
            {
                return 0;
            }

            int days =
                knowledgeDef.gracePeriodDays;

            if (days < 0)
            {
                days = 0;
            }

            return days *
                GenDate.TicksPerDay;
        }
        public static bool IsWithinGracePeriod(
    CivilizationKnowledgeDef knowledgeDef)
        {
            if (knowledgeDef == null)
            {
                return false;
            }

            CivilizationKnowledgeState state =
                CivilizationKnowledgeManager
                    .GetState(knowledgeDef);

            if (state == null)
            {
                return false;
            }

            if (!state.Unlocked)
            {
                return false;
            }

            if (state.MissingSinceTick < 0)
            {
                return false;
            }

            int missingDuration =
                GetMissingDurationTicks(
                    knowledgeDef);

            int gracePeriod =
                GetGracePeriodTicks(
                    knowledgeDef);

            return missingDuration <
                   gracePeriod;
        }
        public static bool HasGracePeriodExpired(
    CivilizationKnowledgeDef knowledgeDef)
        {
            if (knowledgeDef == null)
            {
                return false;
            }

            if (!IsUnlockedButIncomplete(
                knowledgeDef))
            {
                return false;
            }

            CivilizationKnowledgeState state =
                CivilizationKnowledgeManager
                    .GetState(knowledgeDef);

            if (state == null ||
                state.MissingSinceTick < 0)
            {
                return false;
            }

            return GetMissingDurationTicks(
                       knowledgeDef)
                   >=
                   GetGracePeriodTicks(
                       knowledgeDef);
        }
        public static int GetDormantThresholdTicks(
    CivilizationKnowledgeDef knowledgeDef)
        {
            if (knowledgeDef == null)
            {
                return 0;
            }

            int days =
                knowledgeDef.dormantAfterDays;

            if (days < 0)
            {
                days = 0;
            }

            return days *
                GenDate.TicksPerDay;
        }
        public static bool ShouldBeDormant(
    CivilizationKnowledgeDef knowledgeDef)
        {
            if (knowledgeDef == null)
            {
                return false;
            }

            if (!IsUnlockedButIncomplete(
                knowledgeDef))
            {
                return false;
            }

            CivilizationKnowledgeState state =
                CivilizationKnowledgeManager
                    .GetState(knowledgeDef);

            if (state == null ||
                state.MissingSinceTick < 0)
            {
                return false;
            }

            int dormantThreshold =
                GetDormantThresholdTicks(
                    knowledgeDef);

            int graceThreshold =
                GetGracePeriodTicks(
                    knowledgeDef);

            if (dormantThreshold <
                graceThreshold)
            {
                dormantThreshold =
                    graceThreshold;
            }

            return GetMissingDurationTicks(
                       knowledgeDef)
                   >=
                   dormantThreshold;
        }
        public static bool CanReactivate(
    CivilizationKnowledgeDef knowledgeDef)
        {
            if (knowledgeDef == null)
            {
                return false;
            }

            CivilizationKnowledgeState state =
                CivilizationKnowledgeManager
                    .GetState(knowledgeDef);

            if (state == null)
            {
                return false;
            }

            if (!state.Unlocked)
            {
                return false;
            }

            if (!state.AwaitingReactivation)
            {
                return false;
            }

            return HasRequiredTexts(
                knowledgeDef);
        }
    }
}