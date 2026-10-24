using System;
using System.Collections.Generic;
using System.Linq;
using LiAIChat.AlliedSettlementSurvival;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse;

namespace LiAIChat.Questing
{
    public enum AlliedSettlementAidType
    {
        None,
        Food,
        Medicine,
        Purification
    }

    public class QuestNode_CreateAlliedSettlementAid : QuestNode
    {
        protected override bool TestRunInt(Slate slate) => slate.Get<Settlement>("alliedSettlement") != null &&
            slate.Get<AlliedSettlementCrisisKind>("alliedCrisisKind") != AlliedSettlementCrisisKind.None;

        protected override void RunInt()
        {
            Settlement settlement = QuestGen.slate.Get<Settlement>("alliedSettlement");
            AlliedSettlementCrisisKind crisisKind =
                QuestGen.slate.Get<AlliedSettlementCrisisKind>("alliedCrisisKind");
            AlliedSettlementAidType aidType = QuestGen.slate.Get("alliedAidType", AlliedSettlementAidType.None);
            if (aidType == AlliedSettlementAidType.None)
                aidType = crisisKind == AlliedSettlementCrisisKind.DiseaseOutbreak
                    ? AlliedSettlementAidType.Medicine : AlliedSettlementAidType.Food;
            float strength = AlliedSettlementWorldComponent.Current?.Get(settlement)?.strength ?? 0f;
            QuestGen.quest.AddPart(new QuestPart_AlliedSettlementAid
            {
                settlement = settlement,
                crisisKind = crisisKind,
                aidType = aidType,
                requiredNutrition = aidType == AlliedSettlementAidType.Food
                    ? AlliedSettlementAidQuestUtility.RequiredFoodNutrition(strength) : 0f,
                requiredMedicine = aidType == AlliedSettlementAidType.Medicine
                    ? AlliedSettlementAidQuestUtility.RequiredMedicineUnits(strength) : 0,
                requiredPurificationSupplies = aidType == AlliedSettlementAidType.Purification
                    ? AlliedSettlementAidQuestUtility.RequiredPurificationSupplies(strength) : 0
            });
        }
    }

    public class QuestPart_AlliedSettlementAid : QuestPart
    {
        public Settlement settlement;
        public AlliedSettlementCrisisKind crisisKind;
        public AlliedSettlementAidType aidType;
        public float requiredNutrition;
        public int requiredMedicine;
        public int requiredPurificationSupplies;
        public float deliveredNutrition;
        public int deliveredMedicine;
        public int deliveredPurificationSupplies;

        public float RemainingNutrition => Math.Max(0f, requiredNutrition - deliveredNutrition);
        public int RemainingMedicine => Math.Max(0, requiredMedicine - deliveredMedicine);

        public override string DescriptionPart
        {
            get
            {
                if (quest == null || quest.State != QuestState.Ongoing) return null;
                if (aidType == AlliedSettlementAidType.Food)
                    return "LiASS_AidFoodProgress".Translate(deliveredNutrition.ToString("0.#"), requiredNutrition.ToString("0.#"));
                if (aidType == AlliedSettlementAidType.Medicine)
                    return "LiASS_AidMedicineProgress".Translate(deliveredMedicine, requiredMedicine);
                if (aidType == AlliedSettlementAidType.Purification)
                    return "LiASS_AidPurificationProgress".Translate(deliveredPurificationSupplies,
                        requiredPurificationSupplies);
                return null;
            }
        }

        public override IEnumerable<GlobalTargetInfo> QuestLookTargets
        {
            get
            {
                if (settlement != null) yield return new GlobalTargetInfo(settlement);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref settlement, "settlement");
            Scribe_Values.Look(ref crisisKind, "crisisKind", AlliedSettlementCrisisKind.None);
            Scribe_Values.Look(ref aidType, "aidType", AlliedSettlementAidType.None);
            Scribe_Values.Look(ref requiredNutrition, "requiredNutrition");
            Scribe_Values.Look(ref requiredMedicine, "requiredMedicine");
            Scribe_Values.Look(ref requiredPurificationSupplies, "requiredPurificationSupplies");
            Scribe_Values.Look(ref deliveredNutrition, "deliveredNutrition");
            Scribe_Values.Look(ref deliveredMedicine, "deliveredMedicine");
            Scribe_Values.Look(ref deliveredPurificationSupplies, "deliveredPurificationSupplies");
            if (Scribe.mode == LoadSaveMode.PostLoadInit && aidType == AlliedSettlementAidType.None)
                aidType = crisisKind == AlliedSettlementCrisisKind.DiseaseOutbreak
                    ? AlliedSettlementAidType.Medicine : AlliedSettlementAidType.Food;
        }
    }

