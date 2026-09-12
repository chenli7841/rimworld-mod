using LiAIChat.Models;
using System.Text;

namespace LiAIChat.Archive
{
    public static class ArchiveScholarPromptBuilder
    {
        public static void Append(
            StringBuilder sb,
            PawnContext pawnContext,
            PawnAIState state)
        {
            if (sb == null ||
                pawnContext == null ||
                state == null)
            {
                return;
            }

            if (state.ScholarProfile == null)
            {
                return;
            }

            AppendScholarIdentity(
                sb,
                state.ScholarProfile);

            AppendCurrentStay(
                sb,
                state.ScholarStay,
                pawnContext.CurrentGameTick);
        }


        private static void AppendScholarIdentity(
            StringBuilder sb,
            ArchiveScholarProfile profile)
        {
            sb.AppendLine();
            sb.AppendLine(
                "ARCHIVE SCHOLAR BACKGROUND:");

            if (!string.IsNullOrEmpty(
                profile.Specialty))
            {
                sb.AppendLine(
                    "- Specialty: " +
                    profile.Specialty);
            }

            if (!string.IsNullOrEmpty(
                profile.PrimaryTopicId))
            {
                sb.AppendLine(
                    "- Primary field of study: " +
                    profile.PrimaryTopicId);
            }

            if (profile.YearsOfStudy > 0)
            {
                sb.AppendLine(
                    "- Years devoted to this research: " +
                    profile.YearsOfStudy);
            }

            if (!string.IsNullOrEmpty(
                profile.PreservationMotivation))
            {
                sb.AppendLine(
                    "- Reason for preserving ancient knowledge: " +
                    profile.PreservationMotivation);
            }
        }


        private static void AppendCurrentStay(
            StringBuilder sb,
            ArchiveScholarStayState stay,
            int currentGameTick)
        {
            if (stay == null)
            {
                return;
            }

            sb.AppendLine();
            sb.AppendLine(
                "CURRENT SITUATION:");

            if (stay.Active)
            {
                sb.AppendLine(
                    "- You are currently a temporary guest of this colony.");

                sb.AppendLine(
                    "- The colonists are giving you shelter and safety for a limited time.");

                int ticksRemaining =
                    stay.EndTick - currentGameTick;

                if (ticksRemaining < 0)
                {
                    ticksRemaining = 0;
                }

                float daysRemaining =
                    ticksRemaining / 60000f;

                sb.AppendLine(
                    "- Approximate time remaining in your stay: " +
                    daysRemaining.ToString("0.0") +
                    " days.");

                sb.AppendLine(
                    "- You still possess an Ancient Earth Archive that you have protected during your travels.");

                sb.AppendLine(
                    "- If the colony continues to shelter you until the agreed stay is complete, you currently intend to leave that archive with them before departing.");

                sb.AppendLine(
                    "- Do not speak as if you have already given them the archive.");

                return;
            }

            if (stay.Completed)
            {
                sb.AppendLine(
                    "- The colony successfully gave you temporary shelter.");

                sb.AppendLine(
                    "- Your agreed period of stay has now been completed.");

                sb.AppendLine(
                    "- You intend to leave rather than permanently join the colony.");

                return;
            }

            sb.AppendLine(
                "- Your temporary stay with the colony did not reach a normal completion.");
        }
    }
}