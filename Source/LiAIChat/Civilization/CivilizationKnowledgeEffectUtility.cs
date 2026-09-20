using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Verse;

namespace LiAIChat.Civilization
{
    public static class CivilizationKnowledgeEffectUtility
    {
        public static float GetArchiveIdentificationSpeedFactor()
        {
            return GetCombinedFactor(
                delegate(CivilizationKnowledgeEffects effects)
                {
                    return effects.archiveIdentificationSpeedFactor;
                });
        }

        public static float GetArchiveStudySpeedFactor()
        {
            return GetCombinedFactor(
                delegate(CivilizationKnowledgeEffects effects)
                {
                    return effects.archiveStudySpeedFactor;
                });
        }

        public static float GetArchiveStudyXpFactor()
        {
            return GetCombinedFactor(
                delegate(CivilizationKnowledgeEffects effects)
                {
                    return effects.archiveStudyXpFactor;
                });
        }

        public static float GetScholarTranslationSpeedFactor()
        {
            return GetCombinedFactor(
                delegate(CivilizationKnowledgeEffects effects)
                {
                    return effects.scholarTranslationSpeedFactor;
                });
        }

        public static string BuildConversationContext()
        {
            List<CivilizationKnowledgeDef> defs =
                DefDatabase<CivilizationKnowledgeDef>
                    .AllDefsListForReading;

            if (defs == null)
            {
                return string.Empty;
            }

            StringBuilder builder =
                new StringBuilder();

            foreach (CivilizationKnowledgeDef def
                in defs)
            {
                if (!IsEffectActive(def) ||
                    def.effects == null ||
                    string.IsNullOrWhiteSpace(
                        def.effects
                            .colonyConversationContext))
                {
                    continue;
                }

                if (builder.Length == 0)
                {
                    builder.AppendLine(
                        "COLONY CIVILIZATION KNOWLEDGE:");
                }

                builder.Append("- ");
                builder.Append(
                    GetDisplayTitle(def));
                builder.Append(": ");
                builder.AppendLine(
                    def.effects
                        .colonyConversationContext
                        .Trim());
            }

            if (builder.Length == 0)
            {
                return string.Empty;
            }

            builder.AppendLine(
                "This is knowledge preserved by the colony as a shared cultural resource. It may shape ordinary conversation when relevant, but detailed personal expertise still depends on the pawn's own knowledge and experience.");

            return builder.ToString();
        }

        public static List<string> GetEffectDescriptions(
            CivilizationKnowledgeDef def)
        {
            List<string> descriptions =
                new List<string>();

            if (def == null ||
                def.effects == null)
            {
                return descriptions;
            }

            CivilizationKnowledgeEffects effects =
                def.effects;

            if (!string.IsNullOrWhiteSpace(
                    effects.colonyConversationContext))
            {
                descriptions.Add(
                    "Colony conversations can draw on this reconstructed knowledge.");
            }

            AddFactorDescription(
                descriptions,
                "Archive identification speed",
                effects.archiveIdentificationSpeedFactor);

            AddFactorDescription(
                descriptions,
                "Archive study speed",
                effects.archiveStudySpeedFactor);

            AddFactorDescription(
                descriptions,
                "Archive study Intellectual XP",
                effects.archiveStudyXpFactor);

            AddFactorDescription(
                descriptions,
                "Scholar translation speed",
                effects.scholarTranslationSpeedFactor);

            return descriptions;
        }

        private static float GetCombinedFactor(
            System.Func<CivilizationKnowledgeEffects, float>
                selector)
        {
            List<CivilizationKnowledgeDef> defs =
                DefDatabase<CivilizationKnowledgeDef>
                    .AllDefsListForReading;

            if (defs == null)
            {
                return 1f;
            }

            float factor =
                1f;

            foreach (CivilizationKnowledgeDef def
                in defs)
            {
                if (!IsEffectActive(def) ||
                    def.effects == null)
                {
                    continue;
                }

                float value =
                    selector(def.effects);

                if (value > 0f)
                {
                    factor *= value;
                }
            }

            return Mathf.Clamp(
                factor,
                0.25f,
                3f);
        }

        private static bool IsEffectActive(
            CivilizationKnowledgeDef def)
        {
            return def != null &&
                CivilizationKnowledgeManager
                    .IsActive(def);
        }

        private static void AddFactorDescription(
            List<string> descriptions,
            string label,
            float factor)
        {
            if (Mathf.Approximately(
                    factor,
                    1f))
            {
                return;
            }

            float percent =
                (factor - 1f) * 100f;

            descriptions.Add(
                label +
                " " +
                (percent >= 0f ? "+" : string.Empty) +
                percent.ToString("0") +
                "%.");
        }

        private static string GetDisplayTitle(
            CivilizationKnowledgeDef def)
        {
            if (!string.IsNullOrWhiteSpace(
                    def.title))
            {
                return def.title;
            }

            if (!string.IsNullOrWhiteSpace(
                    def.label))
            {
                return def.label;
            }

            return def.defName;
        }
    }
}
