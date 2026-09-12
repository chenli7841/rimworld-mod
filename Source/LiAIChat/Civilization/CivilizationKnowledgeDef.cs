using System.Collections.Generic;
using LiAIChat.Archive;
using Verse;

namespace LiAIChat.Civilization
{
    public class CivilizationKnowledgeDef : Def
    {
        public string title;

        public string titleChinese;

        public CivilizationKnowledgeCategory category;

        public List<EarthTextDef> requiredTexts =
            new List<EarthTextDef>();

        public int gracePeriodDays = 10;

        public int dormantAfterDays = 30;
    }
}