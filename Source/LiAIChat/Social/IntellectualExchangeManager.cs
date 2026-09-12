using LiAIChat.AI;
using LiAIChat.Background;
using LiAIChat.Game;
using LiAIChat.Knowledge;
using LiAIChat.Models;
using LiAIChat.Pawns;
using LiAIChat.State;
using LiAIChat.Worldview;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Verse;

namespace LiAIChat.Social
{
    public static class IntellectualExchangeManager
    {
        private const int MaxStoredExchanges = 30;
        private static bool isRunning = false;
        private static IIntellectualExchangeDirector director;
        private static IIntellectualExchangeImpactDirector impactDirector;

        public static void Initialize(
            string apiKey)
        {
            director = new OpenAIIntellectualExchangeDirector(apiKey);
            impactDirector = new OpenAIIntellectualExchangeImpactDirector(apiKey);
        }

        public static async void TryCreateExchange()
        {
            if (isRunning)
                return;

            if (director == null)
                return;

            Map map = Find.CurrentMap;

            if (map == null)
                return;

            List<Pawn> pawns = map.mapPawns.FreeColonistsSpawned;

            if (pawns == null || pawns.Count < 2)
            {
                return;
            }

            isRunning = true;

            try
            {
                Pawn pawnA;
                Pawn pawnB;

                if (!TryChoosePair(pawns, out pawnA, out pawnB))
                {
                    return;
                }

                PawnAIState stateA = PawnAIStateManager.GetState(pawnA);

                PawnAIState stateB = PawnAIStateManager.GetState(pawnB);

                LiAIChatGameComponent component = Current.Game.GetComponent<LiAIChatGameComponent>();

                IntellectualExchangeSnapshot snapshot = new IntellectualExchangeSnapshot
                {
                    GameId = component.RuntimeGameId,

                    PawnA = PawnAISnapshotBuilder.Build(pawnA, stateA),

                    PawnB = PawnAISnapshotBuilder.Build(pawnB, stateB)
                };

                await RunExchange(snapshot);
            }
            catch (Exception ex)
            {
                Log.Warning(
                    "[Li AI Chat] Intellectual exchange failed: " +
                    ex);
            }
            finally
            {
                isRunning = false;
            }
        }

        private static bool TryChoosePair(List<Pawn> pawns, out Pawn pawnA, out Pawn pawnB)
        {
            pawnA = null;
            pawnB = null;

            if (pawns.Count < 2)
                return false;

            int firstIndex = Rand.Range(0, pawns.Count);

            int secondIndex = Rand.Range(0, pawns.Count - 1);

            if (secondIndex >= firstIndex)
                secondIndex++;

            pawnA = pawns[firstIndex];

            pawnB = pawns[secondIndex];

            return pawnA != null && pawnB != null;
        }

        private static async Task RunExchange(IntellectualExchangeSnapshot snapshot)
        {
            IntellectualExchangeResult exchange =
                await director.GenerateAsync(
                    snapshot.PawnA,
                    snapshot.PawnB);

            if (exchange == null ||
                !exchange.ShouldExchange ||
                string.IsNullOrWhiteSpace(
                    exchange.Summary))
            {
                return;
            }

            IntellectualExchangeImpactResult impact =
                await impactDirector.AnalyzeAsync(
                    snapshot.PawnA,
                    snapshot.PawnB,
                    exchange);

            MainThreadActionQueue.Enqueue(
                () =>
                {
                    ApplyCompletedExchange(
                        snapshot,
                        exchange,
                        impact);
                });
        }

        private static void ApplyCompletedExchange(IntellectualExchangeSnapshot snapshot, IntellectualExchangeResult exchange, IntellectualExchangeImpactResult impact)
        {
            if (Current.Game == null)
                return;

            LiAIChatGameComponent component =
                Current.Game.GetComponent<
                    LiAIChatGameComponent>();

            if (component == null)
                return;

            if (component.RuntimeGameId !=
                snapshot.GameId)
            {
                Log.Message(
                    "[Li AI Chat] Ignored AI result from an old game.");

                return;
            }

            Pawn pawnA = FindPawn(snapshot.PawnA.PawnId);

            Pawn pawnB = FindPawn(snapshot.PawnB.PawnId);

            if (pawnA == null || pawnB == null)
            {
                return;
            }

            if (pawnA.Dead || pawnB.Dead)
            {
                return;
            }

            if (!pawnA.IsColonist || !pawnB.IsColonist)
            {
                return;
            }
            PawnAIState stateA = PawnAIStateManager.GetState(pawnA);

            PawnAIState stateB = PawnAIStateManager.GetState(pawnB);
            RecordExchange(pawnA, stateA, pawnB, stateB, exchange);

            if (impact != null)
            {
                ApplyImpact(pawnA, stateA, pawnB, stateB, impact);
            }
        }

