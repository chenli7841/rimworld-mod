using LiAIChat.Archive;
using LiAIChat.Refugees;
using RimWorld.QuestGen;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace LiAIChat.Questing
{
    public class QuestNode_GiveArchiveToRefugeeGroup : QuestNode
    {
        public SlateRef<IEnumerable<Pawn>> pawns;

        public string storeArchiveAs;

        public string storeGroupIdAs;

        public string storeLeaderAs;

        public SlateRef<string> exitSignal;

        public SlateRef<string> failureSignal;

        protected override bool TestRunInt(Slate slate)
        {
            IEnumerable<Pawn> pawnEnumerable =
                pawns.GetValue(slate);

            if (pawnEnumerable == null)
            {
                LiAIQuestDebug.LogTest(
                    "GiveArchiveToRefugeeGroup",
                    false,
                    "pawns=null");

                return false;
            }

            List<Pawn> refugeeList =
                pawnEnumerable
                    .Where(p => p != null)
                    .ToList();

            if (refugeeList.Count == 0)
            {
                LiAIQuestDebug.LogTest(
                    "GiveArchiveToRefugeeGroup",
                    false,
                    "valid refugees=0");

                return false;
            }

            if (string.IsNullOrEmpty(storeArchiveAs))
            {
                return false;
            }

            // ---------------------------------------------------------
            // Dry-run Slate values
            //
            // Do NOT create a real ArchiveRefugeeGroupState here.
            // We only provide values required by later TestRun nodes.
            // ---------------------------------------------------------

            if (!string.IsNullOrEmpty(storeGroupIdAs))
            {
                slate.Set(
                    storeGroupIdAs,
                    "LiAIChat_TestRefugeeGroup");
            }

            if (!string.IsNullOrEmpty(storeLeaderAs))
            {
                slate.Set(
                    storeLeaderAs,
                    refugeeList[0]);
            }
            LiAIQuestDebug.LogTest(
                "GiveArchiveToRefugeeGroup",
                true,
                "refugees=" +
                refugeeList.Count +
                ", groupIdPrepared=" +
                (!string.IsNullOrEmpty(storeGroupIdAs)));
            return true;
        }

        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;

            IEnumerable<Pawn> pawnEnumerable =
                pawns.GetValue(slate);

            if (pawnEnumerable == null)
            {
                Log.Warning(
                    "[Li AI Chat] GiveArchiveToRefugeeGroup: " +
                    "pawn collection was null.");

                return;
            }

            List<Pawn> refugeeList =
                pawnEnumerable
                    .Where(p => p != null)
                    .ToList();

            if (refugeeList.Count == 0)
            {
                Log.Warning(
                    "[Li AI Chat] GiveArchiveToRefugeeGroup: " +
                    "no valid refugees.");

                return;
            }

            Pawn carrier = refugeeList[0];

            Thing_AncientEarthArchiveFragment archive =
                AncientArchiveFactory.Create(
                    ArchiveSourceType.RefugeeGroup,
                    BuildSourceDescription(refugeeList));

            if (archive == null)
            {
                Log.Error(
                    "[Li AI Chat] GiveArchiveToRefugeeGroup: " +
                    "archive creation failed.");

                return;
            }

            bool added =
                carrier.inventory.innerContainer.TryAdd(archive);

            if (!added)
            {
                Log.Warning(
                    "[Li AI Chat] GiveArchiveToRefugeeGroup: " +
                    "failed to place archive in carrier inventory.");

                archive.Destroy();

                return;
            }

            slate.Set(
                storeArchiveAs,
                archive);

            ArchiveRefugeeGroupState group =
                ArchiveRefugeeGroupManager.CreateGroup(
                    refugeeList,
                    archive,
                    carrier);

            if (group == null)
            {
                Log.Warning(
                    "[Li AI Chat] GiveArchiveToRefugeeGroup: " +
                    "failed to register refugee group.");

                return;
            }

            group.ExitSignal =
    exitSignal.GetValue(slate);

            group.FailureSignal =
                failureSignal.GetValue(slate);

            if (!string.IsNullOrEmpty(storeGroupIdAs))
            {
                slate.Set(
                    storeGroupIdAs,
                    group.GroupId);
            }

            Log.Message(
                "[Li AI Chat] Refugee group archive created. " +
                "Carrier=" +
                carrier.LabelShort +
                ", archive=" +
                archive.Label);

            Pawn leader = null;

            for (int i = 0;
                 i < refugeeList.Count;
                 i++)
            {
                Pawn pawn =
                    refugeeList[i];

                if (pawn != null &&
                    pawn.thingIDNumber ==
                    group.LeaderPawnId)
                {
                    leader = pawn;
                    break;
                }
            }

            if (leader != null)
            {
                ArchiveRefugeeLeaderInitializer
                    .Initialize(leader, group);
            }

            if (leader != null &&
    !string.IsNullOrEmpty(storeLeaderAs))
            {
                slate.Set(
                    storeLeaderAs,
                    leader);
            }
        }

        private static string BuildSourceDescription(
            List<Pawn> refugees)
        {
            return
                "Preserved and carried by a group of " +
                refugees.Count +
                " travelers seeking shelter.";
        }
    }
}