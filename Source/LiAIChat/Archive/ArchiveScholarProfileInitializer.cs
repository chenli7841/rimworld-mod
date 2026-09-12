using LiAIChat.Models;
using LiAIChat.State;
using Verse;

namespace LiAIChat.Archive
{
    public static class ArchiveScholarProfileInitializer
    {
        public static void Initialize(
            Pawn pawn,
            Thing_AncientEarthArchiveFragment archive)
        {
            if (pawn == null ||
                archive == null ||
                archive.Content == null)
            {
                return;
            }

            PawnAIState state =
                PawnAIStateManager.GetState(
                    pawn);

            if (state == null)
            {
                return;
            }

            ArchiveContentDef content =
                archive.Content;

            ArchiveScholarProfile profile =
                new ArchiveScholarProfile();

            profile.Specialty =
                GetSpecialtyName(
                    content.theme);

            profile.PrimaryTopicId =
                content.topicId;

            profile.YearsOfStudy =
                Rand.RangeInclusive(
                    4,
                    18);

            profile.PreservationMotivation =
                CreatePreservationMotivation(
                    content);

            state.ScholarProfile =
                profile;

            Log.Message(
                "[Li AI Chat] Scholar profile initialized for " +
                pawn.LabelShort +
                ". Specialty=" +
                profile.Specialty +
                ", topic=" +
                profile.PrimaryTopicId +
                ", years=" +
                profile.YearsOfStudy);
        }
        private static string GetSpecialtyName(
    ArchiveTheme theme)
        {
            switch (theme)
            {
                case ArchiveTheme.History:
                    return "Ancient Earth History";

                case ArchiveTheme.Philosophy:
                    return "Ancient Earth Philosophy";

                case ArchiveTheme.Religion:
                    return "Ancient Earth Religion";

                case ArchiveTheme.Politics:
                    return "Ancient Earth Political Thought";

                case ArchiveTheme.Science:
                    return "Ancient Earth Science";

                default:
                    return "Ancient Earth Studies";
            }
        }
        private static string CreatePreservationMotivation(
    ArchiveContentDef content)
        {
            int choice =
                Rand.Range(
                    0,
                    4);

            switch (choice)
            {
                case 0:
                    return
                        "Believes that the knowledge of Ancient Earth " +
                        "should not disappear from human memory.";

                case 1:
                    return
                        "Has spent years preserving fragments of " +
                        "Ancient Earth thought for future generations.";

                case 2:
                    return
                        "Regards surviving Earth archives as part of " +
                        "humanity's shared inheritance.";

                default:
                    return
                        "Wants later generations to understand how " +
                        "people on Ancient Earth understood the world.";
            }
        }
    }
}