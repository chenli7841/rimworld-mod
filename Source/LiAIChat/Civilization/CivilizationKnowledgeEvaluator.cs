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

            UpdateAllDormantStates();

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

            bool changed;

            do
            {
                changed = false;

                foreach (CivilizationKnowledgeDef def
                    in defs)
                {
                    if (Evaluate(def))
                    {
                        changed = true;
                    }
                }
            }
            while (changed);
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
    .HasPrerequisites(knowledgeDef))
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
            LiAIChat.Events.ColonyEventLog.Record("文明知识", "殖民地重建了“" + knowledgeDef.label + "”文明知识。", 3, null,
                "civilization-unlocked:" + knowledgeDef.defName);

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

            HandleTextRestoration(
                knowledgeDef);
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

            if (state.Dormant)
            {
                state.Unstable = false;
                return;
            }

            if (state.AwaitingReactivation)
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
        public static void UpdateDormantState(
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
                state.Dormant = false;
                state.AwaitingReactivation = false;
                return;
            }

            if (state.AwaitingReactivation)
            {
                state.Dormant = false;
                return;
            }

            if (CivilizationKnowledgeUtility
                .ShouldBeDormant(
                    knowledgeDef))
            {
                state.Dormant = true;
                state.Unstable = false;
                return;
            }
        }
        public static void UpdateAllDormantStates()
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
                UpdateDormantState(def);
            }
        }

        public static void HandleTextRestoration(
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
                return;
            }

            bool complete =
                CivilizationKnowledgeUtility
                    .HasRequiredTexts(
                        knowledgeDef);

            if (!complete)
            {
                return;
            }

            state.MissingSinceTick = -1;

            // Once a knowledge system has fallen dormant and entered
            // reactivation mode, restoring the books alone must not
            // automatically reactivate it.
            if (state.AwaitingReactivation)
            {
                state.Dormant = false;
                state.Unstable = false;
                return;
            }

            // First restoration after actual Dormancy.
            if (state.Dormant)
            {
                state.Dormant = false;
                state.Unstable = false;
                state.AwaitingReactivation = true;

                return;
            }

            // Knowledge never reached Dormant.
            // Restoration during Grace / Unstable restores it directly.
            if (state.Unstable)
            {
                state.Unstable = false;
            }
        }
    }
}
