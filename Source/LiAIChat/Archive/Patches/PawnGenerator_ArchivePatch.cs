using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace LiAIChat.Archive.Patches
{
    [HarmonyPatch(
        typeof(PawnGenerator),
        nameof(PawnGenerator.GeneratePawn),
        new[] { typeof(PawnGenerationRequest) })]
    public static class PawnGenerator_ArchivePatch
    {
        [HarmonyPostfix]
        public static void Postfix(
            PawnGenerationRequest request,
            Pawn __result)
        {
            Pawn pawn = __result;

            if (pawn == null)
            {
                return;
            }

            if (pawn.kindDef !=
                PawnKindDefOf.AncientSoldier)
            {
                return;
            }

            TryGiveArchive(pawn);
        }

        private static void TryGiveArchive(
    Pawn pawn)
        {
            if (pawn.inventory == null)
            {
                return;
            }

            if (AlreadyHasArchive(pawn))
            {
                return;
            }

            float chance =
                ArchiveLootRules
                    .GetArchiveChance(pawn);

            if (chance <= 0f)
            {
                return;
            }

            Log.Message(
    "[Li AI Chat] Ancient soldier " +
    pawn.LabelShort +
    " archive chance: " +
    chance.ToString("P0"));

            if (!Rand.Chance(chance))
            {
                return;
            }

            Thing_AncientEarthArchiveFragment archive =
                AncientArchiveFactory.Create(
                    ArchiveSourceType.AncientPawn,
                    BuildSourceDescription(pawn));

            if (archive == null)
            {
                return;
            }

            bool added =
                pawn.inventory
                    .innerContainer
                    .TryAdd(archive);

            if (!added)
            {
                archive.Destroy();

                Log.Warning(
                    "[Li AI Chat] Could not add archive to " +
                    pawn.LabelShort +
                    "'s inventory.");

                return;
            }

            Log.Message(
                "[Li AI Chat] Ancient pawn " +
                pawn.LabelShort +
                " generated with archive. Chance=" +
                chance.ToString("P0") +
                ", Content=" +
                archive.Content?.defName);
        }

        private static string BuildSourceDescription(
    Pawn pawn)
        {
            if (pawn == null)
            {
                return "Recovered from an ancient soldier.";
            }

            return
                "Recovered from the ancient soldier " +
                pawn.LabelShort +
                ".";
        }

        private static bool AlreadyHasArchive(
            Pawn pawn)
        {
            return pawn.inventory
                .innerContainer
                .Any(
                    thing =>
                        thing.def ==
                        ThingDefOfArchive
                            .LiAIChat_AncientEarthArchiveFragment);
        }
    }
}