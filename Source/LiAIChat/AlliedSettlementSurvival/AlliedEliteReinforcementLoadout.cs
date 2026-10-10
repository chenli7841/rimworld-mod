using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace LiAIChat.AlliedSettlementSurvival
{
    public static class AlliedEliteReinforcementLoadout
    {
        private static readonly string[] StandardWeaponDefNames =
        {
            "Gun_AssaultRifle",
            "Gun_SniperRifle",
            "Gun_DoomsdayRocket",
            "Gun_ChargeRifle",
            "Gun_ChargeLance",
            "MeleeWeapon_MonoSword",
            "MeleeWeapon_Zeushammer"
        };

        private static List<ThingDef> specializedRangedWeapons;

        public static void EquipGroup(IEnumerable<Pawn> pawns)
        {
            List<Pawn> guards = pawns == null
                ? new List<Pawn>()
                : pawns.Where(IsEquipableGuard).Distinct().ToList();
            if (guards.Count == 0) return;

            ThingDef armorDef = DefDatabase<ThingDef>.GetNamedSilentFail("Apparel_PowerArmor");
            ThingDef helmetDef = DefDatabase<ThingDef>.GetNamedSilentFail("Apparel_PowerArmorHelmet");
            if (armorDef == null || helmetDef == null)
            {
                Log.ErrorOnce("[LiAIChat] Could not equip top-tier allied reinforcements: power armor definitions are missing.",
                    "LiAIChat_TopTierAlliedArmorMissing".GetHashCode());
                return;
            }

            foreach (Pawn pawn in guards)
                EquipArmor(pawn, armorDef, helmetDef);

            List<Pawn> rangedCapableGuards = guards.Where(CanUseRangedWeapons).ToList();
            HashSet<Pawn> assigned = new HashSet<Pawn>();
            ThingDef tripleRocket = DefDatabase<ThingDef>.GetNamedSilentFail("Gun_TripleRocket");
            if (tripleRocket != null && tripleRocket.IsRangedWeapon && rangedCapableGuards.Count > 0)
            {
                int rocketCount = Math.Min(rangedCapableGuards.Count, Rand.RangeInclusive(1, 2));
                for (int i = 0; i < rocketCount; i++)
                {
                    Pawn bearer = rangedCapableGuards.RandomElement();
                    rangedCapableGuards.Remove(bearer);
                    EquipWeapon(bearer, tripleRocket);
                    assigned.Add(bearer);
                }
            }

            List<ThingDef> specializedWeapons = SpecializedRangedWeapons();
            if (specializedWeapons.Count > 0 && rangedCapableGuards.Count > 0)
            {
                Pawn bearer = rangedCapableGuards.RandomElement();
                rangedCapableGuards.Remove(bearer);
                EquipWeapon(bearer, specializedWeapons.RandomElement());
                assigned.Add(bearer);
            }

            foreach (Pawn pawn in guards)
            {
                if (assigned.Contains(pawn)) continue;
                EquipWeapon(pawn, ChooseRegularWeapon(CanUseRangedWeapons(pawn)));
            }
        }

        private static void EquipArmor(Pawn pawn, ThingDef armorDef, ThingDef helmetDef)
        {
            try
            {
                pawn.apparel.DestroyAll(DestroyMode.Vanish);
                Apparel armor = ThingMaker.MakeThing(armorDef) as Apparel;
                Apparel helmet = ThingMaker.MakeThing(helmetDef) as Apparel;
                if (armor == null || helmet == null)
                {
                    armor?.Destroy(DestroyMode.Vanish);
                    helmet?.Destroy(DestroyMode.Vanish);
                    return;
                }
                pawn.apparel.Wear(armor, false, true);
                pawn.apparel.Wear(helmet, false, true);
            }
            catch (Exception error)
            {
                Log.Error("[LiAIChat] Could not equip power armor on a top-tier allied reinforcement: " + error);
            }
        }

        private static void EquipWeapon(Pawn pawn, ThingDef weaponDef)
        {
            if (pawn?.equipment == null || weaponDef == null) return;
            try
            {
                pawn.equipment.DestroyAllEquipment(DestroyMode.Vanish);
                ThingWithComps weapon = ThingMaker.MakeThing(weaponDef) as ThingWithComps;
                if (weapon != null)
                    pawn.equipment.AddEquipment(weapon);
            }
            catch (Exception error)
            {
                Log.Error("[LiAIChat] Could not equip a top-tier allied reinforcement weapon: " + error);
            }
        }

        public static void RepairIncompatibleRangedWeapon(Pawn pawn)
        {
            if (pawn == null || pawn.Dead || pawn.equipment == null ||
                pawn.equipment.Primary == null || !pawn.equipment.Primary.def.IsRangedWeapon ||
                StatDefOf.ShootingAccuracyPawn == null ||
                !StatDefOf.ShootingAccuracyPawn.Worker.IsDisabledFor(pawn))
                return;

            ThingDef weaponDef = ChooseRegularWeapon(false);
            if (weaponDef == null) return;

            try
            {
                pawn.equipment.DestroyAllEquipment(DestroyMode.Vanish);
                ThingWithComps weapon = ThingMaker.MakeThing(weaponDef) as ThingWithComps;
                if (weapon != null)
                    pawn.equipment.AddEquipment(weapon);
            }
            catch (Exception error)
            {
                Log.Error("[LiAIChat] Could not replace an incompatible allied guard weapon: " + error);
            }
        }

        private static bool IsEquipableGuard(Pawn pawn) => pawn != null && !pawn.Dead &&
            pawn.RaceProps.Humanlike && pawn.apparel != null && pawn.equipment != null;

        private static bool CanUseRangedWeapons(Pawn pawn) => StatDefOf.ShootingAccuracyPawn == null ||
            !StatDefOf.ShootingAccuracyPawn.Worker.IsDisabledFor(pawn);

        private static ThingDef ChooseRegularWeapon(bool allowRangedWeapons)
        {
            List<ThingDef> candidates = new List<ThingDef>();
            foreach (string defName in StandardWeaponDefNames)
            {
                ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
                if (def != null && def.IsWeapon && (allowRangedWeapons || def.IsMeleeWeapon) &&
                    !candidates.Contains(def))
                    candidates.Add(def);
            }

            if (!allowRangedWeapons && candidates.Count == 0)
            {
                ThingDef fallback = DefDatabase<ThingDef>.GetNamedSilentFail("MeleeWeapon_LongSword");
                if (fallback != null && fallback.IsWeapon)
                    candidates.Add(fallback);
            }

            return candidates.RandomElementWithFallback();
        }

        private static List<ThingDef> SpecializedRangedWeapons()
        {
            if (specializedRangedWeapons == null)
            {
                specializedRangedWeapons = DefDatabase<ThingDef>.AllDefsListForReading
                    .Where(def => def != null && def.IsRangedWeapon && def.defName != null &&
                        IsSpecializedRangedWeapon(def.defName))
                    .ToList();
            }
            return specializedRangedWeapons;
        }

        private static bool IsSpecializedRangedWeapon(string defName)
        {
            string name = defName.ToLowerInvariant();
            return name.Contains("unique") || name.Contains("laser") || name.Contains("plasma") ||
                name.Contains("beam") || name.Contains("energy") || name.Contains("ion") ||
                name.Contains("charge");
        }
    }
}
