using System;
using System.Linq;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace LiAIChat.Questing
{
    /// <summary>
    /// Finds a colonist who can turn their equipped masterpiece into an Odyssey
    /// unique weapon, then lets the quest part handle their permanent departure.
    /// </summary>
    public class QuestNode_PrepareFarewellWeaponGift : QuestNode
    {
        private static bool HasActiveInvitation()
        {
            return Find.QuestManager.QuestsListForReading.Any(q =>
                q.root != null && q.root.defName == "LiAIChat_FarewellWeaponGift" &&
                (q.State == QuestState.NotYetAccepted || q.State == QuestState.Ongoing));
        }

        internal static bool IsEligible(Pawn pawn, out ThingWithComps weapon, out ThingDef uniqueWeaponDef)
        {
            weapon = null;
            uniqueWeaponDef = null;
            if (pawn == null || !pawn.IsColonistPlayerControlled || pawn.Dead || pawn.Downed ||
                pawn.skills == null || pawn.equipment == null ||
                pawn.skills.GetSkill(SkillDefOf.Shooting).Level <= 12 ||
                pawn.skills.GetSkill(SkillDefOf.Crafting).Level <= 12)
                return false;

            weapon = pawn.equipment.Primary;
            if (weapon == null || !weapon.def.IsRangedWeapon)
                return false;

            CompQuality quality = weapon.TryGetComp<CompQuality>();
            if (quality == null || (quality.Quality != QualityCategory.Masterwork &&
                quality.Quality != QualityCategory.Legendary))
                return false;

            // A weapon already in Odyssey's unique-weapon form can be refined
            // directly. Ordinary supported weapons are rebuilt as their matching
            // <defName>_Unique form, which is how the DLC stores weapon traits.
            if (weapon.TryGetComp<CompUniqueWeapon>() != null)
            {
                uniqueWeaponDef = weapon.def;
                return true;
            }

            uniqueWeaponDef = DefDatabase<ThingDef>.GetNamedSilentFail(weapon.def.defName + "_Unique");
            return uniqueWeaponDef != null && uniqueWeaponDef.GetCompProperties<CompProperties_UniqueWeapon>() != null;
        }

        private static Pawn FindCandidate(Map map, out ThingWithComps weapon, out ThingDef uniqueWeaponDef)
        {
            weapon = null;
            uniqueWeaponDef = null;
            if (map == null)
                return null;

            foreach (Pawn pawn in map.mapPawns.FreeColonistsSpawned.InRandomOrder())
            {
                ThingWithComps foundWeapon;
                ThingDef foundUniqueDef;
                if (IsEligible(pawn, out foundWeapon, out foundUniqueDef))
                {
                    weapon = foundWeapon;
                    uniqueWeaponDef = foundUniqueDef;
                    return pawn;
                }
            }
            return null;
        }

        protected override bool TestRunInt(Slate slate)
        {
            ThingWithComps weapon;
            ThingDef uniqueWeaponDef;
            return !HasActiveInvitation() && FindCandidate(slate.Get<Map>("map"), out weapon, out uniqueWeaponDef) != null;
        }

        protected override void RunInt()
        {
            Map map = QuestGen.slate.Get<Map>("map");
            ThingWithComps weapon;
            ThingDef uniqueWeaponDef;
            Pawn craftsman = FindCandidate(map, out weapon, out uniqueWeaponDef);
            if (craftsman == null)
                throw new InvalidOperationException("Farewell weapon gift requires an eligible armed colonist.");

            Faction destination = Find.FactionManager.AllFactionsVisible
                .Where(f => f != Faction.OfPlayer && !f.def.hidden && !f.HostileTo(Faction.OfPlayer))
                .InRandomOrder().FirstOrDefault();
            if (destination == null)
                throw new InvalidOperationException("Farewell weapon gift requires a friendly visible faction.");

            QuestGen.quest.AddPart(new QuestPart_FarewellWeaponGift
            {
                map = map,
                craftsman = craftsman,
                sourceWeapon = weapon,
                uniqueWeaponDef = uniqueWeaponDef,
                destinationFaction = destination,
                inSignalEnable = QuestGen.slate.Get<string>("inSignal")
            });
        }
    }
}
