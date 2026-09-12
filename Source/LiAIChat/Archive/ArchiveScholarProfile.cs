using Verse;

namespace LiAIChat.Archive
{
    public class ArchiveScholarProfile : IExposable
    {
        public string Specialty;
        public string PrimaryTopicId;

        public int YearsOfStudy;

        public string PreservationMotivation;

        public void ExposeData()
        {
            Scribe_Values.Look(
                ref Specialty,
                "specialty");

            Scribe_Values.Look(
                ref PrimaryTopicId,
                "primaryTopicId");

            Scribe_Values.Look(
                ref YearsOfStudy,
                "yearsOfStudy",
                0);

            Scribe_Values.Look(
                ref PreservationMotivation,
                "preservationMotivation");
        }
    }
}