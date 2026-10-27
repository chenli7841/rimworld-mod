using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace LiAIChat.Blightfall
{
    public class HediffCompProperties_BlightTimedEffect : HediffCompProperties
    {
        public int durationTicks = 1200;

        public HediffCompProperties_BlightTimedEffect()
        {
            compClass = typeof(HediffComp_BlightTimedEffect);
        }
    }

    public class CompProperties_BlightSeed : CompProperties
    {
        public CompProperties_BlightSeed()
        {
            compClass = typeof(ThingComp_BlightSeed);
        }
    }

    public class HediffComp_BlightTimedEffect : HediffComp
    {
        private int ticksRemaining;
        private HediffCompProperties_BlightTimedEffect Props => (HediffCompProperties_BlightTimedEffect)props;

        public void Refresh()
        {
            ticksRemaining = Props.durationTicks;
        }

        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            Refresh();
        }

        public override void CompPostTickInterval(ref float severityAdjustment, int interval)
        {
            if (ticksRemaining <= 0) return;
            ticksRemaining -= interval;
            if (ticksRemaining <= 0 && parent != null)
                parent.Severity = 0f;
        }

        public override void CompExposeData()
        {
            Scribe_Values.Look(ref ticksRemaining, "blightTimedEffectTicksRemaining", 0);
        }
    }

    public static class AdvancedBlightWeaponUtility
    {
        public const string ResonanceLanceDefName = "LiAIChat_ResonanceLance";
        public const string SymbioticBladeDefName = "LiAIChat_SymbioticBlade";
        public const string BlightSeederDefName = "LiAIChat_BlightSeeder";
        public const string HeartbreakerCannonDefName = "LiAIChat_HeartbreakerCannon";
        public const string ResonanceDisruptionHediff = "LiAIChat_NeuralDisruption";
        public const string SeedSlowHediff = "LiAIChat_BlightSeedDisruption";
        public const string ShieldDisruptionHediff = "LiAIChat_ShieldRecoveryDisrupted";
        public const string BlightSeedDefName = "LiAIChat_BlightSeedNode";
        private static readonly MethodInfo ShieldBreakMethod = AccessTools.Method(typeof(CompShield), "Break");

        public static void OnProjectileImpact(string projectileDefName, Thing hitThing, Map map, IntVec3 cell, Pawn launcher)
        {
            if (projectileDefName == "LiAIChat_ResonanceLanceBolt" && hitThing is Pawn target)
                ApplyTimedHediff(target, ResonanceDisruptionHediff);
            else if (projectileDefName == "LiAIChat_BlightSeederPod")
                SpawnBlightSeed(map, cell, launcher);
        }

        public static void OnBladeDamage(Thing victim, DamageInfo dinfo, float actualDamage)
        {
            if (actualDamage <= 0f || !(victim is Pawn target) || !IsBiologicalPawn(target)) return;
            Pawn attacker = dinfo.Instigator as Pawn;
            if (attacker == null || attacker.Dead || !attacker.HostileTo(target)) return;

            float requestedHealing = Mathf.Min(actualDamage * 0.2f, 4f);
            float healed = HealOrdinaryInjury(attacker, requestedHealing);
            if (healed <= 0f) return;

            if (attacker.needs?.food != null)
                attacker.needs.food.CurLevel = Mathf.Max(0f, attacker.needs.food.CurLevel - 0.01f);
            string source = DefDatabase<ThingDef>.GetNamedSilentFail(SymbioticBladeDefName)?.LabelCap.ToString() ?? SymbioticBladeDefName;
            BlightAfflictionUtility.ApplyExposure(attacker, BlightAfflictionType.SymbioticMetabolicStrain,
                0.01f, source);
        }

        public static float HealOrdinaryInjury(Pawn pawn, float amount)
        {
            if (pawn?.health?.hediffSet == null || pawn.Dead || amount <= 0f) return 0f;
            float remaining = amount;
            float healed = 0f;
            foreach (Hediff_Injury injury in pawn.health.hediffSet.hediffs
                .OfType<Hediff_Injury>()
                .Where(candidate => candidate.Severity > 0f && candidate.Part != null &&
                    !pawn.health.hediffSet.HasMissingPartFor(candidate.Part) && !HediffUtility.IsPermanent(candidate))
                .OrderByDescending(candidate => candidate.Severity).ToList())
            {
                if (remaining <= 0f) break;
                float before = injury.Severity;
                injury.Heal(remaining);
                float amountHealed = before - injury.Severity;
                healed += amountHealed;
                remaining -= amountHealed;
            }
            return healed;
        }

        public static void ApplyTimedHediff(Pawn pawn, string hediffDefName)
        {
            if (!IsLivingFlesh(pawn)) return;
            HediffDef def = DefDatabase<HediffDef>.GetNamedSilentFail(hediffDefName);
            if (def == null) return;
            Hediff hediff = pawn.health.hediffSet.hediffs.FirstOrDefault(existing => existing.def == def);
            if (hediff == null) hediff = pawn.health.AddHediff(def);
            (hediff as HediffWithComps)?.TryGetComp<HediffComp_BlightTimedEffect>()?.Refresh();
        }

        public static void SpawnBlightSeed(Map map, IntVec3 impactCell, Pawn owner = null)
        {
            if (map == null || !impactCell.InBounds(map)) return;
            ThingDef seedDef = DefDatabase<ThingDef>.GetNamedSilentFail(BlightSeedDefName);
            if (seedDef == null) return;
            IntVec3 spawnCell = IntVec3.Invalid;
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(impactCell, 2f, true))
            {
                if (!cell.InBounds(map) || !cell.Walkable(map) || cell.GetEdifice(map) != null) continue;
                spawnCell = cell;
                break;
            }
            if (!spawnCell.IsValid) return;

            if (owner != null) ExpireOldestSeeds(owner);
            Thing seed = ThingMaker.MakeThing(seedDef);
            seed.TryGetComp<ThingComp_BlightSeed>()?.SetOwner(owner);
            GenSpawn.Spawn(seed, spawnCell, map, WipeMode.VanishOrMoveAside);
        }

        private static void ExpireOldestSeeds(Pawn owner)
        {
            List<ThingComp_BlightSeed> ownedSeeds = Find.Maps
                .Where(map => map != null)
                .SelectMany(map => map.listerThings.AllThings)
                .Select(thing => thing.TryGetComp<ThingComp_BlightSeed>())
                .Where(comp => comp?.Owner == owner && comp.parent.Spawned)
                .OrderBy(comp => comp.CreatedAtTick)
                .ToList();
            while (ownedSeeds.Count >= 2)
            {
                ThingComp_BlightSeed oldest = ownedSeeds[0];
                ownedSeeds.RemoveAt(0);
                oldest.parent.Destroy(DestroyMode.Vanish);
            }
        }

        public static bool IsLivingFlesh(Pawn pawn)
        {
            return pawn?.health?.hediffSet != null && !pawn.Dead && IsBiologicalPawn(pawn);
        }

        private static bool IsBiologicalPawn(Pawn pawn) => pawn?.RaceProps?.IsFlesh == true && pawn.RaceProps.IsMechanoid == false;

        public static void DisruptNearbyEnemies(Map map, IntVec3 center, Pawn owner)
        {
            HashSet<Pawn> affected = new HashSet<Pawn>();
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, 4f, true))
            {
                if (!cell.InBounds(map)) continue;
                foreach (Thing thing in map.thingGrid.ThingsListAt(cell))
                {
                    Pawn pawn = thing as Pawn;
                    if (!IsLivingFlesh(pawn) || (owner != null && !owner.HostileTo(pawn)) || !affected.Add(pawn)) continue;
                    ApplyTimedHediff(pawn, SeedSlowHediff);
                }
            }
        }

        internal static void ApplyHeartbreakerShieldHit(CompShield shield, DamageInfo dinfo, ref float energy)
        {
            if (shield == null || dinfo.Weapon?.defName != HeartbreakerCannonDefName) return;
            float extraLoss = Mathf.Max(0f, dinfo.Amount) * shield.Props.energyLossPerDamage * 1.5f;
            if (extraLoss > 0f)
                energy = Mathf.Max(0f, energy - extraLoss);
            ApplyTimedHediff(ShieldWearer(shield), ShieldDisruptionHediff);
            if (energy <= 0.0001f)
                ShieldBreakMethod?.Invoke(shield, null);
        }

        public static Pawn ShieldWearer(CompShield shield)
        {
            return (shield?.parent as Apparel)?.Wearer;
        }

        public static bool IsShieldRecoveryDisrupted(Pawn pawn)
        {
            return pawn?.health?.hediffSet?.hediffs.Any(hediff =>
                hediff.def?.defName == ShieldDisruptionHediff) == true;
        }
    }

    public class MapComponent_BlightSeederPollution : MapComponent
    {
        private const int MaxSeededPollutionCellsPerMap = 60;
        private List<IntVec3> seededPollutionCells = new List<IntVec3>();

        public MapComponent_BlightSeederPollution(Map map) : base(map)
        {
        }

        public bool CanAddSeededPollution => seededPollutionCells.Count < MaxSeededPollutionCellsPerMap;

        public void RecordSeededPollution(IntVec3 cell)
        {
            if (!seededPollutionCells.Contains(cell) && seededPollutionCells.Count < MaxSeededPollutionCellsPerMap)
                seededPollutionCells.Add(cell);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref seededPollutionCells, "blightSeederPollutionCells", LookMode.Value);
            if (seededPollutionCells == null)
                seededPollutionCells = new List<IntVec3>();
        }
    }

    public class ThingComp_BlightSeed : ThingComp
    {
        private Pawn owner;
        private int ticksAlive;
        private int ticksToPollute = 150;
        private int ticksToDisrupt = 300;
        private int pollutedCells;
        private int createdAtTick = -1;

        public Pawn Owner => owner;
        public int CreatedAtTick => createdAtTick;

        public void SetOwner(Pawn pawn)
        {
            owner = pawn;
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (createdAtTick < 0 && Find.TickManager != null)
                createdAtTick = Find.TickManager.TicksGame;
        }

        public override void CompTickInterval(int interval)
        {
            base.CompTickInterval(interval);
            if (parent?.Spawned != true) return;
            ticksAlive += interval;
            ticksToPollute -= interval;
            ticksToDisrupt -= interval;

            while (ticksToPollute <= 0 && pollutedCells < 12)
            {
                TryPolluteOneCell();
                ticksToPollute += 150;
            }
            if (ticksToDisrupt <= 0)
            {
                AdvancedBlightWeaponUtility.DisruptNearbyEnemies(parent.Map, parent.Position, owner);
                ticksToDisrupt = 300;
            }
            if (ticksAlive >= 1800)
                parent.Destroy(DestroyMode.Vanish);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_References.Look(ref owner, "blightSeedOwner");
            Scribe_Values.Look(ref ticksAlive, "blightSeedTicksAlive", 0);
            Scribe_Values.Look(ref ticksToPollute, "blightSeedTicksToPollute", 150);
            Scribe_Values.Look(ref ticksToDisrupt, "blightSeedTicksToDisrupt", 300);
            Scribe_Values.Look(ref pollutedCells, "blightSeedPollutedCells", 0);
            Scribe_Values.Look(ref createdAtTick, "blightSeedCreatedAtTick", -1);
        }

        private void TryPolluteOneCell()
        {
            Map map = parent?.Map;
            MapComponent_BlightSeederPollution budget = map?.GetComponent<MapComponent_BlightSeederPollution>();
            if (map?.pollutionGrid == null || budget == null || !budget.CanAddSeededPollution || pollutedCells >= 12) return;
            List<IntVec3> candidates = GenRadial.RadialCellsAround(parent.Position, 4f, true)
                .Where(cell => cell.InBounds(map) && !map.pollutionGrid.IsPolluted(cell) &&
                    map.pollutionGrid.CanPollute(cell))
                .ToList();
            if (candidates.Count == 0) return;
            IntVec3 cellToPollute = candidates.RandomElement();
            map.pollutionGrid.SetPolluted(cellToPollute, true, false);
            budget.RecordSeededPollution(cellToPollute);
            pollutedCells++;
        }
    }

    [HarmonyPatch(typeof(Thing), nameof(Thing.TakeDamage))]
    public static class SymbioticBladeLifeSiphonPatch
    {
        public static void Postfix(Thing __instance, DamageInfo dinfo, DamageWorker.DamageResult __result)
        {
            if (dinfo.Weapon?.defName == AdvancedBlightWeaponUtility.SymbioticBladeDefName)
                AdvancedBlightWeaponUtility.OnBladeDamage(__instance, dinfo, __result.totalDamageDealt);
        }
    }

    [HarmonyPatch(typeof(CompShield), nameof(CompShield.PostPreApplyDamage))]
    public static class HeartbreakerShieldDisruptionPatch
    {
        public static void Postfix(CompShield __instance, ref DamageInfo dinfo, bool absorbed, ref float ___energy)
        {
            if (!absorbed || dinfo.Weapon?.defName != AdvancedBlightWeaponUtility.HeartbreakerCannonDefName) return;
            AdvancedBlightWeaponUtility.ApplyHeartbreakerShieldHit(__instance, dinfo, ref ___energy);
        }
    }

    [HarmonyPatch(typeof(CompShield), "get_EnergyGainPerTick")]
    public static class HeartbreakerShieldRecoveryPatch
    {
        public static void Postfix(CompShield __instance, ref float __result)
        {
            if (AdvancedBlightWeaponUtility.IsShieldRecoveryDisrupted(AdvancedBlightWeaponUtility.ShieldWearer(__instance)))
                __result = 0f;
        }
    }
}
