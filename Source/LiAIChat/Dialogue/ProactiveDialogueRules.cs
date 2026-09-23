using LiAIChat.Models;
using LiAIChat.Events;
using System.Collections.Generic;
using System.Linq;
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
            AddKnowledgeOption(state, options);
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

        private static void AddKnowledgeOption(PawnAIState state, List<TriggerOption> options)
        {
            bool hasTexts = state.EarthTextFamiliarity != null && state.EarthTextFamiliarity.Any(pair => pair.Value >= 0.2f);
            bool hasTopics = state.Knowledge?.KnownTopics != null && state.Knowledge.KnownTopics.Any(topic => topic != null && topic.Familiarity >= 0.2f);
            if (!hasTexts && !hasTopics) return;
            options.Add(new TriggerOption
            {
                Source = "knowledge", Weight = 1.0f,
                Instruction = "Open with a curious, grounded thought or question about an Ancient Earth text or topic the character has studied. It may be philosophical, historical, religious, political, or scientific; avoid presenting it as a tragedy unless the state supports that."
            });
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
