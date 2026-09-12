using RimWorld;
using System.Collections.Generic;
using Verse;

namespace LiAIChat.Civilization
{
    public static class CivilizationKnowledgeEvaluator
    {
        public static void NotifyLibraryMayHaveChanged()
        {
            EvaluateAll();

            UpdateAllMissingStates();

            UpdateAllUnstableStates();
        }
        public static void UpdateAllMissingStates()
        {
            List<CivilizationKnowledgeDef> defs =
                DefDatabase<CivilizationKnowledgeDef>
                    .AllDefsListForReading;

            if (defs == null)
            {
                return;
            }

            foreach (CivilizationKnowledgeDef def
                in defs)
            {
                UpdateMissingState(def);
            }
        }

        public static void EvaluateAll()
        {
            List<CivilizationKnowledgeDef> defs =
                DefDatabase<CivilizationKnowledgeDef>
                    .AllDefsListForReading;

            if (defs == null)
            {
                return;
            }

            foreach (CivilizationKnowledgeDef def
                in defs)
            {
                Evaluate(def);
            }
        }

        public static bool Evaluate(
            CivilizationKnowledgeDef knowledgeDef)
        {
            if (knowledgeDef == null)
            {
                return false;
            }

            if (CivilizationKnowledgeManager
                .IsUnlocked(knowledgeDef))
            {
                return false;
            }

            if (!CivilizationKnowledgeUtility
                .HasRequiredTexts(knowledgeDef))
            {
                return false;
            }

            bool unlocked =
                CivilizationKnowledgeManager
                    .Unlock(knowledgeDef);

            if (!unlocked)
            {
                return false;
            }

            Messages.Message(
                "Civilization knowledge reconstructed: "
                + knowledgeDef.label,
                MessageTypeDefOf.PositiveEvent);

            return true;
        }

        public static void UpdateMissingState(
    CivilizationKnowledgeDef knowledgeDef)
        {
            if (knowledgeDef == null)
            {
                return;
            }

            CivilizationKnowledgeState state =
                CivilizationKnowledgeManager
                    .GetState(knowledgeDef);

            if (state == null)
            {
                return;
            }

            if (!state.Unlocked)
            {
                state.MissingSinceTick = -1;
                return;
            }

            bool incomplete =
                CivilizationKnowledgeUtility
                    .IsUnlockedButIncomplete(
                        knowledgeDef);

            if (incomplete)
            {
                if (state.MissingSinceTick < 0)
                {
                    state.MissingSinceTick =
                        GetCurrentGameTick();
                }

                return;
            }

            state.MissingSinceTick = -1;
        }

        private static int GetCurrentGameTick()
        {
            if (Find.TickManager == null)
            {
                return -1;
            }

            return Find.TickManager.TicksGame;
        }

        public static void UpdateUnstableState(
    CivilizationKnowledgeDef knowledgeDef)
        {
            if (knowledgeDef == null)
            {
                return;
            }

            CivilizationKnowledgeState state =
                CivilizationKnowledgeManager
                    .GetState(knowledgeDef);

            if (state == null)
            {
                return;
            }

            if (!state.Unlocked)
            {
                state.Unstable = false;
                return;
            }

            bool shouldBeUnstable =
                CivilizationKnowledgeUtility
                    .IsUnlockedButIncomplete(
                        knowledgeDef)
                &&
                CivilizationKnowledgeUtility
                    .HasGracePeriodExpired(
                        knowledgeDef);

            state.Unstable =
                shouldBeUnstable;
        }

        public static void UpdateAllUnstableStates()
        {
            List<CivilizationKnowledgeDef> defs =
                DefDatabase<CivilizationKnowledgeDef>
                    .AllDefsListForReading;

            if (defs == null)
            {
                return;
            }

            foreach (CivilizationKnowledgeDef def
                in defs)
            {
                UpdateUnstableState(def);
            }
        }
    }
}