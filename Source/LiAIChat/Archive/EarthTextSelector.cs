using System.Collections.Generic;
using Verse;

namespace LiAIChat.Archive
{
    public static class EarthTextSelector
    {
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