    public static class AlliedSettlementAidQuestUtility
    {
        public static float RequiredFoodNutrition(float strength) => SettlementStrengthPolicy.ScaleFoodAidRequirement(
            AlliedSettlementSurvivalMod.Current.foodNutrition, strength);
        public static int RequiredMedicineUnits(float strength) => SettlementStrengthPolicy.ScaleMedicineAidRequirement(
            AlliedSettlementSurvivalMod.Current.medicineUnits, strength);
        public static int RequiredPurificationSupplies(float strength) => (int)Math.Ceiling(
            6f * SettlementStrengthPolicy.AidRequirementMultiplier(strength));

        public static bool TryCreate(AlliedSettlementWorldComponent component, AlliedSettlementState state,
            AlliedSettlementCrisisKind kind, out Quest quest)
        {
            quest = null;
            if (kind != AlliedSettlementCrisisKind.FoodShortage &&
                kind != AlliedSettlementCrisisKind.DiseaseOutbreak &&
                kind != AlliedSettlementCrisisKind.BlightfallEnvironmental)
                return false;

            AlliedSettlementAidType aidType = kind == AlliedSettlementCrisisKind.BlightfallEnvironmental
                ? (AlliedSettlementAidType)Rand.RangeInclusive((int)AlliedSettlementAidType.Food,
                    (int)AlliedSettlementAidType.Purification)
                : kind == AlliedSettlementCrisisKind.FoodShortage
                    ? AlliedSettlementAidType.Food : AlliedSettlementAidType.Medicine;
            string defName = kind == AlliedSettlementCrisisKind.BlightfallEnvironmental
                ? "LiAIChat_AlliedSettlementBlightfallAid"
                : aidType == AlliedSettlementAidType.Food
                    ? "LiAIChat_AlliedSettlementFoodAid" : "LiAIChat_AlliedSettlementMedicineAid";
            QuestScriptDef def = DefDatabase<QuestScriptDef>.GetNamedSilentFail(defName);
            if (def == null || Find.QuestManager == null)
            {
                Log.ErrorOnce("[LiAIChat] Allied settlement aid quest definition is missing: " + defName,
                    defName.GetHashCode());
                return false;
            }

            try
            {
                var slate = new Slate();
                slate.Set("alliedSettlement", state.settlement);
                slate.Set("alliedCrisisKind", kind);
                slate.Set("alliedAidType", aidType);
                quest = QuestGen.Generate(def, slate);
                if (quest == null) return false;
                quest.SetInitiallyAccepted();
                Find.QuestManager.Add(quest);
                state.aidQuest = quest;
                return true;
            }
            catch (Exception error)
            {
                quest = null;
                Log.Error("[LiAIChat] Could not create allied settlement aid quest: " + error);
                return false;
            }
        }

        public static QuestPart_AlliedSettlementAid PartFor(AlliedSettlementState state)
        {
            if (state == null || !state.crisisActive || state.aidQuest == null || state.aidQuest.State != QuestState.Ongoing)
                return null;
            return state.aidQuest.PartsListForReading.OfType<QuestPart_AlliedSettlementAid>().FirstOrDefault();
        }

        public static void AddCaravanGizmo(Caravan caravan, Settlement settlement, AlliedSettlementState state,
            List<Gizmo> gizmos)
        {
            QuestPart_AlliedSettlementAid part = PartFor(state);
            if (part == null || !caravan.IsPlayerControlled || !caravan.Spawned || caravan.Tile != settlement.Tile) return;
            if (part.aidType == AlliedSettlementAidType.None) return;
            string label = part.aidType == AlliedSettlementAidType.Food ? "LiASS_DeliverFood" :
                part.aidType == AlliedSettlementAidType.Medicine ? "LiASS_DeliverMedicine" :
                "LiASS_DeliverPurificationSupplies";
            string description = part.aidType == AlliedSettlementAidType.Food
                ? "LiASS_DeliverFoodDescription".Translate(part.RemainingNutrition.ToString("0.#"))
                : part.aidType == AlliedSettlementAidType.Medicine
                    ? "LiASS_DeliverMedicineDescription".Translate(part.RemainingMedicine)
                    : "LiASS_DeliverPurificationSuppliesDescription".Translate(
                        Math.Max(0, part.requiredPurificationSupplies - part.deliveredPurificationSupplies));
            gizmos.Add(new Command_Action
            {
                defaultLabel = label.Translate(),
                defaultDesc = description,
                icon = UI.LiAIChatTextures.PermanentMigration,
                action = () => OpenDeliveryMenu(caravan, settlement)
            });
        }

