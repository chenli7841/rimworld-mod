using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace LiAIChat.Archive
{
    public static class ColonyLibraryIndex
    {
        public static List<ColonyLibraryEntry> Build()
        {
            Dictionary<string, ColonyLibraryEntry> entriesByTextDefName =
                new Dictionary<string, ColonyLibraryEntry>();
            HashSet<int> indexedArchiveIds = new HashSet<int>();
            List<Map> maps = Find.Maps;

            if (maps == null)
            {
                return new List<ColonyLibraryEntry>();
            }

            foreach (Map map in maps)
            {
                if (map == null || !map.IsPlayerHome)
                {
                    continue;
                }

                List<Thing> looseArchives = map.listerThings.ThingsOfDef(
                    ThingDefOfArchive.LiAIChat_AncientEarthArchiveFragment);

                if (looseArchives != null)
                {
                    foreach (Thing thing in looseArchives)
                    {
                        AddArchive(thing as Thing_AncientEarthArchiveFragment,
                            entriesByTextDefName, indexedArchiveIds);
                    }
                }

                // RimWorld bookcases keep books inside a ThingOwner, which is
                // deliberately absent from map.listerThings. A library scan
                // must include that container or shelving a book removes it
                // from every topic's prerequisite check.
                foreach (Building_Bookcase bookcase in
                    map.listerThings.AllThings.OfType<Building_Bookcase>())
                {
                    ThingOwner contents = bookcase.GetDirectlyHeldThings();
                    if (contents == null)
                    {
                        continue;
                    }

                    foreach (Thing thing in contents)
                    {
                        AddArchive(thing as Thing_AncientEarthArchiveFragment,
                            entriesByTextDefName, indexedArchiveIds);
                    }
                }
            }

            return entriesByTextDefName.Values
                .OrderBy(entry => entry.Text.author)
                .ThenBy(entry => entry.Text.title)
                .ToList();
        }

        private static void AddArchive(
            Thing_AncientEarthArchiveFragment archive,
            Dictionary<string, ColonyLibraryEntry> entriesByTextDefName,
            HashSet<int> indexedArchiveIds)
        {
            if (archive == null || !indexedArchiveIds.Add(archive.thingIDNumber)
                || !archive.Identified)
            {
                return;
            }

            EarthTextDef text = archive.EarthText;
            if (text == null)
            {
                return;
            }

            ColonyLibraryEntry entry;
            if (!entriesByTextDefName.TryGetValue(text.defName, out entry))
            {
                entry = new ColonyLibraryEntry { Text = text };
                entriesByTextDefName.Add(text.defName, entry);
            }

            entry.Archives.Add(archive);
        }
    }
}
