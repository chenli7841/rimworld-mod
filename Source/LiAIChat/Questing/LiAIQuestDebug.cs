using Verse;

namespace LiAIChat.Questing
{
    public static class LiAIQuestDebug
    {
        public static bool Enabled = true;

        public static void LogTest(
            string nodeName,
            bool result,
            string details = null)
        {
            if (!Enabled)
                return;

            if (!Prefs.DevMode)
                return;

            string message =
                "[Li AI Chat][Quest TestRun] " +
                nodeName +
                " => " +
                (result ? "PASS" : "FAIL");

            if (!string.IsNullOrEmpty(details))
            {
                message += " | " + details;
            }

            Log.Message(message);
        }
    }
}