using System.Collections.Generic;
using System.Linq;
using LiAIChat.Archive;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace LiAIChat.Questing
{
    public class QuestPart_WaitForArchiveRecovery : QuestPartActivable
    {
        public Site site;
        private List<Thing_AncientEarthArchiveFragment> archives =
            new List<Thing_AncientEarthArchiveFragment>();
        private bool archivesGenerated;

        private static bool IsAvailable(Thing_AncientEarthArchiveFragment archive)
        {
            return archive != null && !archive.Destroyed && !archive.SoldByPlayer;
        }

        private static bool IsRecovered(Thing_AncientEarthArchiveFragment archive)
        {
            if (!IsAvailable(archive) || archive.MapHeld == null ||
                !archive.MapHeld.IsPlayerHome)
                return false;

            // Loose books on a home map count; carried books require player custody.
            if (archive.Spawned)
                return true;

            bool playerCarrier = false;
            for (IThingHolder holder = archive.ParentHolder; holder != null;
                holder = holder.ParentHolder)
            {
                Pawn pawn = holder as Pawn;
                if (pawn == null)
                    continue;
                if (pawn.Dead || pawn.Faction != Faction.OfPlayer)
                    return false;
                playerCarrier = true;
            }
            return playerCarrier;
        }

        // Display is derived from the saved references, never saved separately.
        public override string DescriptionPart
        {
            get
            {
                // Finished quests already display their outcome in the vanilla UI.
                if (quest == null || quest.State != QuestState.Ongoing)
                    return null;

                if (!archivesGenerated)
                    return "LiAIChat_RecoveryAwaitingArchives".Translate();

                int surviving = archives.Count(IsAvailable);
                int atHome = archives.Count(IsRecovered);

                if (atHome > 0)
                    return "LiAIChat_RecoveryArrived".Translate();
                if (surviving == 0)
                    return "LiAIChat_RecoveryLost".Translate();

                // Being outside the site does not prove player possession:
                // a trader or another map can also hold a surviving archive.
                return "LiAIChat_RecoveryRemaining".Translate(surviving);
            }
        }

        public static void Register(Site source, Thing_AncientEarthArchiveFragment archive)
        {
            if (source == null || Find.QuestManager == null)
                return;

            foreach (Quest quest in Find.QuestManager.QuestsListForReading)
            foreach (QuestPart_WaitForArchiveRecovery part in
                quest.PartsListForReading.OfType<QuestPart_WaitForArchiveRecovery>())
            {
                if (part.site != source)
                    continue;
                part.archivesGenerated = true;
                if (!part.archives.Contains(archive))
                    part.archives.Add(archive);
            }
        }

        public override void QuestPartTick()
        {
            base.QuestPartTick();
            if (quest.State != QuestState.Ongoing || Find.TickManager.TicksGame % 250 != 0)
                return;

            if (archives.Any(IsRecovered))
            {
                quest.End(QuestEndOutcome.Success, true);
                return;
            }

            // A caravan has no MapHeld: surviving archives remain valid in transit.
            bool anySurviving = archives.Any(IsAvailable);
            if (!anySurviving && (archivesGenerated || site == null || site.Destroyed))
                quest.End(QuestEndOutcome.Fail, true);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref site, "site");
            Scribe_Collections.Look(ref archives, "archives", LookMode.Reference);
            Scribe_Values.Look(ref archivesGenerated, "archivesGenerated");
            if (Scribe.mode == LoadSaveMode.PostLoadInit && archives == null)
                archives = new List<Thing_AncientEarthArchiveFragment>();
        }
    }
}