        private static void ApplyImpact(Pawn pawnA, PawnAIState stateA, Pawn pawnB, PawnAIState stateB, IntellectualExchangeImpactResult impact)
        {
            if (impact == null)
                return;

            ApplyPawnImpact(pawnA, stateA, impact.PawnA);

            ApplyPawnImpact(pawnB, stateB, impact.PawnB);
        }

        private static void ApplyPawnImpact(Pawn pawn, PawnAIState state, PawnExchangeImpact impact)
        {
            if (pawn == null || state == null || impact == null)
            {
                return;
            }

            // 1. Apply knowledge changes
            ApplyKnowledgeImpact(state, impact);

            // 2. Convert exchange impact into
            //    our normal WorldviewChange proposal
            WorldviewChange proposedChange = BuildWorldviewChange(impact);

            // 3. Let the existing Lesson 15 rules
            //    validate / scale / clamp the change
            AppliedWorldviewChange appliedChange = WorldviewUpdater.Apply(state.Worldview, proposedChange);

            Log.Message(
                "[Li AI Chat] Exchange impact on " +
                pawn.LabelShort +
                ": " +
                impact.Reason);
        }

        private static Pawn FindPawn(int pawnId)
        {
            foreach (Map map in Find.Maps)
            {
                foreach (Pawn pawn
                         in map.mapPawns.AllPawns)
                {
                    if (pawn.thingIDNumber ==
                        pawnId)
                    {
                        return pawn;
                    }
                }
            }

            return null;
        }

        private static void RecordExchange(Pawn pawnA, PawnAIState stateA, Pawn pawnB, PawnAIState stateB, IntellectualExchangeResult result)
        {
            int tick = Find.TickManager.TicksGame;

            stateA.IntellectualExchanges.Add(
                new PawnIntellectualExchange(pawnB.thingIDNumber, result.Summary, tick));

            stateB.IntellectualExchanges.Add(
                new PawnIntellectualExchange(pawnA.thingIDNumber, result.Summary, tick));

            string memoryA = "I had a meaningful conversation with " + pawnB.LabelShort + " about " + result.Topic + ". " + result.PawnAReflection;
            string memoryB = "I had a meaningful conversation with " + pawnA.LabelShort + " about " + result.Topic + ". " + result.PawnBReflection;

            // Importance = 0.70
            // 因为既然 Director 判断：ShouldExchange = true。说明这至少是一场值得保留的谈话。
            stateA.Memories.Add(new PawnMemory(memoryA, 0.70f));
            stateB.Memories.Add(new PawnMemory(memoryB, 0.70f));

            Log.Message("[Li AI Chat] Intellectual exchange: " + pawnA.LabelShort + " <-> " +
                pawnB.LabelShort + " | " + result.Topic + " | " + result.Summary);

            TrimExchanges(stateA);
            TrimExchanges(stateB);
        }

        /// <summary>
        /// 限制 Exchange 数量。因为真正长期重要的内容已经进入Memories。Exchange list 只需要保留近期交流。
        /// </summary>
        private static void TrimExchanges(PawnAIState state)
        {
            while (state.IntellectualExchanges.Count > MaxStoredExchanges)
            {
                state.IntellectualExchanges.RemoveAt(0);
            }
        }
        private static WorldviewChange BuildWorldviewChange(PawnExchangeImpact impact)
        {
            return new WorldviewChange
            {
                BeliefInGodDelta = impact.BeliefInGodDelta,

                BeliefInObjectiveMoralityDelta = impact.BeliefInObjectiveMoralityDelta,

                TrustInChristianityDelta = impact.TrustInChristianityDelta,

                KnowledgeOfChristianityDelta = impact.KnowledgeOfChristianityDelta,

                IntellectualResistanceDelta = impact.IntellectualResistanceDelta,

                EmotionalResistanceDelta = impact.EmotionalResistanceDelta,

                SpiritualInterestDelta = impact.SpiritualInterestDelta,

                Reason = impact.Reason
            };
        }

        private static void ApplyKnowledgeImpact(PawnAIState state, PawnExchangeImpact impact)
        {
            if (state == null || state.Knowledge == null || impact == null)
            {
                return;
            }
            if (string.IsNullOrWhiteSpace(impact.LearnedTopicId))
            {
                return;
            }

            KnowledgeAcquisition acquisition = new KnowledgeAcquisition
            {
                TopicId = impact.LearnedTopicId,

                LearningStrength = impact.LearningStrength,

                Reason = impact.Reason
            };

            KnowledgeUpdater.Apply(state.Knowledge, acquisition);
        }
    }
}