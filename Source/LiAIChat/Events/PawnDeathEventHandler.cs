using LiAIChat.Models;
using LiAIChat.State;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace LiAIChat.Events
{
    public static class PawnDeathEventHandler
    {
        public static void HandleDeath(
            Pawn deadPawn)
        {
            if (deadPawn == null)
                return;

            if (Current.Game == null)
                return;

            Map map =
                deadPawn.MapHeld;

            if (map == null)
                return;

            List<Pawn> pawns =
                map.mapPawns.FreeColonistsSpawned;

            foreach (Pawn observer in pawns)
            {
                if (observer == null)
                    continue;

                if (observer == deadPawn)
                    continue;

                PawnAIState state =
                    PawnAIStateManager.GetState(
                        observer);

                if (state == null)
                    continue;

                foreach (PawnLifeEvent existing in state.LifeEvents)
                {
                    if (existing.Type == "PawnDeath" &&
                        existing.SubjectPawnId ==
                        deadPawn.thingIDNumber)
                    {
                        return;
                    }
                }

                RecordDeath(
                    observer,
                    state,
                    deadPawn);
            }
        }

        private static void RecordDeath(
            Pawn observer,
            PawnAIState state,
            Pawn deadPawn)
        {
            foreach (PawnLifeEvent existing
                     in state.LifeEvents)
            {
                if (existing.Type == "PawnDeath" &&
                    existing.SubjectPawnId ==
                    deadPawn.thingIDNumber)
                {
                    return;
                }
            }
            string deadName =
                deadPawn.LabelShort;

            string description =
                deadName +
                " died in the colony.";

            bool wasSpouse =
                observer.relations != null &&
                observer.relations.DirectRelationExists(
                    PawnRelationDefOf.Spouse,
                    deadPawn);

            if (wasSpouse)
            {
                description =
                    observer.LabelShort +
                    "'s spouse " +
                    deadName +
                    " died.";
            }
            else
            {
                description =
                    deadName +
                    " died in the colony.";
            }


            PawnLifeEvent lifeEvent =
                new PawnLifeEvent(
                    "PawnDeath",
                    description,
                    deadPawn.thingIDNumber,
                    Find.TickManager.TicksGame);

            state.LifeEvents.Add(
                lifeEvent);

            float importance =
                wasSpouse
                    ? 1.0f
                    : 0.85f;

            state.Memories.Add(
                new PawnMemory(
                    description,
                    importance));

            ApplyMeaningImpact(
                state.Meaning, wasSpouse);

            Log.Message(
                "[Li AI Chat] Death impact for " +
                observer.LabelShort +
                ": Hope=" +
                state.Meaning.Hope.ToString("0.00") +
                ", Belonging=" +
                state.Meaning.Belonging.ToString("0.00") +
                ", Coherence=" +
                state.Meaning.Coherence.ToString("0.00"));



        }
        private static void ApplyMeaningImpact(MeaningState meaning, bool wasSpouse)
        {
            if (meaning == null)
                return;

            if (wasSpouse)
            {
                meaning.Hope =
                    Clamp01(
                        meaning.Hope - 0.08f);

                meaning.Belonging =
                    Clamp01(
                        meaning.Belonging - 0.08f);

                meaning.Coherence =
                    Clamp01(
                        meaning.Coherence - 0.10f);

                meaning.Purpose =
                    Clamp01(
                        meaning.Purpose - 0.05f);
            }
            else
            {
                meaning.Hope =
                    Clamp01(
                        meaning.Hope - 0.02f);

                meaning.Coherence =
                    Clamp01(
                        meaning.Coherence - 0.02f);
            }
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