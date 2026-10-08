using LiAIChat.Archive;
using LiAIChat.Models;
using LiAIChat.Events;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace LiAIChat.Dialogue
{
    public static class ProactiveDialogueRules
    {
        public const int MinimumCooldownTicks = 180000;

        private class TriggerOption
        {
            public string Source;
            public string Instruction;
            public float Weight;
        }

        public static bool TryGetTrigger(Pawn pawn, PawnAIState state, out string source, out string instruction)
        {
            source = null;
            instruction = null;
            if (pawn == null || state == null || state.HasPendingProactiveDialogue || Find.TickManager == null)
                return false;
            if (Find.TickManager.TicksGame - state.LastProactiveDialogueTick < MinimumCooldownTicks)
                return false;

            List<TriggerOption> options = new List<TriggerOption>();
            AddLifeEventOption(state, options);
            AddLifeGoalOption(state, options);
            AddKnowledgeOption(pawn, state, options);
            AddExchangeOption(state, options);
            AddColonyEventOption(pawn, options);
            AddMoodOption(pawn, state, options);
            if (options.Count == 0)
                return false;

            bool hasUrgentPersonalEvent = options.Any(option => option.Source == "life-event" || option.Source == "life-goal");
            if (!Rand.Chance(hasUrgentPersonalEvent ? 0.55f : 0.20f))
                return false;

            float roll = Rand.Value * options.Sum(option => option.Weight);
            foreach (TriggerOption option in options)
            {
                roll -= option.Weight;
                if (roll > 0f) continue;
                source = option.Source;
                instruction = option.Instruction;
                return true;
            }
            TriggerOption fallback = options[options.Count - 1];
            source = fallback.Source;
            instruction = fallback.Instruction;
            return true;
        }

        private static void AddLifeEventOption(PawnAIState state, List<TriggerOption> options)
        {
            PawnLifeEvent lifeEvent = state.LifeEvents == null ? null : state.LifeEvents.LastOrDefault(item => item != null && !item.ProactiveDialogueUsed);
            if (lifeEvent == null) return;
            options.Add(new TriggerOption
            {
                Source = "life-event", Weight = 1.5f,
                Instruction = "Open with the unresolved personal event below. Be compassionate and specific, but do not turn it into melodrama. Event: " + lifeEvent.Description
            });
        }

        private static void AddLifeGoalOption(PawnAIState state, List<TriggerOption> options)
        {
            if (state.LifeGoal == null || !state.LifeGoal.IsActive || state.LifeGoal.ProactiveDialogueUsed || state.LifeGoal.Commitment < 0.5f) return;
            options.Add(new TriggerOption
            {
                Source = "life-goal", Weight = 1.2f,
                Instruction = "Open by discussing a practical hope, doubt, or next step connected to this personal goal: " + state.LifeGoal.Title
            });
        }

        private static void AddKnowledgeOption(Pawn pawn, PawnAIState state, List<TriggerOption> options)
        {
            int intellectual = pawn?.skills?.GetSkill(SkillDefOf.Intellectual)?.Level ?? 0;
            EarthTextDef literaryText = SelectLiteraryText(pawn, state);

            // A capable reader who has recently opened an Earth text should
            // usually approach the player as a reader: this weight deliberately
            // outweighs the generic mood, event, and colony-news prompts.
            if (intellectual > 10 && literaryText != null)
            {
                float familiarity = state.GetEarthTextFamiliarity(literaryText.defName);
                options.Add(new TriggerOption
                {
                    Source = "earth-text-reading",
                    Weight = 12.0f,
                    Instruction = BuildLiteraryReadingInstruction(literaryText, familiarity)
                });
                return;
            }

            bool hasTexts = state.EarthTextFamiliarity != null && state.EarthTextFamiliarity.Any(pair => pair.Value >= 0.2f);
            bool hasTopics = state.Knowledge?.KnownTopics != null && state.Knowledge.KnownTopics.Any(topic => topic != null && topic.Familiarity >= 0.2f);
            if (!hasTexts && !hasTopics) return;
            options.Add(new TriggerOption
            {
                Source = "knowledge", Weight = 1.0f,
                Instruction = "Open with a curious, grounded thought or question about an Ancient Earth text or topic the character has studied. It may be philosophical, historical, religious, political, or scientific; avoid presenting it as a tragedy unless the state supports that."
            });
        }

        private static EarthTextDef SelectLiteraryText(Pawn pawn, PawnAIState state)
        {
            if (state == null)
                return null;

            int now = Find.TickManager == null ? 0 : Find.TickManager.TicksGame;
            // A reading session remains relevant through the next daily
            // proactive scan, but not indefinitely.
            if (!string.IsNullOrEmpty(state.RecentEarthTextReadingId) &&
                now - state.RecentEarthTextReadingTick <= 2 * GenDate.TicksPerDay)
            {
                EarthTextDef current = DefDatabase<EarthTextDef>.GetNamedSilentFail(
                    state.RecentEarthTextReadingId);
                if (current != null)
                    return current;
            }

            if (state.EarthTextFamiliarity == null)
                return null;

            return state.EarthTextFamiliarity
                .Where(pair => pair.Value > 0f)
                .OrderByDescending(pair => pair.Value)
                .Select(pair => DefDatabase<EarthTextDef>.GetNamedSilentFail(pair.Key))
                .FirstOrDefault(text => text != null);
        }

        private static string BuildLiteraryReadingInstruction(
            EarthTextDef text,
            float familiarity)
        {
            string title = EarthTextEndorsementUtility.Title(text);
            DocumentRecovery.DocumentLine line = DocumentRecovery.GetLine(text.defName);
            string chapter = "No recovered chapter title is available.";
            if (line != null && line.Titles != null && line.Titles.Length > 0)
            {
                RecoveredDocumentState recovered = DocumentRecovery.GetState(text.defName);
                int index = recovered?.UnlockedSectionIds == null || recovered.UnlockedSectionIds.Count == 0
                    ? 0
                    : Mathf.Min(recovered.UnlockedSectionIds.Count - 1, line.Titles.Length - 1);
                chapter = line.Titles[index];
            }

            string depth;
            if (familiarity < 0.15f)
                depth = "They are a new reader. Ask one concrete, accessible question about what a phrase, claim, or chapter theme means. Do not pretend to understand the book already.";
            else if (familiarity < 0.40f)
                depth = "They have grasped the outline. Ask why the author frames the issue this way, what the chapter is trying to establish, or how its historical setting matters.";
            else if (familiarity < 0.70f)
                depth = "They can trace an argument. Ask about a tension, implication, or possible objection, or share a tentative assessment tied to colony life.";
            else
                depth = "They know the work well. Share a considered whole-book impression or pose a precise, debatable interpretation; do not ask a beginner-level question.";

            return "The character has recently been reading the Ancient Earth text 《" + title + "》 by " + text.author + ". " +
                "Its recorded central concerns are: " + text.shortDescription + ". " +
                "A relevant recovered chapter is: " + chapter + ". " +
                "Their familiarity with this work is " + familiarity.ToString("0.00") + ". " +
                depth + " The opener must chiefly be a natural question to the player about this reading, or a brief personal sharing of its ideas. Do not switch to bereavement, work fatigue, or generic existential loneliness unless the text itself makes that connection necessary.";
        }

        private static void AddExchangeOption(PawnAIState state, List<TriggerOption> options)
        {
            PawnIntellectualExchange exchange = state.IntellectualExchanges == null ? null : state.IntellectualExchanges.LastOrDefault(item => item != null && !string.IsNullOrWhiteSpace(item.Summary));
            if (exchange == null) return;
            options.Add(new TriggerOption
            {
                Source = "intellectual-exchange", Weight = 0.9f,
                Instruction = "Open by returning to this recent discussion with another colonist, perhaps to test an idea or ask for perspective: " + exchange.Summary
            });
        }

        private static void AddMoodOption(Pawn pawn, PawnAIState state, List<TriggerOption> options)
        {
            float mood = pawn.needs?.mood == null ? 0.5f : pawn.needs.mood.CurLevelPercentage;
            if (mood < 0.28f || state.Meaning == null || state.Meaning.Purpose < 0.3f || state.Meaning.Hope < 0.3f || state.Meaning.Coherence < 0.3f)
            {
                options.Add(new TriggerOption
                {
                    Source = "wellbeing", Weight = 1.1f,
                    Instruction = "Open with a restrained, personal request to talk about uncertainty, fatigue, purpose, or hope. Do not invent a death or disaster."
                });
            }
        }

        private static void AddColonyEventOption(Pawn pawn, List<TriggerOption> options)
        {
            string eventContext = ColonyEventLog.BuildConversationContext(pawn, 2);
            if (string.IsNullOrWhiteSpace(eventContext)) return;
            options.Add(new TriggerOption
            {
                Source = "colony-event", Weight = 0.8f,
                Instruction = "Open with a calm, personal observation or question about a recent colony development below. Use it only if it suits this character; do not automatically focus on a death.\n" + eventContext
            });
        }
    }
}
