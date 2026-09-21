using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace LiAIChat.Questing
{
    public class QuestPart_FarewellWeaponGift : QuestPartActivable
    {
        private const int CraftingDurationTicks = 180000; // Three in-game days.

        public Map map;
        public Pawn craftsman;
        public ThingWithComps sourceWeapon;
        public ThingDef uniqueWeaponDef;
        public Faction destinationFaction;
        public ThingWithComps upgradedWeapon;
        public WeaponTraitDef appliedTrait;
        public int returnTick = -1;
        public bool departed;
        public bool delivered;

        public override bool RequiresAccepter => true;

        public override string DescriptionPart
        {
            get
            {
                string name = craftsman != null ? craftsman.LabelShort : "这位武器匠";
                string weapon = sourceWeapon != null ? sourceWeapon.LabelCap : "这件武器";
                if (!departed)
                    return name + " 将带着自己的 " + weapon + " 永久前往 " +
                        (destinationFaction?.Name ?? "友方势力") + "，以数日的专注改造作为告别礼物。";
                if (!delivered)
                    return name + " 正在 " + (destinationFaction?.Name ?? "友方势力") +
                        " 的工坊中改造武器。完成后，改造后的武器将被送回殖民地。";
                return "告别礼物已送达。";
            }
        }

        public override void PreQuestAccept()
        {
            base.PreQuestAccept();
            if (!PrepareDeparture())
            {
                Messages.Message("武器匠的告别礼物未能开始：殖民者或其武器已不再满足条件。", MessageTypeDefOf.NegativeEvent);
                quest.End(QuestEndOutcome.Fail, true);
            }
        }

        private bool PrepareDeparture()
        {
            ThingWithComps verifiedWeapon;
            ThingDef verifiedUniqueDef;
            if (!QuestNode_PrepareFarewellWeaponGift.IsEligible(craftsman, out verifiedWeapon, out verifiedUniqueDef) ||
                verifiedWeapon != sourceWeapon || verifiedUniqueDef != uniqueWeaponDef)
                return false;

            CompQuality sourceQuality = sourceWeapon.TryGetComp<CompQuality>();
            bool alreadyUnique = sourceWeapon.TryGetComp<CompUniqueWeapon>() != null;
            ThingWithComps result = alreadyUnique
                ? sourceWeapon
                : ThingMaker.MakeThing(uniqueWeaponDef) as ThingWithComps;
            CompUniqueWeapon unique = result?.TryGetComp<CompUniqueWeapon>();
            if (result == null || unique == null || sourceQuality == null)
                return false;

            CompQuality resultQuality = result.TryGetComp<CompQuality>();
            if (!alreadyUnique && resultQuality != null)
                resultQuality.SetQuality(sourceQuality.Quality, ArtGenerationContext.Colony);

            List<WeaponTraitDef> traits = DefDatabase<WeaponTraitDef>.AllDefsListForReading
                .Where(t => t.canGenerateAlone && unique.CanAddTrait(t)).ToList();
            if (traits.Count == 0)
                return false;

            appliedTrait = traits.RandomElement();
            unique.AddTrait(appliedTrait);
            upgradedWeapon = result;

            craftsman.equipment.Remove(sourceWeapon);
            if (!alreadyUnique)
                sourceWeapon.Destroy(DestroyMode.Vanish);
            craftsman.SetFaction(destinationFaction);
            craftsman.Destroy(DestroyMode.Vanish);
            departed = true;
            returnTick = Find.TickManager.TicksGame + CraftingDurationTicks;

            Messages.Message(craftsman.LabelShort + " 已前往 " + destinationFaction.Name +
                "，带走其 " + sourceWeapon.LabelCap + " 作为最后的改造作品。", MessageTypeDefOf.NeutralEvent);
            return true;
        }

        public override void QuestPartTick()
        {
            base.QuestPartTick();
            if (Find.TickManager.TicksGame % 250 != 0)
                return;
            if (quest.State != QuestState.Ongoing || !departed || delivered || returnTick < 0 ||
                Find.TickManager.TicksGame < returnTick)
                return;

            Map targetMap = map != null && map.IsPlayerHome ? map : Find.AnyPlayerHomeMap;
            if (targetMap == null || upgradedWeapon == null || upgradedWeapon.Destroyed)
            {
                quest.End(QuestEndOutcome.Fail, true);
                return;
            }

            DropPodUtility.DropThingsNear(DropCellFinder.TradeDropSpot(targetMap), targetMap,
                new List<Thing> { upgradedWeapon }, 110, false, false, true, false, false, destinationFaction);
            delivered = true;
            Find.LetterStack.ReceiveLetter("武器匠的告别礼物",
                "来自 " + (destinationFaction?.Name ?? "友方势力") + " 的空投送回了 " +
                (upgradedWeapon.LabelCap ?? "改造后的武器") + "。" +
                "武器获得特化属性：" + (appliedTrait?.LabelCap ?? "未知") + "。",
                LetterDefOf.PositiveEvent, upgradedWeapon);
            quest.End(QuestEndOutcome.Success, true);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref map, "map");
            Scribe_References.Look(ref craftsman, "craftsman");
            Scribe_References.Look(ref sourceWeapon, "sourceWeapon");
            Scribe_Defs.Look(ref uniqueWeaponDef, "uniqueWeaponDef");
            Scribe_References.Look(ref destinationFaction, "destinationFaction");
            Scribe_References.Look(ref upgradedWeapon, "upgradedWeapon");
            Scribe_Defs.Look(ref appliedTrait, "appliedTrait");
            Scribe_Values.Look(ref returnTick, "returnTick", -1);
            Scribe_Values.Look(ref departed, "departed", false);
            Scribe_Values.Look(ref delivered, "delivered", false);
        }
    }
}
