using System.Collections.Generic;
using System.Linq;

namespace LiAIChat.Archive
{
    public static class ColonyLibrary
    {
        public static bool HasText(
            EarthTextDef text)
        {
            if (text == null)
            {
                return false;
            }

            return HasText(text.defName);
        }

        public static bool HasText(
            string earthTextDefName)
        {
            if (string.IsNullOrWhiteSpace(
                earthTextDefName))
            {
                return false;
            }

            List<ColonyLibraryEntry> entries =
                ColonyLibraryIndex.Build();

            return entries.Any(
                entry =>
                    entry.Text != null &&
                    entry.Text.defName ==
                        earthTextDefName);
        }

        public static int GetCopyCount(
            EarthTextDef text)
        {
            if (text == null)
            {
                return 0;
            }

            return GetCopyCount(
                text.defName);
        }

        public static int GetCopyCount(
            string earthTextDefName)
        {
            if (string.IsNullOrWhiteSpace(
                earthTextDefName))
            {
                return 0;
            }

            List<ColonyLibraryEntry> entries =
                ColonyLibraryIndex.Build();

            ColonyLibraryEntry entry =
                entries.FirstOrDefault(
                    item =>
                        item.Text != null &&
                        item.Text.defName ==
                            earthTextDefName);

            if (entry == null)
            {
                return 0;
            }

            return entry.CopyCount;
        }

        public static List<EarthTextDef>
            GetAvailableTexts()
        {
            return ColonyLibraryIndex
                .Build()
                .Where(
                    entry =>
                        entry.Text != null)
                .Select(
                    entry =>
                        entry.Text)
                .ToList();
        }

        public static List<EarthTextDef>
            GetMissingTexts(
                IEnumerable<EarthTextDef>
                    requiredTexts)
        {
            List<EarthTextDef> missing =
                new List<EarthTextDef>();

            if (requiredTexts == null)
            {
                return missing;
            }

            List<ColonyLibraryEntry> entries =
                ColonyLibraryIndex.Build();

            HashSet<string> available =
                new HashSet<string>(
                    entries
                        .Where(
                            entry =>
                                entry.Text != null)
                        .Select(
                            entry =>
                                entry.Text.defName));

            foreach (EarthTextDef text
                in requiredTexts)
            {
                if (text == null)
                {
                    continue;
                }

                if (!available.Contains(
                    text.defName))
                {
                    missing.Add(text);
                }
            }

            return missing;
        }

        public static bool HasAllTexts(
            IEnumerable<EarthTextDef>
                requiredTexts)
        {
            if (requiredTexts == null)
            {
                return true;
            }

            return GetMissingTexts(
                requiredTexts).Count == 0;
        }
    }
}