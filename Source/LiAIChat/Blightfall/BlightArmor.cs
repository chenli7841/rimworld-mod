using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace LiAIChat.Blightfall
{
    public class HediffCompProperties_SymbioticRegeneration : HediffCompProperties
    {
        public int checkIntervalTicks = 1000;
        public float injuryHealPerCheck = 1.5f;
        public float metabolicSeverityPerRegeneration = 0.004f;

        public HediffCompProperties_SymbioticRegeneration()
        {
            compClass = typeof(HediffComp_SymbioticRegeneration);
        }
    }

    public class HediffComp_SymbioticRegeneration : HediffComp
    {
        private int ticksToNextCheck;
        private HediffCompProperties_SymbioticRegeneration Props =>
            (HediffCompProperties_SymbioticRegeneration)props;

        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            ticksToNextCheck = Props.checkIntervalTicks;
        }

        public override void CompPostTickInterval(ref float severityAdjustment, int interval)
        {
            ticksToNextCheck -= interval;
            if (ticksToNextCheck > 0) return;
            ticksToNextCheck = Props.checkIntervalTicks;
            TryRegenerate();
        }

        public override void CompExposeData()
        {
            Scribe_Values.Look(ref ticksToNextCheck, "symbioticRegenerationTicksToCheck", 0);
        }

        private void TryRegenerate()
        {
            Pawn pawn = parent?.pawn;
            if (pawn?.health?.hediffSet == null || pawn.Dead || pawn.Downed ||
                !BlightArmorUtility.IsWearing(pawn, BlightArmorUtility.SymbioticCarapaceDefName))
                return;

            // Avoid spending extra nutrition to regenerate while the wearer is already hungry.
            if (pawn.needs?.food != null && pawn.needs.food.CurCategory >= HungerCategory.Hungry)
                return;

            Hediff_Injury injury = pawn.health.hediffSet.hediffs
                .OfType<Hediff_Injury>()
                .Where(candidate => candidate.Severity > 0f && candidate.Part != null &&
                    !pawn.health.hediffSet.HasMissingPartFor(candidate.Part) &&
                    !HediffUtility.IsPermanent(candidate))
                .OrderByDescending(candidate => candidate.Severity)
                .FirstOrDefault();
            if (injury == null) return;

            float before = injury.Severity;
            injury.Heal(Props.injuryHealPerCheck);
            if (injury.Severity < before)
                BlightAfflictionUtility.ApplyExposure(pawn, BlightAfflictionType.SymbioticMetabolicStrain,
                    Props.metabolicSeverityPerRegeneration, "LiAIChat_SymbioticCarapace".Translate().ToString());
        }
    }

    public class HediffCompProperties_PhaseveilActive : HediffCompProperties
    {
        public float neuralFatiguePerSecond = 0.004f;

        public HediffCompProperties_PhaseveilActive()
        {
            compClass = typeof(HediffComp_PhaseveilActive);
        }
    }

    public class HediffComp_PhaseveilActive : HediffComp
    {
        private HediffCompProperties_PhaseveilActive Props =>
            (HediffCompProperties_PhaseveilActive)props;

        public override bool CompShouldRemove
        {
            get
            {
                Pawn pawn = parent?.pawn;
                return pawn == null || pawn.Dead || pawn.Downed ||
                    pawn.health.capacities.GetLevel(PawnCapacityDefOf.Consciousness) <= 0.01f;
            }
        }

        public override void CompPostTickInterval(ref float severityAdjustment, int interval)
        {
            Pawn pawn = parent?.pawn;
            if (pawn == null || CompShouldRemove) return;
            BlightAfflictionUtility.ApplyExposure(pawn, BlightAfflictionType.PhaseveilNeuralFatigue,
                Props.neuralFatiguePerSecond * interval / 60f, "LiAIChat_PhaseveilArmor".Translate().ToString());
        }
    }

    public class CompProperties_BlightArmor : CompProperties
    {
        public CompProperties_BlightArmor()
        {
            compClass = typeof(ThingComp_BlightArmor);
        }
    }

    public class ThingComp_BlightArmor : ThingComp
    {
        public override void Notify_Equipped(Pawn pawn)
        {
            base.Notify_Equipped(pawn);
            BlightArmorUtility.SyncWornEffects(pawn);
        }

        public override void Notify_Unequipped(Pawn pawn)
        {
            base.Notify_Unequipped(pawn);
            BlightArmorUtility.SyncWornEffects(pawn);
        }

        public override IEnumerable<Gizmo> CompGetWornGizmosExtra()
        {
            IEnumerable<Gizmo> baseGizmos = base.CompGetWornGizmosExtra();
            if (baseGizmos != null)
                foreach (Gizmo gizmo in baseGizmos)
                    yield return gizmo;

            if (parent?.def?.defName != BlightArmorUtility.PhaseveilArmorDefName) yield break;
            Pawn pawn = (parent as Apparel)?.Wearer;
            if (pawn == null) yield break;

            string disabledReason;
            bool canActivate = BlightArmorUtility.CanActivatePhaseveil(pawn, out disabledReason);
            yield return new Command_Action
            {
                defaultLabel = "LiAIChat_ActivatePhaseveil".Translate(),
                defaultDesc = "LiAIChat_ActivatePhaseveilDesc".Translate(),
                action = () => BlightArmorUtility.ActivatePhaseveil(pawn),
                Disabled = !canActivate,
                disabledReason = disabledReason
            };
        }
    }

    public static class BlightArmorUtility
    {
        public const string SymbioticCarapaceDefName = "LiAIChat_SymbioticCarapace";
        public const string PhaseveilArmorDefName = "LiAIChat_PhaseveilArmor";
        public const string SymbioticBondHediff = "LiAIChat_SymbioticCarapaceBond";
        public const string SymbioticMetabolicHediff = "LiAIChat_SymbioticMetabolicStrain";
        public const string PhaseveilFatigueHediff = "LiAIChat_PhaseveilNeuralFatigue";
        public const string PhaseveilActiveHediff = "LiAIChat_PhaseveilActive";
        public const string PhaseveilCooldownHediff = "LiAIChat_PhaseveilCooldown";
        public const float PhaseveilFatigueLockSeverity = 0.75f;

        public static void SyncWornEffects(Pawn pawn)
        {
            if (pawn?.health?.hediffSet == null || pawn.apparel == null) return;
            bool wearingCarapace = IsWearing(pawn, SymbioticCarapaceDefName);
            SetHediffPresence(pawn, SymbioticBondHediff, wearingCarapace);

            if (!IsWearing(pawn, PhaseveilArmorDefName))
                EndPhaseveil(pawn);
        }

        public static bool IsWearing(Pawn pawn, string apparelDefName)
        {
            return pawn?.apparel?.WornApparel?.Any(apparel => apparel?.def?.defName == apparelDefName) == true;
        }

        public static bool CanActivatePhaseveil(Pawn pawn, out string reason)
        {
            reason = null;
            if (pawn == null || !IsWearing(pawn, PhaseveilArmorDefName))
            {
                reason = "LiAIChat_PhaseveilMustBeWorn".Translate();
                return false;
            }
            if (HasHediff(pawn, PhaseveilActiveHediff))
            {
                reason = "LiAIChat_PhaseveilAlreadyActive".Translate();
                return false;
            }
            if (HasHediff(pawn, PhaseveilCooldownHediff))
            {
                reason = "LiAIChat_PhaseveilOnCooldown".Translate();
                return false;
            }
            Hediff fatigue = FindHediff(pawn, PhaseveilFatigueHediff);
            if (fatigue != null && fatigue.Severity >= PhaseveilFatigueLockSeverity)
            {
                reason = "LiAIChat_PhaseveilTooFatigued".Translate();
                return false;
            }
            if (pawn.Dead || pawn.Downed || pawn.health.capacities.GetLevel(PawnCapacityDefOf.Consciousness) <= 0.01f)
            {
                reason = "LiAIChat_PhaseveilUnconscious".Translate();
                return false;
            }
            return true;
        }

        public static void ActivatePhaseveil(Pawn pawn)
        {
            string reason;
            if (!CanActivatePhaseveil(pawn, out reason))
            {
                if (!string.IsNullOrEmpty(reason))
                    Messages.Message(reason, pawn, MessageTypeDefOf.RejectInput, false);
                return;
            }

            HediffDef activeDef = DefDatabase<HediffDef>.GetNamedSilentFail(PhaseveilActiveHediff);
            HediffDef cooldownDef = DefDatabase<HediffDef>.GetNamedSilentFail(PhaseveilCooldownHediff);
            if (activeDef == null || cooldownDef == null) return;

            pawn.health.AddHediff(activeDef);
            pawn.health.AddHediff(cooldownDef);
            Messages.Message("LiAIChat_PhaseveilActivated".Translate(pawn.LabelShort), pawn,
                MessageTypeDefOf.PositiveEvent, false);
        }

        public static void EndPhaseveil(Pawn pawn)
        {
            Hediff active = FindHediff(pawn, PhaseveilActiveHediff);
            if (active != null)
                pawn.health.RemoveHediff(active);
        }

        public static void OnPawnKilled(Pawn pawn)
        {
            if (pawn?.health?.hediffSet == null) return;
            EndPhaseveil(pawn);
            SetHediffPresence(pawn, SymbioticBondHediff, false);
        }

        private static bool HasHediff(Pawn pawn, string defName) => FindHediff(pawn, defName) != null;

        private static Hediff FindHediff(Pawn pawn, string defName)
        {
            return pawn?.health?.hediffSet?.hediffs.FirstOrDefault(hediff => hediff.def?.defName == defName);
        }

        private static void SetHediffPresence(Pawn pawn, string defName, bool shouldExist)
        {
            if (pawn?.health?.hediffSet == null) return;
            Hediff hediff = FindHediff(pawn, defName);
            if (shouldExist && hediff == null)
            {
                HediffDef def = DefDatabase<HediffDef>.GetNamedSilentFail(defName);
                if (def != null) pawn.health.AddHediff(def);
            }
            else if (!shouldExist && hediff != null)
                pawn.health.RemoveHediff(hediff);
        }
    }

    [HarmonyPatch]
    public static class BlightArmorApparelChangedPatch
    {
        public static System.Reflection.MethodBase TargetMethod() =>
            AccessTools.Method(typeof(Pawn_ApparelTracker), "Notify_ApparelChanged");

        public static void Postfix(Pawn ___pawn)
        {
            BlightArmorUtility.SyncWornEffects(___pawn);
        }
    }

    [HarmonyPatch(typeof(Need_Food), "get_FoodFallPerTick")]
    public static class BlightCarapaceHungerPatch
    {
        public static void Postfix(Need_Food __instance, Pawn ___pawn, ref float __result)
        {
            Pawn pawn = ___pawn;
            if (pawn?.health?.hediffSet?.hediffs.Any(hediff =>
                    hediff.def?.defName == BlightArmorUtility.SymbioticBondHediff) == true)
                __result *= 1.5f;
        }
    }

    [HarmonyPatch(typeof(Verb_LaunchProjectile), "TryCastShot")]
    public static class BlightPhaseveilRangedAttackPatch
    {
        public static void Postfix(Verb_LaunchProjectile __instance, bool __result)
        {
            if (__result)
                BlightArmorUtility.EndPhaseveil(__instance?.CasterPawn);
        }
    }

    [HarmonyPatch(typeof(Verb_MeleeAttack), "TryCastShot")]
    public static class BlightPhaseveilMeleeAttackPatch
    {
        public static void Postfix(Verb_MeleeAttack __instance, bool __result)
        {
            if (__result)
                BlightArmorUtility.EndPhaseveil(__instance?.CasterPawn);
        }
    }

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.Kill))]
    public static class BlightArmorPawnKilledPatch
    {
        public static void Postfix(Pawn __instance)
        {
            if (__instance?.Dead == true)
                BlightArmorUtility.OnPawnKilled(__instance);
        }
    }
}
