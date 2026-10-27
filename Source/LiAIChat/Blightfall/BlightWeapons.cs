using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace LiAIChat.Blightfall
{
    public class HediffCompProperties_BlightSporeInfection : HediffCompProperties
    {
        public float recoveryPerDay = 0.01f;
        public float deathCloudThreshold = 0.5f;

        public HediffCompProperties_BlightSporeInfection()
        {
            compClass = typeof(HediffComp_BlightSporeInfection);
        }
    }

    public class HediffComp_BlightSporeInfection : HediffComp
    {
        private bool deathCloudReleased;
        private HediffCompProperties_BlightSporeInfection Props =>
            (HediffCompProperties_BlightSporeInfection)props;

        public void AddExposure(float severity)
        {
            if (parent != null && severity > 0f)
                parent.Severity = Mathf.Min(parent.def.maxSeverity, parent.Severity + severity);
        }

        public override void CompPostTickInterval(ref float severityAdjustment, int interval)
        {
            severityAdjustment -= Props.recoveryPerDay * interval / GenDate.TicksPerDay;
        }

        public bool TryReleaseDeathCloud(Pawn pawn)
        {
            if (deathCloudReleased || parent == null || pawn?.Map == null ||
                parent.Severity < Props.deathCloudThreshold)
                return false;

            // Mark first so repeated Kill calls or save/load cannot produce another cloud.
            deathCloudReleased = true;
            Map map = pawn.Map;
            IntVec3 center = pawn.Position;
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, 3f, true))
            {
                if (cell.InBounds(map))
                    GasUtility.AddGas(cell, map, GasType.ToxGas, 18);
            }
            return true;
        }

        public override string CompTipStringExtra => "LiAIChat_SporeInfectionTooltip".Translate(
            parent.Severity.ToStringPercent(), Props.deathCloudThreshold.ToStringPercent()).ToString();

        public override void CompExposeData()
        {
            Scribe_Values.Look(ref deathCloudReleased, "blightSporeDeathCloudReleased", false);
        }
    }

    public class HediffCompProperties_BlightArmorCorrosion : HediffCompProperties
    {
        public int durationTicks = 1200;
        public float severityPerHit = 0.08f;
        public float maxSeverity = 0.24f;

        public HediffCompProperties_BlightArmorCorrosion()
        {
            compClass = typeof(HediffComp_BlightArmorCorrosion);
        }
    }

    public class HediffComp_BlightArmorCorrosion : HediffComp
    {
        private int ticksRemaining;
        private HediffCompProperties_BlightArmorCorrosion Props =>
            (HediffCompProperties_BlightArmorCorrosion)props;

        public int Layers => Mathf.Clamp(Mathf.RoundToInt(parent.Severity / Props.severityPerHit), 0, 3);

        public void AddCorrosionHit()
        {
            if (parent == null) return;
            parent.Severity = Mathf.Min(Props.maxSeverity, parent.Severity + Props.severityPerHit);
            ticksRemaining = Props.durationTicks;
        }

        public override void CompPostTickInterval(ref float severityAdjustment, int interval)
        {
            if (ticksRemaining <= 0) return;
            ticksRemaining -= interval;
            if (ticksRemaining <= 0 && parent != null)
                parent.Severity = 0f;
        }

        public override string CompTipStringExtra => "LiAIChat_ArmorCorrosionTooltip".Translate(
            Layers, (Layers * Props.severityPerHit).ToStringPercent(),
            (Mathf.Max(0, ticksRemaining) / 60f).ToString("0.#")).ToString();

        public override void CompExposeData()
        {
            Scribe_Values.Look(ref ticksRemaining, "blightArmorCorrosionTicksRemaining", 0);
        }
    }

    public static class BlightWeaponImpactUtility
    {
        public const string SporeInfectionDefName = "LiAIChat_BlightSporeInfection";
        public const string ArmorCorrosionDefName = "LiAIChat_BlightArmorCorrosion";
        private const float SporeSeverityPerHit = 0.12f;

        public static void ApplySporeHit(Pawn pawn)
        {
            if (!IsBiologicalTarget(pawn)) return;
            HediffDef def = DefDatabase<HediffDef>.GetNamedSilentFail(SporeInfectionDefName);
            if (def == null) return;
            Hediff hediff = pawn.health.hediffSet.hediffs.FirstOrDefault(h => h.def == def);
            if (hediff == null) hediff = pawn.health.AddHediff(def);
            (hediff as HediffWithComps)?.TryGetComp<HediffComp_BlightSporeInfection>()?.AddExposure(SporeSeverityPerHit);
        }

        public static void ApplyCorrosionHit(Pawn pawn)
        {
            if (pawn?.health?.hediffSet == null || pawn.Dead) return;
            HediffDef def = DefDatabase<HediffDef>.GetNamedSilentFail(ArmorCorrosionDefName);
            if (def == null) return;
            Hediff hediff = pawn.health.hediffSet.hediffs.FirstOrDefault(h => h.def == def);
            if (hediff == null) hediff = pawn.health.AddHediff(def);
            (hediff as HediffWithComps)?.TryGetComp<HediffComp_BlightArmorCorrosion>()?.AddCorrosionHit();
        }

        public static void TryReleaseDeathCloud(Pawn pawn)
        {
            if (pawn?.health?.hediffSet == null) return;
            Hediff infection = pawn.health.hediffSet.hediffs.FirstOrDefault(
                h => h.def?.defName == SporeInfectionDefName);
            (infection as HediffWithComps)?.TryGetComp<HediffComp_BlightSporeInfection>()?.TryReleaseDeathCloud(pawn);
        }

        public static int CorrosionLayers(Pawn pawn)
        {
            Hediff corrosion = pawn?.health?.hediffSet?.hediffs.FirstOrDefault(
                h => h.def?.defName == ArmorCorrosionDefName);
            return (corrosion as HediffWithComps)?.TryGetComp<HediffComp_BlightArmorCorrosion>()?.Layers ?? 0;
        }

        private static bool IsBiologicalTarget(Pawn pawn)
        {
            return pawn?.health?.hediffSet != null && pawn.RaceProps?.IsFlesh == true &&
                   pawn.RaceProps.IsMechanoid == false && !pawn.Dead;
        }
    }

    public class Projectile_BlightWeapon : Projectile
    {
        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            if (!blockedByShield && hitThing is Pawn pawn)
            {
                if (def.defName == "LiAIChat_SporecasterBullet")
                    BlightWeaponImpactUtility.ApplySporeHit(pawn);
                else if (def.defName == "LiAIChat_CorrosionProjectorBolt")
                    BlightWeaponImpactUtility.ApplyCorrosionHit(pawn);
            }
            base.Impact(hitThing, blockedByShield);
        }
    }

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.Kill))]
    public static class BlightSporeDeathCloudPatch
    {
        public static void Postfix(Pawn __instance)
        {
            if (__instance?.Dead == true)
                BlightWeaponImpactUtility.TryReleaseDeathCloud(__instance);
        }
    }

    [HarmonyPatch]
    public static class BlightArmorCorrosionPatch
    {
        public static System.Reflection.MethodBase TargetMethod()
        {
            return AccessTools.GetDeclaredMethods(typeof(ArmorUtility))
                .FirstOrDefault(method => method.Name == "ApplyArmor");
        }

        public static void Prefix(Pawn pawn, ref float armorRating)
        {
            int layers = BlightWeaponImpactUtility.CorrosionLayers(pawn);
            if (layers > 0)
                armorRating *= 1f - Mathf.Min(0.24f, layers * 0.08f);
        }
    }
}
