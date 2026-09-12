using RimWorld;
using Verse;

namespace LiAIChat.Archive
{
    [DefOf]
    public static class ThingDefOfArchive
    {
        public static ThingDef
            LiAIChat_AncientEarthArchiveFragment;

        public static ThingDef Fragment =>
            LiAIChat_AncientEarthArchiveFragment;

        static ThingDefOfArchive()
        {
            DefOfHelper.EnsureInitializedInCtor(
                typeof(ThingDefOfArchive));
        }
    }
}