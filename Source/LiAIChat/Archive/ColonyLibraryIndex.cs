using System.Collections.Generic;
using System.Linq;
using Verse;

namespace LiAIChat.Archive
{
    public static class ColonyLibraryIndex
    {
        public static List<ColonyLibraryEntry>
            Build()
        {
            Dictionary<string, ColonyLibraryEntry>
                entriesByTextDefName =
                    new Dictionary<
                        string,
                        ColonyLibraryEntry>();

            List<Map> maps =
                Find.Maps;

            if (maps == null)
            {
                return new List<
                    ColonyLibraryEntry>();
            }

            foreach (Map map in maps)
            {
                if (map == null ||
                    !map.IsPlayerHome)
                {
                    continue;
                }

                List<Thing> things =
                    map.listerThings
                        .ThingsOfDef(
                            ThingDefOfArchive
                                .LiAIChat_AncientEarthArchiveFragment);

                if (things == null)
                {
                    continue;
                }

                foreach (Thing thing in things)
                {
                    Thing_AncientEarthArchiveFragment
                        archive =
                            thing as
                                Thing_AncientEarthArchiveFragment;

                    if (archive == null)
                    {
                        continue;
                    }

                    if (!archive.Identified)
                    {
                        continue;
                    }

                    EarthTextDef text =
                        archive.EarthText;

                    if (text == null)
                    {
                        continue;
                    }

                    ColonyLibraryEntry entry;

                    if (!entriesByTextDefName
                        .TryGetValue(
                            text.defName,
                            out entry))
                    {
                        entry =
                            new ColonyLibraryEntry
                            {
                                Text = text
                            };

                        entriesByTextDefName.Add(
                            text.defName,
                            entry);
                    }

                    entry.Archives.Add(
                        archive);
                }
            }

            return entriesByTextDefName
                .Values
                .OrderBy(
                    entry =>
                        entry.Text.author)
                .ThenBy(
                    entry =>
                        entry.Text.title)
                .ToList();
        }
    }
}