using RimWorld;
using Verse;

namespace LiAIChat.Commentary
{
    public static class CommentaryResearch
    {
        private const string ResearchDefName = "LiAIChat_EarthTextCommentary";

        public static bool IsFinished
        {
            get
            {
                ResearchProjectDef project = DefDatabase<ResearchProjectDef>.GetNamedSilentFail(ResearchDefName);
                return project != null && project.IsFinished;
            }
        }

        public static bool MarginaliaIsFinished
        {
            get
            {
                ResearchProjectDef project = DefDatabase<ResearchProjectDef>.GetNamedSilentFail("LiAIChat_MysteriousMarginalia");
                return project != null && project.IsFinished;
            }
        }
    }
}
