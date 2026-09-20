using Verse;

namespace LiAIChat.Archive
{
    public static class AncientArchiveFactory
    {
        public static Thing_AncientEarthArchiveFragment Create(
    ArchiveSourceType sourceType,
    string sourceDescription)
        {
            return Create(sourceType, sourceDescription, null);
        }

        public static Thing_AncientEarthArchiveFragment Create(
            ArchiveSourceType sourceType, string sourceDescription, EarthTextDef fixedText)
        {
            Thing_AncientEarthArchiveFragment archive =
                ThingMaker.MakeThing(
                    ThingDefOfArchive
                        .LiAIChat_AncientEarthArchiveFragment)
                as Thing_AncientEarthArchiveFragment;

            if (archive == null)
            {
                return null;
            }
            archive.SetProvenance(sourceType, sourceDescription);

            ArchiveContentDef content = ArchiveContentSelector.SelectForSource(sourceType);
            archive.SetContent(content);

            EarthTextDef earthText = fixedText ?? EarthTextSelector.SelectRandom();
            archive.SetEarthText(earthText);


            return archive;
        }
    }
}
