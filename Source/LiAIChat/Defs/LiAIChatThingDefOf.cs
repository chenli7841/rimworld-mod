using RimWorld;
using Verse;

namespace LiAIChat
{
    [DefOf]
    public static class LiAIChatThingDefOf
    {
        // =====================================================
        // Ancient Earth Archive Site
        // =====================================================

        public static ThingDef LiAIChat_AncientArchiveSignalTerminal;


        // =====================================================
        // Initialization
        // =====================================================

        static LiAIChatThingDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(
                typeof(LiAIChatThingDefOf));
        }
    }
}