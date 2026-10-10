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
            "Gun_TripleRocket",
            "Gun_ChargeRifle",
            "Gun_ChargeLance",
            "MeleeWeapon_MonoSword",
            "MeleeWeapon_Zeushammer"
        };

        private static List<ThingDef> specializedRangedWeapons;

        public static void Equip(Pawn pawn)
        {
            if (pawn == null || pawn.Dead || !pawn.RaceProps.Humanlike ||
                pawn.apparel == null || pawn.equipment == null)
                return;

            ThingDef armorDef = DefDatabase<ThingDef>.GetNamedSilentFail("Apparel_PowerArmor");
            ThingDef helmetDef = DefDatabase<ThingDef>.GetNamedSilentFail("Apparel_PowerArmorHelmet");
            bool canUseRangedWeapon = StatDefOf.ShootingAccuracyPawn == null ||
                !StatDefOf.ShootingAccuracyPawn.Worker.IsDisabledFor(pawn);
            ThingDef weaponDef = ChooseWeapon(canUseRangedWeapon);
            if (armorDef == null || helmetDef == null)
            {
                Log.ErrorOnce("[LiAIChat] Could not equip top-tier allied reinforcements: power armor definitions are missing.",
                    "LiAIChat_TopTierAlliedArmorMissing".GetHashCode());
                return;
            }

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

                if (weaponDef == null) return;
                pawn.equipment.DestroyAllEquipment(DestroyMode.Vanish);
                ThingWithComps weapon = ThingMaker.MakeThing(weaponDef) as ThingWithComps;
                if (weapon != null)
                    pawn.equipment.AddEquipment(weapon);
            }
            catch (Exception error)
            {
                Log.Error("[LiAIChat] Could not fully equip a top-tier allied reinforcement: " + error);
            }
        }

        private static ThingDef ChooseWeapon(bool allowRangedWeapons)
        {
            List<ThingDef> candidates = new List<ThingDef>();
            foreach (string defName in StandardWeaponDefNames)
            {
                ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
                if (def != null && def.IsWeapon && (allowRangedWeapons || def.IsMeleeWeapon) &&
                    !candidates.Contains(def))
                    candidates.Add(def);
            }

            // DLCs and weapon mods may add unique/specialized ranged weapons. Include
            // only actual ranged weapon defs, so mech turrets and buildings are excluded.
            if (allowRangedWeapons && specializedRangedWeapons == null)
            {
                specializedRangedWeapons = DefDatabase<ThingDef>.AllDefsListForReading
                    .Where(def => def != null && def.IsRangedWeapon && def.defName != null &&
                        IsSpecializedRangedWeapon(def.defName))
                    .ToList();
            }
            if (allowRangedWeapons)
                foreach (ThingDef def in specializedRangedWeapons)
                    if (!candidates.Contains(def)) candidates.Add(def);

            return candidates.RandomElementWithFallback();
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
