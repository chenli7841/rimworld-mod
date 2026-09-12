using System.Collections.Generic;
using System.Linq;
using Verse;

namespace LiAIChat.Civilization
{
    public static class CivilizationKnowledgeManager
    {
        private static
            CivilizationKnowledgeGameComponent
            Component
        {
            get
            {
                if (Current.Game == null)
                {
                    return null;
                }

                return Current.Game
                    .GetComponent<
                        CivilizationKnowledgeGameComponent>();
            }
        }

        public static CivilizationKnowledgeState
            GetState(
                CivilizationKnowledgeDef knowledgeDef)
        {
            if (knowledgeDef == null)
            {
                return null;
            }

            return GetState(
                knowledgeDef.defName);
        }

        public static CivilizationKnowledgeState
            GetState(
                string knowledgeDefName)
        {
            if (string.IsNullOrWhiteSpace(
                knowledgeDefName))
            {
                return null;
            }

            CivilizationKnowledgeGameComponent
                component =
                    Component;

            if (component == null)
            {
                return null;
            }

            CivilizationKnowledgeState state =
                component.States
                    .FirstOrDefault(
                        item =>
                            item != null &&
                            item.KnowledgeDefName ==
                                knowledgeDefName);

            if (state != null)
            {
                return state;
            }

            state =
                new CivilizationKnowledgeState
                {
                    KnowledgeDefName =
                        knowledgeDefName
                };

            component.States.Add(
                state);

            return state;
        }

        public static bool IsUnlocked(
            CivilizationKnowledgeDef knowledgeDef)
        {
            CivilizationKnowledgeState state =
                GetState(
                    knowledgeDef);

            return state != null &&
                   state.Unlocked;
        }
        public static bool Unlock(
            CivilizationKnowledgeDef knowledgeDef)
        {
            if (knowledgeDef == null)
            {
                return false;
            }

            CivilizationKnowledgeState state =
                GetState(
                    knowledgeDef);

            if (state == null)
            {
                return false;
            }

            if (state.Unlocked)
            {
                return false;
            }

            state.Unlocked = true;

            if (Find.TickManager != null)
            {
                state.UnlockedTick =
                    Find.TickManager.TicksGame;
            }
            else
            {
                state.UnlockedTick = -1;
            }

            return true;
        }
        public static bool IsUnstable(
    CivilizationKnowledgeDef knowledgeDef)
        {
            CivilizationKnowledgeState state =
                GetState(knowledgeDef);

            return state != null &&
                   state.Unlocked &&
                   state.Unstable;
        }
        public static bool IsDormant(
    CivilizationKnowledgeDef knowledgeDef)
        {
            CivilizationKnowledgeState state =
                GetState(knowledgeDef);

            return state != null &&
                   state.Unlocked &&
                   state.Dormant;
        }
        public static bool IsActive(
    CivilizationKnowledgeDef knowledgeDef)
        {
            CivilizationKnowledgeState state =
                GetState(knowledgeDef);

            if (state == null)
            {
                return false;
            }

            return state.Unlocked &&
               !state.Unstable &&
               !state.Dormant &&
               !state.AwaitingReactivation;
        }
        public static bool IsAwaitingReactivation(
    CivilizationKnowledgeDef knowledgeDef)
        {
            CivilizationKnowledgeState state =
                GetState(knowledgeDef);

            return state != null &&
                   state.Unlocked &&
                   state.AwaitingReactivation;
        }
        public static bool Reactivate(
    CivilizationKnowledgeDef knowledgeDef)
        {
            if (knowledgeDef == null)
            {
                return false;
            }

            if (!CivilizationKnowledgeUtility
                .CanReactivate(knowledgeDef))
            {
                return false;
            }

            CivilizationKnowledgeState state =
                GetState(knowledgeDef);

            if (state == null)
            {
                return false;
            }

            state.AwaitingReactivation = false;
            state.Unstable = false;
            state.Dormant = false;
            state.MissingSinceTick = -1;

            return true;
        }
    }
}