        private static void OpenDeliveryMenu(Caravan caravan, Settlement settlement)
        {
            AlliedSettlementWorldComponent component = AlliedSettlementWorldComponent.Current;
            if (component == null || caravan == null || settlement == null ||
                !caravan.Spawned || !caravan.IsPlayerControlled || caravan.Tile != settlement.Tile)
            {
                Messages.Message("LiASS_DeliveryUnavailable".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            AlliedSettlementState state = component.Get(settlement);
            QuestPart_AlliedSettlementAid part = PartFor(state);
            if (part == null)
            {
                Messages.Message("LiASS_DeliveryUnavailable".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            var options = new List<FloatMenuOption>();
            List<Thing> inventory = AvailableDeliveryItems(caravan);
            if (part.aidType == AlliedSettlementAidType.Food)
            {
                float remaining = part.RemainingNutrition;
                foreach (ThingDef def in inventory.Where(t => t.def.IsNutritionGivingIngestible &&
                    t.def.ingestible.HumanEdible && t.IngestibleNow)
                    .Select(t => t.def).Distinct().OrderBy(d => d.LabelCap.ToString(), StringComparer.CurrentCulture))
                {
                    int available = inventory.Where(t => t.def == def).Sum(t => t.stackCount);
                    float nutrition = def.GetStatValueAbstract(StatDefOf.Nutrition);
                    if (available <= 0 || nutrition <= 0f || remaining <= 0f) continue;
                    int count = Math.Min(available, (int)Math.Ceiling(remaining / nutrition));
                    ThingDef selected = def;
                    options.Add(new FloatMenuOption("LiASS_FoodOption".Translate(selected.LabelCap, count,
                        (count * nutrition).ToString("0.#"), available), () => ConfirmDelivery(caravan, settlement, part,
                            selected, count, "LiASS_ConfirmFood".Translate(count, (count * nutrition).ToString("0.#"), settlement.LabelCap))));
                }
            }
            else if (part.aidType == AlliedSettlementAidType.Medicine)
            {
                int remaining = part.RemainingMedicine;
                foreach (ThingDef def in inventory.Where(t => t.def.IsMedicine).Select(t => t.def)
                    .Distinct().OrderBy(d => d.LabelCap.ToString(), StringComparer.CurrentCulture))
                {
                    int available = inventory.Where(t => t.def == def).Sum(t => t.stackCount);
                    if (available <= 0 || remaining <= 0) continue;
                    int count = Math.Min(available, remaining);
                    ThingDef selected = def;
                    options.Add(new FloatMenuOption("LiASS_MedicineOption".Translate(selected.LabelCap, count, available),
                        () => ConfirmDelivery(caravan, settlement, part, selected, count,
                            "LiASS_ConfirmMedicine".Translate(count, settlement.LabelCap))));
                }
            }
            else if (part.aidType == AlliedSettlementAidType.Purification)
            {
                int remaining = Math.Max(0, part.requiredPurificationSupplies - part.deliveredPurificationSupplies);
                ThingDef def = ThingDefOf.ComponentIndustrial;
                int available = inventory.Where(thing => thing.def == def).Sum(thing => thing.stackCount);
                int count = Math.Min(available, remaining);
                if (count > 0)
                {
                    options.Add(new FloatMenuOption("LiASS_PurificationSupplyOption".Translate(
                        def.LabelCap, count, available),
                        () => ConfirmDelivery(caravan, settlement, part, def, count,
                            "LiASS_ConfirmPurificationSupplies".Translate(count, settlement.LabelCap))));
                }
            }
            if (options.Count == 0) options.Add(new FloatMenuOption("LiASS_NoAidItems".Translate(), null));
            Find.WindowStack.Add(new FloatMenu(options));
        }

        private static void ConfirmDelivery(Caravan caravan, Settlement settlement, QuestPart_AlliedSettlementAid part,
            ThingDef def, int count, TaggedString confirmation)
        {
            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(confirmation,
                () => Deliver(caravan, settlement, part, def, count), true));
        }

        private static void Deliver(Caravan caravan, Settlement settlement, QuestPart_AlliedSettlementAid part,
            ThingDef def, int requestedCount)
        {
            AlliedSettlementWorldComponent component = AlliedSettlementWorldComponent.Current;
            if (component == null || caravan == null || requestedCount <= 0 || !caravan.Spawned || !caravan.IsPlayerControlled ||
                settlement == null || caravan.Tile != settlement.Tile || !component.Eligible(settlement) ||
                part.quest == null || part.quest.State != QuestState.Ongoing ||
                component.Get(settlement)?.aidQuest != part.quest)
            {
                Messages.Message("LiASS_DeliveryUnavailable".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            bool food = part.aidType == AlliedSettlementAidType.Food;
            bool medicine = part.aidType == AlliedSettlementAidType.Medicine;
            bool purification = part.aidType == AlliedSettlementAidType.Purification;
            if ((food && (!def.IsNutritionGivingIngestible || !def.ingestible.HumanEdible || part.RemainingNutrition <= 0f)) ||
                (medicine && (!def.IsMedicine || part.RemainingMedicine <= 0)) ||
                (purification && (def != ThingDefOf.ComponentIndustrial ||
                    part.requiredPurificationSupplies <= part.deliveredPurificationSupplies)) ||
                (!food && !medicine && !purification))
            {
                Messages.Message("LiASS_DeliveryUnavailable".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            int unitsLeft = requestedCount;
            List<Thing> taken = TakeDeliveryItems(caravan, thing =>
            {
                if (unitsLeft <= 0 || thing.def != def || (food && !thing.IngestibleNow)) return 0;
                int units = Math.Min(unitsLeft, thing.stackCount);
                unitsLeft -= units;
                return units;
            });
            if (taken.Count == 0)
            {
                Messages.Message("LiASS_DeliveryUnavailable".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            float nutritionDelivered = food
                ? taken.Sum(thing => thing.def.GetStatValueAbstract(StatDefOf.Nutrition) * thing.stackCount)
                : 0f;
            int medicineDelivered = medicine ? taken.Sum(thing => thing.stackCount) : 0;
            int suppliesDelivered = purification ? taken.Sum(thing => thing.stackCount) : 0;
            foreach (Thing thing in taken) thing.Destroy(DestroyMode.Vanish);

            if (food) part.deliveredNutrition = Math.Min(part.requiredNutrition, part.deliveredNutrition + nutritionDelivered);
            else if (medicine) part.deliveredMedicine = Math.Min(part.requiredMedicine, part.deliveredMedicine + medicineDelivered);
            else part.deliveredPurificationSupplies = Math.Min(part.requiredPurificationSupplies,
                part.deliveredPurificationSupplies + suppliesDelivered);
            caravan.RecacheInventory();
            Messages.Message("LiASS_AidDelivered".Translate(def.LabelCap,
                food ? nutritionDelivered.ToString("0.#") : medicine ? medicineDelivered.ToString() : suppliesDelivered.ToString(),
                settlement.LabelCap),
                settlement, MessageTypeDefOf.PositiveEvent);

            bool complete = food ? part.RemainingNutrition <= 0.001f :
                medicine ? part.RemainingMedicine <= 0 :
                    part.deliveredPurificationSupplies >= part.requiredPurificationSupplies;
            if (complete) component.CompleteCrisis(settlement, part.crisisKind);
        }

        private static List<Thing> AvailableDeliveryItems(Caravan caravan)
        {
            var items = new List<Thing>(CaravanInventoryUtility.AllInventoryItems(caravan));
            var seen = new HashSet<Thing>(items);
            foreach (ThingOwner cargo in ShuttleCargoOwners(caravan))
            {
                foreach (Thing thing in cargo)
                    if (thing != null && seen.Add(thing))
                        items.Add(thing);
            }
            return items;
        }

        private static List<Thing> TakeDeliveryItems(Caravan caravan, Func<Thing, int> countToTake)
        {
            List<Thing> taken = CaravanInventoryUtility.TakeThings(caravan, countToTake);
            foreach (ThingOwner cargo in ShuttleCargoOwners(caravan))
            {
                if (cargo == null) continue;
                foreach (Thing thing in cargo.ToList())
                {
                    int count = countToTake(thing);
                    if (count <= 0) continue;
                    Thing removed = cargo.Take(thing, Math.Min(count, thing.stackCount));
                    if (removed != null) taken.Add(removed);
                }
            }
            return taken;
        }

        private static IEnumerable<ThingOwner> ShuttleCargoOwners(Caravan caravan)
        {
            if (caravan == null) yield break;

            // The shuttle is not always returned by AllInventoryItems after landing.
            // Find it through the caravan API so cargo remains visible on repeat visits.
            Building_PassengerShuttle shuttle = CaravanInventoryUtility.FindShuttle(caravan);
            if (shuttle?.TransporterComp != null)
                yield return shuttle.TransporterComp.GetDirectlyHeldThings();
        }
    }
}
