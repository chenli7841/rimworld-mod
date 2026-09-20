using System.Collections.Generic;

using LiAIChat.Archive;

using Verse;

namespace LiAIChat.Civilization
{
    public class CivilizationKnowledgeDef : Def
    {
        public string title;

        public string titleChinese;

        public string descriptionChinese;

        public CivilizationKnowledgeCategory category;

        public List<EarthTextDef> requiredTexts =
            new List<EarthTextDef>();

        public List<CivilizationKnowledgeDef> prerequisites =
            new List<CivilizationKnowledgeDef>();

        public int gracePeriodDays = 10;

        public int dormantAfterDays = 30;

        public CivilizationKnowledgeEffects effects =
            new CivilizationKnowledgeEffects();

        public float treeX;

        public float treeY;
    }

    public class CivilizationKnowledgeEffects
    {
        public float archiveIdentificationSpeedFactor =
            1f;

        public float archiveStudySpeedFactor =
            1f;

        public float archiveStudyXpFactor =
            1f;

        public float scholarTranslationSpeedFactor =
            1f;

        public string colonyConversationContext;
    }
}
