using RimWorld;
using Verse;

namespace LiAIChat.Dialogue
{
    public static class ConversationMoodBoost
    {
        private const string ThoughtDefName = "LiAIChat_ConversationAfterglow";

        // A completed player-to-pawn exchange leaves a small, visible positive
        // memory. ThoughtDef stacking handles the six-layer cap and renews the
        // one-day duration whenever the conversation continues.
        public static void Grant(Pawn pawn)
        {
            if (pawn == null || !pawn.IsColonistPlayerControlled ||
                pawn.needs?.mood?.thoughts?.memories == null)
            {
                return;
            }

            ThoughtDef thought = DefDatabase<ThoughtDef>.GetNamedSilentFail(ThoughtDefName);
            if (thought != null)
            {
                pawn.needs.mood.thoughts.memories.TryGainMemory(thought);
            }
        }
    }
}
