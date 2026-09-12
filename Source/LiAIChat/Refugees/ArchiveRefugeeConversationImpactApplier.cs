using LiAIChat.Models;
using LiAIChat.State;
using UnityEngine;

namespace LiAIChat.Refugees
{
    public static class ArchiveRefugeeConversationImpactApplier
    {
        public static void Apply(
            PawnAIState pawnState,
            ArchiveRefugeeConversationImpact impact,
            ref bool meaningfulRecordedThisSession)
        {
            if (pawnState == null ||
                impact == null)
            {
                return;
            }

            if (!pawnState.IsRefugeeGroupLeader)
            {
                return;
            }

            ArchiveRefugeeGroupState group =
                ArchiveRefugeeGroupManager
                    .GetGroup(
                        pawnState.RefugeeGroupId);

            if (group == null ||
                !group.Active)
            {
                return;
            }


            // ---------------------------------------------
            // Hospitality always reacts to each exchange.
            // ---------------------------------------------

            group.HospitalityImpression =
                Mathf.Clamp(
                    group.HospitalityImpression +
                    impact.HospitalityDelta,
                    -1f,
                    1f);


            // ---------------------------------------------
            // Meaningful count only once per Talk session.
            // ---------------------------------------------

            if (impact.MeaningfulConversation &&
                !meaningfulRecordedThisSession)
            {
                group.MeaningfulConversationCount++;

                meaningfulRecordedThisSession =
                    true;
            }
        }
    }
}