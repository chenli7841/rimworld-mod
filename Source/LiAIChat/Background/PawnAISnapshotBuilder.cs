using LiAIChat.Models;
using RimWorld;
using System;
using System.Linq;
using Verse;

namespace LiAIChat.Background
{
    public static class PawnAISnapshotBuilder
    {
        public static PawnAISnapshot Build(
            Pawn pawn,
            PawnAIState state)
        {
            PawnAISnapshot snapshot =
                new PawnAISnapshot();

            snapshot.PawnId =
                pawn.thingIDNumber;

            snapshot.Name =
                pawn.LabelShort;

            snapshot.CurrentRelationships = PawnRelationshipContextBuilder.Build(pawn);

            if (pawn.story?.traits?.allTraits != null)
            {
                foreach (Trait trait
                         in pawn.story.traits.allTraits)
                {
                    snapshot.Traits.Add(
                        trait.Label);
                }
            }

            CopyWorldview(
                snapshot,
                state);

            CopyMeaning(
                snapshot,
                state);

            CopyKnowledge(
                snapshot,
                state);

            CopyMemories(
                snapshot,
                state);

            CopyLifeGoal(
                snapshot,
                state);

            return snapshot;
        }
        private static void CopyWorldview(PawnAISnapshot snapshot, PawnAIState state)
        {
            if (state.Worldview == null)
                return;

            snapshot.BeliefInGod =
                state.Worldview.BeliefInGod;

            snapshot.BeliefInObjectiveMorality =
                state.Worldview.BeliefInObjectiveMorality;

            snapshot.TrustInChristianity =
                state.Worldview.TrustInChristianity;

            snapshot.KnowledgeOfChristianity =
                state.Worldview.KnowledgeOfChristianity;

            snapshot.IntellectualResistance =
                state.Worldview.IntellectualResistance;

            snapshot.EmotionalResistance =
                state.Worldview.EmotionalResistance;

            snapshot.SpiritualInterest =
                state.Worldview.SpiritualInterest;
        }
        private static void CopyMeaning(PawnAISnapshot snapshot, PawnAIState state)
        {
            if (state.Meaning == null)
                return;

            snapshot.Purpose =
                state.Meaning.Purpose;

            snapshot.Belonging =
                state.Meaning.Belonging;

            snapshot.Hope =
                state.Meaning.Hope;

            snapshot.Coherence =
                state.Meaning.Coherence;

            snapshot.Transcendence =
                state.Meaning.Transcendence;
        }
        private static void CopyKnowledge(PawnAISnapshot snapshot, PawnAIState state)
        {
            if (state.Knowledge?.KnownTopics == null)
                return;

            foreach (KnowledgeTopic topic
                     in state.Knowledge.KnownTopics)
            {
                if (topic == null)
                    continue;

                snapshot.KnownTopics.Add(
                    new KnowledgeTopicSnapshot
                    {
                        TopicId =
                            topic.TopicId,

                        Familiarity =
                            topic.Familiarity
                    });
            }
        }
        private static void CopyMemories(PawnAISnapshot snapshot, PawnAIState state)
        {
            if (state.Memories == null)
                return;

            foreach (PawnMemory memory
                     in state.Memories
                         .Where(m =>
                             m != null &&
                             !string.IsNullOrWhiteSpace(
                                 m.Text))
                         .OrderByDescending(
                             m => m.Importance)
                         .Take(8))
            {
                snapshot.ImportantMemories.Add(
                    memory.Text);
            }
        }

        private static void CopyLifeGoal(PawnAISnapshot snapshot, PawnAIState state)
        {
            if (state.LifeGoal == null || !state.LifeGoal.IsActive)
            {
                return;
            }

            snapshot.LifeGoalTitle = state.LifeGoal.Title;

            snapshot.LifeGoalDescription = state.LifeGoal.Description;
        }
    }
}