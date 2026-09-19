using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace LiAIChat.Archive
{
    public static class
        AncientArchiveIdentificationUtility
    {
        public static void TryIdentifyArchive(
            Pawn pawn)
        {
            if (pawn == null ||
                pawn.Map == null)
            {
                return;
            }

            List<Thing_AncientEarthArchiveFragment>
                archives =
                    pawn.Map.listerThings
                        .ThingsOfDef(
                            ThingDefOfArchive
                                .LiAIChat_AncientEarthArchiveFragment)
                        .OfType<
                            Thing_AncientEarthArchiveFragment>()
                        .Where(
                            archive =>
                                !archive.Identified &&
                                archive.Content != null &&
                                pawn.CanReach(
                                    archive,
                                    PathEndMode.Touch,
                                    Danger.Some))
                        .OrderBy(
                            archive =>
                                pawn.Position
                                    .DistanceToSquared(
                                        archive.Position))
                        .ToList();

            if (archives.Count == 0)
            {
                Messages.Message(
                    "No unidentified ancient Earth archives are reachable.",
                    MessageTypeDefOf.RejectInput);

                return;
            } else
            {
                Messages.Message(
                    "Found " + archives.Count + " ancient Earth archives are reachable.",
                    MessageTypeDefOf.PositiveEvent);
            }

                ShowSelectionMenu(
                    pawn,
                    archives);
        }

        private static void ShowSelectionMenu(
            Pawn pawn,
            List<
                Thing_AncientEarthArchiveFragment>
                archives)
        {
            List<FloatMenuOption> options =
                new List<FloatMenuOption>();

            foreach (
                Thing_AncientEarthArchiveFragment
                    archive in archives)
            {
                Thing_AncientEarthArchiveFragment
                    capturedArchive = archive;

                int distance =
                    (int)pawn.Position.DistanceTo(
                        archive.Position);
                string label =
                    "Unknown Ancient Earth Archive" +
                    " (" +
                    distance +
                    " tiles away)";

                options.Add(
                    new FloatMenuOption(
                        label,
                        () =>
                            StartIdentificationJob(
                                pawn,
                                capturedArchive)));
            }

            Find.WindowStack.Add(
                new FloatMenu(options));
        }

        private static void StartIdentificationJob(
            Pawn pawn,
            Thing_AncientEarthArchiveFragment archive)
        {
            if (pawn == null ||
                archive == null ||
                archive.Destroyed ||
                !archive.Spawned ||
                archive.Identified)
            {
                return;
            }

            Job job =
                JobMaker.MakeJob(
                    LiAIChatJobDefOf
                        .LiAIChat_IdentifyAncientArchive,
                    archive);

            pawn.jobs.TryTakeOrderedJob(
                job,
                JobTag.Misc);
        }
    }
}