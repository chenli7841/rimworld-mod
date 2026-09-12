using LiAIChat.Background;
using LiAIChat.Config;
using LiAIChat.Game;
using LiAIChat.Models;
using LiAIChat.State;
using System;
using System.Net.Http;
using Verse;

namespace LiAIChat.Archive
{
    public static class ArchiveReflectionService
    {
        private static readonly HttpClient HttpClient =
            new HttpClient();

        public static void GenerateReflection(
            Pawn pawn,
            Thing_AncientEarthArchiveFragment archive)
        {
            if (pawn == null ||
                archive == null ||
                archive.Content == null)
            {
                return;
            }

            LiAIChatGameComponent component =
                Current.Game
                    .GetComponent<LiAIChatGameComponent>();

            if (component == null)
            {
                return;
            }

            string runtimeGameId =
                component.RuntimeGameId.ToString();

            int pawnId =
                pawn.thingIDNumber;

            PawnAIState state =
    PawnAIStateManager.GetState(pawn);

            if (state == null)
            {
                return;
            }

            PawnAISnapshot snapshot =
                PawnAISnapshotBuilder.Build(pawn, state);

            ArchiveContentDef content =
                archive.Content;

            string sourceDescription =
                archive.SourceDescription;

            IArchiveReflectionGenerator generator =
                new OpenAIArchiveReflectionGenerator(
                    HttpClient,
                    Config.Config.OpenAI_API_KEY,
                    "gpt-5.6-luna");

            _ = GenerateAsync(
                generator,
                snapshot,
                content,
                sourceDescription,
                runtimeGameId,
                pawnId);
        }

        private static async System.Threading.Tasks.Task GenerateAsync(
            IArchiveReflectionGenerator generator,
            PawnAISnapshot snapshot,
            ArchiveContentDef content,
            string sourceDescription,
            string runtimeGameId,
            int pawnId)
        {
            try
            {
                string reflection =
                    await generator.GenerateAsync(
                        snapshot,
                        content,
                        sourceDescription);

                if (string.IsNullOrWhiteSpace(
                    reflection))
                {
                    return;
                }

                MainThreadActionQueue.Enqueue(
                    delegate
                    {
                        ApplyReflection(
                            runtimeGameId,
                            pawnId,
                            content.defName,
                            reflection);
                    });
            }
            catch (Exception ex)
            {
                Log.Error(
                    "[Li AI Chat] Archive reflection failed: "
                    + ex);
            }
        }

        private static void ApplyReflection(
            string runtimeGameId,
            int pawnId,
            string contentDefName,
            string reflection)
        {
            if (Current.Game == null)
            {
                return;
            }

            LiAIChatGameComponent component =
                Current.Game
                    .GetComponent<LiAIChatGameComponent>();

            if (component == null ||
                component.RuntimeGameId.ToString() != runtimeGameId)
            {
                return;
            }

            Pawn pawn =
                FindPawn(pawnId);

            if (pawn == null)
            {
                return;
            }

            PawnAIState state =
                PawnAIStateManager
                    .GetState(pawn);

            if (state == null)
            {
                return;
            }

            state.ArchiveReflections.Add(
                new ArchiveReflection
                {
                    ContentDefName =
                        contentDefName,

                    Text =
                        reflection,

                    CreatedTick =
                        Find.TickManager != null
                            ? Find.TickManager.TicksGame
                            : -1
                });

            Log.Message(
                "[Li AI Chat] Archive reflection created for "
                + pawn.LabelShort
                + ": "
                + reflection);
        }

        private static Pawn FindPawn(
            int pawnId)
        {
            foreach (Map map in Find.Maps)
            {
                foreach (Pawn pawn in
                    map.mapPawns.AllPawns)
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
    }
}