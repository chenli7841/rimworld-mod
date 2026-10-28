using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace LiAIChat.Blightfall
{
    public enum BlightAfflictionType
    {
        SporeLungInfection,
        CorrosiveTissueDamage,
        NeuralResonanceStrain,
        SymbioticMetabolicStrain,
        PhaseveilNeuralFatigue,
        ToxicAccumulation,
        DermalErosion
    }

    public class HediffCompProperties_BlightAffliction : HediffCompProperties
    {
        public float naturalRecoveryPerDay = 0.01f;
        public float severeThreshold = 0.5f;
        public float severeRecoveryFactor = 0.2f;
        public float treatmentRecovery = 0.12f;

        public HediffCompProperties_BlightAffliction()
        {
            compClass = typeof(HediffComp_BlightAffliction);
        }
    }

    public class HediffComp_BlightAffliction : HediffComp
    {
        private string sourceLabel;
        private string protectionLabel;

        private HediffCompProperties_BlightAffliction Props =>
            (HediffCompProperties_BlightAffliction)props;

        public override bool CompShouldRemove
        {
            get { return parent == null || parent.Severity <= 0.0001f || BlightAfflictionUtility.BodyPartNoLongerSusceptible(parent); }
        }

        public override void CompPostTickInterval(ref float severityAdjustment, int interval)
        {
            if (parent == null || parent.Severity <= 0f) return;
            float recoveryPerDay = Props.naturalRecoveryPerDay;
            if (parent.Severity >= Props.severeThreshold)
                recoveryPerDay *= Props.severeRecoveryFactor;
            if (parent.def?.defName == BlightAfflictionUtility.PhaseveilHediff && parent.pawn?.needs?.rest != null &&
                parent.pawn.needs.rest.CurLevelPercentage > 0.7f)
                recoveryPerDay *= 3f;
            severityAdjustment -= recoveryPerDay * interval / GenDate.TicksPerDay;
        }

        public override void CompTended(float quality, float maxQuality, int batchPosition)
        {
            if (parent == null || parent.Severity <= 0f) return;
            float normalizedQuality = maxQuality > 0f ? Mathf.Clamp01(quality / maxQuality) : Mathf.Clamp01(quality);
            parent.Severity = Mathf.Max(0f, parent.Severity - Props.treatmentRecovery * normalizedQuality);
        }

        public override string CompTipStringExtra
        {
            get
            {
                string text = "LiAIChat_BlightAfflictionSeverity".Translate(parent.Severity.ToStringPercent()).ToString();
                if (!string.IsNullOrEmpty(sourceLabel))
                    text += "\n" + "LiAIChat_BlightAfflictionSource".Translate(sourceLabel);
                if (!string.IsNullOrEmpty(protectionLabel))
                    text += "\n" + "LiAIChat_BlightAfflictionProtection".Translate(protectionLabel);
                text += "\n" + "LiAIChat_BlightAfflictionTend".Translate();
                return text;
            }
        }

        public void RecordExposure(string source, string protection)
        {
            if (!string.IsNullOrWhiteSpace(source)) sourceLabel = source;
            if (!string.IsNullOrWhiteSpace(protection)) protectionLabel = protection;
            else protectionLabel = null;
        }

        public override void CompExposeData()
        {
            Scribe_Values.Look(ref sourceLabel, "blightAfflictionSource");
            Scribe_Values.Look(ref protectionLabel, "blightAfflictionProtection");
        }
    }

    public class BlightWeaponAfflictionExtension : DefModExtension
    {
        public float sporeSeverityPerUse;
        public float corrosiveSeverityPerUse;
        public float neuralSeverityPerUse;
    }

    public class BlightApparelAfflictionExtension : DefModExtension
    {
        public float neuralSeverityPerDay;
        public float symbioticSeverityPerDay;
        public float phaseveilSeverityPerDay;
    }

    public static class BlightAfflictionUtility
    {
        public const string SporeHediff = "LiAIChat_SporeLungInfection";
        public const string CorrosiveHediff = "LiAIChat_CorrosiveTissueDamage";
        public const string NeuralHediff = "LiAIChat_NeuralResonanceStrain";
        public const string SymbioticHediff = "LiAIChat_SymbioticMetabolicStrain";
        public const string PhaseveilHediff = "LiAIChat_PhaseveilNeuralFatigue";
        public const string ToxicAccumulationHediff = "LiAIChat_ToxicAccumulation";
        public const string DermalErosionHediff = "LiAIChat_DermalErosion";
        private static readonly Dictionary<BlightAfflictionType, string> AfflictionDefs =
            new Dictionary<BlightAfflictionType, string>
            {
                { BlightAfflictionType.SporeLungInfection, SporeHediff },
                { BlightAfflictionType.CorrosiveTissueDamage, CorrosiveHediff },
                { BlightAfflictionType.NeuralResonanceStrain, NeuralHediff },
                { BlightAfflictionType.SymbioticMetabolicStrain, SymbioticHediff },
                { BlightAfflictionType.PhaseveilNeuralFatigue, PhaseveilHediff },
                { BlightAfflictionType.ToxicAccumulation, ToxicAccumulationHediff },
                { BlightAfflictionType.DermalErosion, DermalErosionHediff }
            };

        public static void RegisterAfflictionDef(BlightAfflictionType type, string hediffDefName)
        {
            if (string.IsNullOrWhiteSpace(hediffDefName)) AfflictionDefs.Remove(type);
            else AfflictionDefs[type] = hediffDefName;
        }

        public static void ApplyExposure(Pawn pawn, BlightAfflictionType type, float severity, string source)
        {
            if (severity <= 0f) return;
            if (type == BlightAfflictionType.SporeLungInfection)
                ApplySporeExposure(pawn, severity, source);
            else if (type == BlightAfflictionType.CorrosiveTissueDamage)
                ApplyCorrosiveExposure(pawn, severity, source);
            else if (type == BlightAfflictionType.ToxicAccumulation)
                ApplyToxicExposure(pawn, severity, source);
            else
                ApplyToWholePawn(pawn, HediffFor(type), severity, source, null);
        }

        public static void ApplyWeaponUse(Pawn pawn, Thing weapon)
        {
            BlightWeaponAfflictionExtension extension = weapon?.def?.GetModExtension<BlightWeaponAfflictionExtension>();
            if (pawn == null || extension == null) return;
            string source = weapon.LabelCap.ToString();
            if (extension.sporeSeverityPerUse > 0f)
                ApplySporeExposure(pawn, extension.sporeSeverityPerUse, source);
            if (extension.corrosiveSeverityPerUse > 0f)
                ApplyCorrosiveExposure(pawn, extension.corrosiveSeverityPerUse, source);
            if (extension.neuralSeverityPerUse > 0f)
                ApplyExposure(pawn, BlightAfflictionType.NeuralResonanceStrain, extension.neuralSeverityPerUse, source);
        }

        public static void ApplyApparelExposure(Pawn pawn, Apparel apparel, int ticks)
        {
            BlightApparelAfflictionExtension extension = apparel?.def?.GetModExtension<BlightApparelAfflictionExtension>();
            if (pawn == null || extension == null || ticks <= 0) return;
            float fractionOfDay = ticks / (float)GenDate.TicksPerDay;
            string source = apparel.LabelCap.ToString();
            ApplyExposure(pawn, BlightAfflictionType.NeuralResonanceStrain,
                extension.neuralSeverityPerDay * fractionOfDay, source);
            ApplyExposure(pawn, BlightAfflictionType.SymbioticMetabolicStrain,
                extension.symbioticSeverityPerDay * fractionOfDay, source);
            ApplyExposure(pawn, BlightAfflictionType.PhaseveilNeuralFatigue,
                extension.phaseveilSeverityPerDay * fractionOfDay, source);
        }

        public static void ApplySporeExposure(Pawn pawn, float severityPerUse, string source)
        {
            if (pawn?.health?.hediffSet == null || severityPerUse <= 0f || pawn.RaceProps?.body == null) return;
            List<BodyPartRecord> lungs = pawn.RaceProps.body.AllParts
                .Where(part => part?.def?.defName == "Lung" && !pawn.health.hediffSet.HasMissingPartFor(part))
                .ToList();
            if (lungs.Count == 0) return;

            int protectedLungs = lungs.Count(lung => IsDetoxifierLung(pawn, lung));
            List<BodyPartRecord> susceptible = lungs.Where(lung => !IsDetoxifierLung(pawn, lung)).ToList();
            if (susceptible.Count == 0) return;

            // One detoxifier lung protects its own side and halves the remaining exposure.
            float protectedDose = protectedLungs == 1 ? severityPerUse * 0.5f : severityPerUse;
            string protection = protectedLungs > 0 ? "LiAIChat_DetoxifierLungProtection".Translate().ToString() : null;
            float dosePerLung = protectedDose / susceptible.Count;
            foreach (BodyPartRecord lung in susceptible)
                ApplyToPart(pawn, SporeHediff, lung, dosePerLung, source, protection);
        }

        public static void ApplyCorrosiveExposure(Pawn pawn, float severityPerUse, string source)
        {
            if (pawn?.health?.hediffSet == null || severityPerUse <= 0f || pawn.RaceProps?.body == null) return;
            List<BodyPartRecord> naturalArms = pawn.RaceProps.body.AllParts
                .Where(part => IsArm(part) && !pawn.health.hediffSet.HasMissingPartFor(part) &&
                    !pawn.health.hediffSet.PartOrAnyAncestorHasDirectlyAddedParts(part))
                .ToList();
            if (naturalArms.Count == 0) return;

            // Corrosive weapon dose is specified per natural arm; each eligible arm gets the full dose.
            float dosePerArm = severityPerUse;
            foreach (BodyPartRecord arm in naturalArms)
                ApplyToPart(pawn, CorrosiveHediff, arm, dosePerArm, source, null);
        }

        private static void ApplyToxicExposure(Pawn pawn, float severity, string source)
        {
            if (pawn?.health?.hediffSet == null || severity <= 0f) return;
            int detoxifierLungs = pawn.health.hediffSet.GetNotMissingParts()
                .Where(part => part?.def?.defName == "Lung")
                .Count(part => IsDetoxifierLung(pawn, part));
            float protectionFactor = detoxifierLungs >= 2 ? 0.5f : detoxifierLungs == 1 ? 0.75f : 1f;
            ApplyToWholePawn(pawn, ToxicAccumulationHediff, severity * protectionFactor, source,
                detoxifierLungs > 0 ? "Detoxifier Lung" : null);
        }

        public static bool ReduceAffliction(Pawn pawn, BlightAfflictionType type, float amount)
        {
            if (pawn?.health?.hediffSet == null || amount <= 0f) return false;
            string defName = HediffFor(type);
            if (defName == null) return false;
            bool changed = false;
            foreach (Hediff hediff in pawn.health.hediffSet.hediffs
                .Where(candidate => candidate.def?.defName == defName).ToList())
            {
                hediff.Severity = Mathf.Max(0f, hediff.Severity - amount);
                changed = true;
            }
            return changed;
        }

        public static string HediffFor(BlightAfflictionType type)
        {
            string defName;
            return AfflictionDefs.TryGetValue(type, out defName) ? defName : null;
        }

        public static bool BodyPartNoLongerSusceptible(Hediff hediff)
        {
            if (hediff?.Part == null || hediff.pawn?.health?.hediffSet == null) return false;
            HediffSet set = hediff.pawn.health.hediffSet;
            if (set.HasMissingPartFor(hediff.Part)) return true;
            if (hediff.def?.defName == SporeHediff && IsDetoxifierLung(hediff.pawn, hediff.Part)) return true;
            return hediff.def?.defName == CorrosiveHediff && set.PartOrAnyAncestorHasDirectlyAddedParts(hediff.Part);
        }

        private static bool IsDetoxifierLung(Pawn pawn, BodyPartRecord lung)
        {
            Hediff added = pawn?.health?.hediffSet?.GetDirectlyAddedPartFor(lung);
            return added?.def?.defName == "DetoxifierLung";
        }

        private static bool IsArm(BodyPartRecord part)
        {
            return part?.def != null && (part.def.defName == "Arm" ||
                (part.def.tags != null && part.def.tags.Any(tag => tag?.defName == "ManipulationLimbCore")));
        }

        private static void ApplyToWholePawn(Pawn pawn, string defName, float amount, string source, string protection)
        {
            if (pawn?.health?.hediffSet == null || amount <= 0f || string.IsNullOrEmpty(defName)) return;
            Apply(pawn, defName, null, amount, source, protection);
        }

        private static void ApplyToPart(Pawn pawn, string defName, BodyPartRecord part, float amount, string source, string protection)
        {
            if (pawn?.health?.hediffSet == null || part == null || amount <= 0f || pawn.health.hediffSet.HasMissingPartFor(part)) return;
            Apply(pawn, defName, part, amount, source, protection);
        }

        private static void Apply(Pawn pawn, string defName, BodyPartRecord part, float amount, string source, string protection)
        {
            HediffDef def = DefDatabase<HediffDef>.GetNamedSilentFail(defName);
            if (def == null) return;
            Hediff hediff = pawn.health.hediffSet.hediffs.FirstOrDefault(existing => existing.def == def && existing.Part == part);
            if (hediff == null) hediff = pawn.health.AddHediff(def, part);
            if (hediff == null) return;
            hediff.Severity = Mathf.Clamp(hediff.Severity + amount, 0f, 1f);
            HediffComp_BlightAffliction comp = (hediff as HediffWithComps)?.TryGetComp<HediffComp_BlightAffliction>();
            comp?.RecordExposure(source, protection);
        }
    }

    [HarmonyPatch(typeof(Verb_LaunchProjectile), "TryCastShot")]
    public static class BlightWeaponUsePatch
    {
        public static void Postfix(Verb_LaunchProjectile __instance, bool __result)
        {
            if (!__result || __instance == null) return;
            BlightAfflictionUtility.ApplyWeaponUse(__instance.CasterPawn, __instance.EquipmentSource);
        }
    }

    [HarmonyPatch(typeof(Thing), "get_DescriptionDetailed")]
    public static class BlightEquipmentDescriptionPatch
    {
        private static readonly HashSet<string> BlightEquipmentDefNames = new HashSet<string>
        {
            "LiAIChat_Sporecaster", "LiAIChat_CorrosionProjector", "LiAIChat_SymbioticCarapace",
            "LiAIChat_ResonanceLance", "LiAIChat_SymbioticBlade", "LiAIChat_BlightSeeder",
            "LiAIChat_HeartbreakerCannon", "LiAIChat_PhaseveilArmor", "LiAIChat_BlightAbsorberArmor",
            "LiAIChat_RetaliatorCarapace", "LiAIChat_ResonanceMantle"
        };

        public static void Postfix(Thing __instance, ref string __result)
        {
            if (__instance?.def == null) return;
            BlightWeaponAfflictionExtension weapon = __instance.def.GetModExtension<BlightWeaponAfflictionExtension>();
            if (weapon != null)
            {
                if (weapon.sporeSeverityPerUse > 0f)
                    __result += "\n" + "LiAIChat_BlightWeaponSporeSideEffect".Translate(weapon.sporeSeverityPerUse.ToStringPercent("F1"));
                if (weapon.corrosiveSeverityPerUse > 0f)
                    __result += "\n" + "LiAIChat_BlightWeaponCorrosiveSideEffect".Translate(weapon.corrosiveSeverityPerUse.ToStringPercent("F1"));
                if (weapon.neuralSeverityPerUse > 0f)
                    __result += "\n" + "LiAIChat_BlightWeaponNeuralSideEffect".Translate(weapon.neuralSeverityPerUse.ToStringPercent("F1"));
            }

            BlightApparelAfflictionExtension apparel = __instance.def.GetModExtension<BlightApparelAfflictionExtension>();
            if (apparel != null)
            {
                if (apparel.neuralSeverityPerDay > 0f)
                    __result += "\n" + "LiAIChat_BlightApparelNeuralSideEffect".Translate(apparel.neuralSeverityPerDay.ToStringPercent());
                if (apparel.symbioticSeverityPerDay > 0f)
                    __result += "\n" + "LiAIChat_BlightApparelSymbioticSideEffect".Translate(apparel.symbioticSeverityPerDay.ToStringPercent());
                if (apparel.phaseveilSeverityPerDay > 0f)
                    __result += "\n" + "LiAIChat_BlightApparelPhaseveilSideEffect".Translate(apparel.phaseveilSeverityPerDay.ToStringPercent());
            }

            if (BlightEquipmentDefNames.Contains(__instance.def.defName))
            {
                RecipeMakerProperties recipe = __instance.def.recipeMaker;
                string research = recipe?.researchPrerequisite?.LabelCap ?? "LiAIChat_BlightNoResearchPrerequisite".Translate().ToString();
                string materials = __instance.def.costList == null || __instance.def.costList.Count == 0
                    ? "LiAIChat_BlightNoCraftingMaterials".Translate().ToString()
                    : string.Join(", ", __instance.def.costList.Select(cost => cost.count + " " + cost.thingDef.label));
                __result += "\n" + "LiAIChat_BlightEquipmentRequirements".Translate(research, materials);
            }
        }
    }
}
