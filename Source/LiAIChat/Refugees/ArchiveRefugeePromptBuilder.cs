using LiAIChat.Models;
using LiAIChat.State;
using System.Text;

namespace LiAIChat.Refugees
{
    public static class ArchiveRefugeePromptBuilder
    {
        public static void Append(
            StringBuilder sb,
            PawnAIState state)
        {
            if (sb == null ||
                state == null)
            {
                return;
            }

            if (!state.IsRefugeeGroupLeader)
            {
                return;
            }

            ArchiveRefugeeGroupState group =
                ArchiveRefugeeGroupManager
                    .GetGroup(
                        state.RefugeeGroupId);

            if (group == null)
            {
                return;
            }

            sb.AppendLine();
            sb.AppendLine(
                "REFUGEE GROUP CONTEXT:");

            sb.AppendLine(
                "You are the representative of a temporary refugee group.");

            sb.AppendLine(
                "Group size: " +
                group.MemberPawnIds.Count);

            sb.AppendLine(
                "Group background: " +
                group.SituationType);

            sb.AppendLine(
                "Travel hardship: " +
                group.TravelHardship);

            sb.AppendLine(
                "Resource condition: " +
                group.ResourceCondition);

            sb.AppendLine(
                "Age range of the group: " +
                group.YoungestAge +
                "-" +
                group.OldestAge);

            sb.AppendLine(
                "Your group is carrying and protecting an Ancient Earth Archive.");

            sb.AppendLine(
                "You may speak for the group, but you are a representative, not an absolute ruler.");

            sb.AppendLine(
                "Do not invent major facts about the group's history that are not provided.");
        }
    }
}