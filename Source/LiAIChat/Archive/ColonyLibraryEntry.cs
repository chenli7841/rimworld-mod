using System.Collections.Generic;

namespace LiAIChat.Archive
{
    public class ColonyLibraryEntry
    {
        public EarthTextDef Text;

        public List<Thing_AncientEarthArchiveFragment>
            Archives =
                new List<
                    Thing_AncientEarthArchiveFragment>();

        public int CopyCount
        {
            get
            {
                return Archives.Count;
            }
        }
    }
}