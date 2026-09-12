using System.Collections.Generic;
using System.Linq;
using LiAIChat.Refugees;
using RimWorld.QuestGen;
using Verse;

namespace LiAIChat.Questing
{
    public class QuestNode_FinalizeArchiveRefugeeStay
        : QuestNode
    {
        public SlateRef<string> groupId;

        public SlateRef<IEnumerable<Pawn>> pawns;


        protected override bool TestRunInt(
            Slate slate)
        {
            string id =
                groupId.GetValue(slate);

            return
                !string.IsNullOrEmpty(id);
        }


        protected override void RunInt()
        {
            Slate slate =
                QuestGen.slate;

            string id =
                groupId.GetValue(slate);

            ArchiveRefugeeGroupState group =
                ArchiveRefugeeGroupManager
                    .GetGroup(id);

            if (group == null)
            {
                Log.Warning(
                    "[Li AI Chat] Finalize Refugee Stay: " +
                    "group not found.");

                return;
            }


            IEnumerable<Pawn> pawnEnumerable =
                pawns.GetValue(slate);

            List<Pawn> pawnList =
                pawnEnumerable == null
                    ? new List<Pawn>()
                    : pawnEnumerable.ToList();


            bool leaveArchive =
                ArchiveRefugeeArchiveDecisionUtility
                    .Decide(group);


            if (leaveArchive)
            {
                bool transferred =
                    ArchiveRefugeeArchiveTransferUtility
                        .TryLeaveArchive(
                            group,
                            pawnList);

                if (!transferred)
                {
                    Log.Warning(
                        "[Li AI Chat] Refugee group intended to leave Archive, " +
                        "but the physical transfer could not be completed.");
                }
            }


            group.DepartureStarted =
                true;


            Log.Message(
                "[Li AI Chat] Refugee stay finalized: " +
                "group=" +
                group.GroupId +
                ", leaveArchive=" +
                group.WillLeaveArchive);
        }
    }
}