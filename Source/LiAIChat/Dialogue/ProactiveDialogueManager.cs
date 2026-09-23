using LiAIChat.AI;
using LiAIChat.Archive;
using LiAIChat.Models;
using LiAIChat.Pawns;
using LiAIChat.State;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Verse;

namespace LiAIChat.Dialogue
{
    public static class ProactiveDialogueManager
    {
        private static bool isChecking = false;

        private static IProactiveDialogueDirector
            dialogueDirector;


        public static void Initialize(
            string apiKey)
        {
            dialogueDirector =
                new OpenAIProactiveDialogueDirector(
                    apiKey);
        }


        public static async void CheckForProactiveDialogue()
        {
            if (isChecking)
            {
                return;
            }

            if (dialogueDirector == null)
            {
                return;
            }

            if (Current.Game == null)
            {
                return;
            }

            Map map =
                Find.CurrentMap;

            if (map == null)
            {
                return;
            }

            isChecking = true;

            try
            {
                List<Pawn> candidates = map.mapPawns.FreeColonistsSpawned;

                if (Prefs.DevMode)
                {
                    Log.Message("[Li AI Chat] Proactive scan: FreeColonistsSpawned count=" + candidates.Count);
                }
                foreach (Pawn pawn in candidates)
                {
                    PawnAIState state = PawnAIStateManager.GetState(pawn);

                    if (state == null)
                    {
                        continue;
                    }

                    bool isScholar = state != null && state.ScholarProfile != null;

                    if (Prefs.DevMode)
                    {
                        Log.Message(
                            "[Li AI Chat] Proactive candidate: " +
                            pawn.LabelShort +
                            ", IsColonist=" +
                            pawn.IsColonist +
                            ", IsQuestLodger=" +
                            pawn.IsQuestLodger() +
                            ", IsScholar=" +
                            isScholar);
                    }



                    // =====================================================
                    // Build snapshot/context once on the main thread.
                    // =====================================================

                    PawnContext context =
                        PawnContextBuilder.Build(
                            pawn);

                    if (context == null)
                    {
                        continue;
                    }


                    // =====================================================
                    // 1. Check normal proactive-dialogue rules.
                    // =====================================================

                    string normalTriggerSource;
                    string normalTriggerInstruction;
                    bool normalTrigger = ProactiveDialogueRules.TryGetTrigger(
                        pawn, state, out normalTriggerSource, out normalTriggerInstruction);


                    // =====================================================
                    // 2. Check Scholar-specific quest trigger.
                    //
                    // This is independent from normal proactive rules.
                    // A Scholar should still be able to initiate an
                    // arrival/farewell conversation even if the normal
                    // proactive cooldown / LifeEvent rules do not trigger.
                    // =====================================================

                    string scholarTrigger =
                        ArchiveScholarProactiveDialogue
                            .TryGetTrigger(
                                context,
                                state);


                    // =====================================================
                    // Nothing wants to start a dialogue.
                    // =====================================================

                    if (!normalTrigger &&
                        string.IsNullOrEmpty(
                            scholarTrigger))
                    {
                        continue;
                    }


                    // Scholar quest dialogue takes priority if both
                    // happen to become eligible at the same time.
                    await CreateRequest(
                        pawn,
                        context,
                        state,
                        normalTrigger,
                        normalTriggerSource,
                        normalTriggerInstruction,
                        scholarTrigger);


                    // 一次扫描只触发一个。
                    // 避免多个 Pawns 同时主动谈话轰炸玩家。
                    // 以后可以做队列。
                    break;
                }
            }
            catch (Exception ex)
            {
                Log.Warning(
                    "[Li AI Chat] Proactive dialogue check failed: " +
                    ex);
            }
            finally
            {
                isChecking = false;
            }
        }


        private static async Task CreateRequest(
            Pawn pawn,
            PawnContext context,
            PawnAIState state,
            bool normalTrigger,
            string normalTriggerSource,
            string normalTriggerInstruction,
            string scholarTrigger)
        {
            if (pawn == null ||
                context == null ||
                state == null)
            {
                return;
            }


            // =========================================================
            // Build special Scholar instruction if this request was
            // triggered by the Scholar quest lifecycle.
            // =========================================================

            string specialInstruction = null;

            bool isScholarDialogue =
                !string.IsNullOrEmpty(
                    scholarTrigger);

            if (isScholarDialogue)
            {
                specialInstruction = ArchiveScholarProactiveDialogue.BuildPromptInstruction(scholarTrigger, state);
            }
            else if (normalTrigger)
            {
                specialInstruction = normalTriggerInstruction;
            }


            // =========================================================
            // Ask the existing Proactive Dialogue Director.
            // =========================================================

            string opener =
                await dialogueDirector
                    .GenerateAsync(
                        context,
                        state,
                        specialInstruction);


            // Nothing was successfully generated.
            // Do NOT consume Scholar trigger / LifeEvent / LifeGoal.
            if (string.IsNullOrWhiteSpace(
                opener))
            {
                return;
            }


            // =========================================================
            // Save pending proactive dialogue.
            // =========================================================

            state.PendingProactiveDialogue =
                opener;

            state.HasPendingProactiveDialogue =
                true;

            state.LastProactiveDialogueTick =
                Find.TickManager.TicksGame;


            // =========================================================
            // Scholar lifecycle trigger:
            //
            // Only mark it AFTER an opener was successfully generated.
            // Otherwise API failure / rejection could permanently lose
            // the dialogue opportunity.
            // =========================================================

            if (isScholarDialogue)
            {
                ArchiveScholarProactiveDialogue
                    .MarkTriggered(
                        state,
                        scholarTrigger);
            }


            // =========================================================
            // Normal proactive dialogue bookkeeping.
            //
            // IMPORTANT:
            // Do NOT consume an unrelated LifeEvent or LifeGoal when
            // this conversation was specifically triggered by the
            // Scholar quest.
            // =========================================================

            if (!isScholarDialogue &&
                normalTrigger)
            {
                MarkNormalProactiveSourceUsed(
                    state,
                    normalTriggerSource);
            }


            // =========================================================
            // Notify player.
            // =========================================================

            ShowNotification(
                pawn,
                opener);
        }


        private static void MarkNormalProactiveSourceUsed(
            PawnAIState state,
            string source)
        {
            if (state == null)
            {
                return;
            }


            if (source == "life-event" && state.LifeEvents != null)
            {
                for (int i = state.LifeEvents.Count - 1; i >= 0; i--)
                {
                    PawnLifeEvent lifeEvent = state.LifeEvents[i];
                    if (lifeEvent != null &&
                        !lifeEvent.ProactiveDialogueUsed)
                    {
                        lifeEvent.ProactiveDialogueUsed =
                            true;
                        return;
                    }
                }
            }


            // =========================================================
            // Otherwise mark LifeGoal if it has not been used.
            // =========================================================

            if (source == "life-goal" && state.LifeGoal != null &&
                !state.LifeGoal
                    .ProactiveDialogueUsed)
            {
                state.LifeGoal
                    .ProactiveDialogueUsed =
                    true;
            }
        }


        private static void ShowNotification(
            Pawn pawn,
            string opener)
        {
            string label =
                pawn.LabelShort +
                " wants to talk";

            string text =
                opener +
                "\n\n" +
                "Select " +
                pawn.LabelShort +
                " and use the Talk command to continue the conversation.";

            Find.LetterStack.ReceiveLetter(
                label,
                text,
                LetterDefOf.NeutralEvent,
                pawn);
        }
    }
}
