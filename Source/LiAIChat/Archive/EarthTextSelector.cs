using System.Collections.Generic;
using Verse;

namespace LiAIChat.Archive
{
    public static class EarthTextSelector
    {
        public static EarthTextDef SelectMissingOrRandom()
        {
            List<EarthTextDef> missing = ColonyLibrary.GetMissingTexts(
                DefDatabase<EarthTextDef>.AllDefsListForReading);
            return missing.Count > 0
                ? missing[Rand.Range(0, missing.Count)]
                : SelectRandom();
        }

        public static EarthTextDef SelectRandom()
        {
            List<EarthTextDef> texts =
                DefDatabase<EarthTextDef>.AllDefsListForReading;

            if (texts == null || texts.Count == 0)
            {
                Log.Warning(
                    "[LiAIChat] Cannot select Earth text: "
                    + "no EarthTextDef loaded.");

                return null;
            }

            int index = Rand.Range(0, texts.Count);

            return texts[index];
        }
    }
}
