using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace LiAIChat.Blightfall
{
    public class HediffCompProperties_BlightAbsorberBond : HediffCompProperties
    {
        public int checkIntervalTicks = 250;
        public HediffCompProperties_BlightAbsorberBond() => compClass = typeof(HediffComp_BlightAbsorberBond);
    }

    public class HediffComp_BlightAbsorberBond : HediffComp
    {
        private int ticksToCheck = 250;
        public override void CompPostTickInterval(ref float severityAdjustment, int interval)
        {
            ticksToCheck -= interval;
            if (ticksToCheck > 0) return;
            ticksToCheck += ((HediffCompProperties_BlightAbsorberBond)props).checkIntervalTicks;
            Pawn pawn = parent?.pawn;
            if (pawn?.Map == null || pawn.Dead || pawn.Downed ||
                !BlightArmorUtility.IsWearing(pawn, BlightArmorUtility.BlightAbsorberArmorDefName)) return;
            BlightArmorUtility.AbsorbNearbyPollution(pawn);
        }
        public override void CompExposeData() => Scribe_Values.Look(ref ticksToCheck, "blightAbsorberTicksToCheck", 250);
    }

    public class HediffCompProperties_ResonanceMantleActive : HediffCompProperties
    {
        public int pulseIntervalTicks = 60;
        public int targetDebuffTicks = 180;
        public float neuralStrainPerSecond = 0.002f;
        public float radius = 8f;
        public HediffCompProperties_ResonanceMantleActive() => compClass = typeof(HediffComp_ResonanceMantleActive);
    }

    public class HediffComp_ResonanceMantleActive : HediffComp
    {
        private int ticksToPulse = 60;
        public override bool CompShouldRemove => parent?.pawn == null || parent.pawn.Dead || parent.pawn.Downed ||
            !BlightArmorUtility.IsWearing(parent.pawn, BlightArmorUtility.ResonanceMantleDefName);

        public override void CompPostTickInterval(ref float severityAdjustment, int interval)
        {
            Pawn pawn = parent?.pawn;
            if (pawn == null || CompShouldRemove) return;
            HediffCompProperties_ResonanceMantleActive settings = (HediffCompProperties_ResonanceMantleActive)props;
            BlightAfflictionUtility.ApplyExposure(pawn, BlightAfflictionType.NeuralResonanceStrain,
                settings.neuralStrainPerSecond * interval / 60f, BlightArmorUtility.ResonanceMantleDefName);
            ticksToPulse -= interval;
            if (ticksToPulse > 0) return;
            ticksToPulse += settings.pulseIntervalTicks;
            ApplyField(pawn, settings);
        }

        public override void CompExposeData() => Scribe_Values.Look(ref ticksToPulse, "mantleTicksToPulse", 60);

        private static void ApplyField(Pawn wearer, HediffCompProperties_ResonanceMantleActive settings)
        {
            Map map = wearer.Map;
            if (map == null || wearer.Faction == null) return;
            HediffDef disruptionDef = DefDatabase<HediffDef>.GetNamedSilentFail(BlightArmorUtility.MantleDisruptionHediff);
            if (disruptionDef == null) return;
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(wearer.Position, settings.radius, true))
            {
                if (!cell.InBounds(map)) continue;
                foreach (Pawn target in map.thingGrid.ThingsListAt(cell).OfType<Pawn>())
                {
                    if (!AdvancedBlightWeaponUtility.IsLivingFlesh(target) || target == wearer ||
                        !wearer.HostileTo(target)) continue;
                    Hediff disruption = target.health.hediffSet.hediffs.FirstOrDefault(h => h.def == disruptionDef);
                    if (disruption == null) disruption = target.health.AddHediff(disruptionDef);
                    disruption?.TryGetComp<HediffComp_BlightTimedEffect>()?.Refresh();
                }
            }
        }
    }

    public class HediffCompProperties_DermalErosionBond : HediffCompProperties
    {
        public int intervalTicks = 250;
        public float severityPerDay = 0.035f;
        public HediffCompProperties_DermalErosionBond() => compClass = typeof(HediffComp_DermalErosionBond);
    }

    public class HediffComp_DermalErosionBond : HediffComp
    {
        private int ticksToCheck = 250;
        public override void CompPostTickInterval(ref float severityAdjustment, int interval)
        {
            ticksToCheck -= interval;
            if (ticksToCheck > 0) return;
            HediffCompProperties_DermalErosionBond settings = (HediffCompProperties_DermalErosionBond)props;
            int elapsed = settings.intervalTicks - ticksToCheck;
            ticksToCheck += settings.intervalTicks;
            Pawn pawn = parent?.pawn;
            if (pawn == null || pawn.Dead || !BlightArmorUtility.IsWearing(pawn, AdvancedBlightArmorUtility.RetaliatorCarapaceDefName)) return;
            BlightAfflictionUtility.ApplyExposure(pawn, BlightAfflictionType.DermalErosion,
                settings.severityPerDay * elapsed / GenDate.TicksPerDay, AdvancedBlightArmorUtility.DermalErosionSourceName);
        }
        public override void CompExposeData() => Scribe_Values.Look(ref ticksToCheck, "dermalErosionTicksToCheck", 250);
    }

    public static class AdvancedBlightArmorUtility
    {
        public const string RetaliatorCarapaceDefName = "LiAIChat_RetaliatorCarapace";
        private const float MinRetaliationDamage = 2f;
        private const float SpinesDamage = 6f;
        private const float RetaliationChance = 0.4f;
        private const string RetaliationCooldownHediff = "LiAIChat_RetaliatorCooldown";
        private const string SpineToxinHediff = "LiAIChat_SpineToxin";
        public const string DermalErosionSourceName = "LiAIChat_RetaliatorCarapace";
        private static readonly HashSet<int> RetaliationInProgress = new HashSet<int>();

        public static bool IsRetaliationInProgress => RetaliationInProgress.Count > 0;

        public static void OnPawnTookDamage(Pawn victim, DamageInfo dinfo, float actualDamage)
        {
            if (victim?.health?.hediffSet == null || victim.Dead || actualDamage < MinRetaliationDamage ||
                IsRetaliationInProgress || dinfo.Def == null || dinfo.Def.isRanged || dinfo.Def.isExplosive ||
                !BlightArmorUtility.IsWearing(victim, RetaliatorCarapaceDefName)) return;
            Pawn attacker = dinfo.Instigator as Pawn;
            if (attacker == null || attacker == victim || attacker.Dead || !attacker.Spawned || attacker.Map != victim.Map ||
                !attacker.HostileTo(victim) || attacker.Position.DistanceTo(victim.Position) > 2.1f ||
                HasHediff(victim, RetaliationCooldownHediff) ||
                Rand.Value >= RetaliationChance) return;

            HediffDef cooldown = DefDatabase<HediffDef>.GetNamedSilentFail(RetaliationCooldownHediff);
            if (cooldown != null) victim.health.AddHediff(cooldown);
            BlightAfflictionUtility.ApplyExposure(victim, BlightAfflictionType.DermalErosion, 0.01f, DermalErosionSourceName);
            if (!RetaliationInProgress.Add(victim.thingIDNumber)) return;
            try
            {
                attacker.TakeDamage(new DamageInfo(DamageDefOf.Stab, SpinesDamage, 0.1f, -1f, victim));
                AdvancedBlightWeaponUtility.ApplyTimedHediff(attacker, SpineToxinHediff);
            }
            finally { RetaliationInProgress.Remove(victim.thingIDNumber); }
        }

        public static bool CanActivateMantle(Pawn pawn, out string reason)
        {
            reason = null;
            if (pawn == null || !BlightArmorUtility.IsWearing(pawn, BlightArmorUtility.ResonanceMantleDefName))
            { reason = "LiAIChat_MantleMustBeWorn".Translate(); return false; }
            if (HasHediff(pawn, BlightArmorUtility.MantleActiveHediff))
            { reason = "LiAIChat_MantleAlreadyActive".Translate(); return false; }
            if (HasHediff(pawn, BlightArmorUtility.MantleCooldownHediff))
            { reason = "LiAIChat_MantleOnCooldown".Translate(); return false; }
            Hediff strain = BlightArmorUtility.FindHediffPublic(pawn, BlightAfflictionUtility.NeuralHediff);
            if (strain != null && strain.Severity >= 0.75f)
            { reason = "LiAIChat_MantleTooStrained".Translate(); return false; }
            if (pawn.Dead || pawn.Downed || pawn.Map == null || pawn.health.capacities.GetLevel(PawnCapacityDefOf.Consciousness) <= 0.01f)
            { reason = "LiAIChat_MantleUnconscious".Translate(); return false; }
            return true;
        }

        public static void ActivateMantle(Pawn pawn)
        {
            string reason;
            if (!CanActivateMantle(pawn, out reason))
            {
                if (!string.IsNullOrEmpty(reason)) Messages.Message(reason, pawn, MessageTypeDefOf.RejectInput, false);
                return;
            }
            HediffDef active = DefDatabase<HediffDef>.GetNamedSilentFail(BlightArmorUtility.MantleActiveHediff);
            HediffDef cooldown = DefDatabase<HediffDef>.GetNamedSilentFail(BlightArmorUtility.MantleCooldownHediff);
            if (active == null || cooldown == null) return;
            pawn.health.AddHediff(active);
            pawn.health.AddHediff(cooldown);
            Messages.Message("LiAIChat_MantleActivated".Translate(pawn.LabelShort), pawn, MessageTypeDefOf.PositiveEvent, false);
        }

        private static bool HasHediff(Pawn pawn, string name) => BlightArmorUtility.FindHediffPublic(pawn, name) != null;
    }

    [HarmonyPatch(typeof(Thing), nameof(Thing.TakeDamage))]
    public static class BlightAbsorberShieldDamagePatch
    {
        public static void Prefix(Thing __instance, ref DamageInfo dinfo)
        {
            Pawn pawn = __instance as Pawn;
            if (pawn == null || !BlightArmorUtility.HasActiveAbsorberShield(pawn) ||
                !BlightArmorUtility.IsWearing(pawn, BlightArmorUtility.BlightAbsorberArmorDefName) || dinfo.Amount <= 0f) return;
            float absorbed = BlightArmorUtility.ConsumeAbsorberEnergy(pawn, dinfo.Amount);
            if (absorbed <= 0f) return;
            dinfo.SetAmount(Mathf.Max(0f, dinfo.Amount - absorbed));
            if (dinfo.Amount <= 0.001f) BlightArmorUtility.EndAbsorberShield(pawn);
        }
    }

    [HarmonyPatch(typeof(Thing), nameof(Thing.TakeDamage))]
    public static class RetaliatorCarapaceDamagePatch
    {
        public static void Postfix(Thing __instance, DamageInfo dinfo, DamageWorker.DamageResult __result)
        {
            Pawn pawn = __instance as Pawn;
            if (pawn != null && !AdvancedBlightArmorUtility.IsRetaliationInProgress)
                AdvancedBlightArmorUtility.OnPawnTookDamage(pawn, dinfo, __result.totalDamageDealt);
        }
    }
}
