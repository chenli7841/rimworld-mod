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
    public class QuestNode_CreateAlliedSettlementAid : QuestNode
    {
        protected override bool TestRunInt(Slate slate) => slate.Get<Settlement>("alliedSettlement") != null &&
            slate.Get<AlliedSettlementCrisisKind>("alliedCrisisKind") != AlliedSettlementCrisisKind.None;

        protected override void RunInt()
        {
            QuestGen.quest.AddPart(new QuestPart_AlliedSettlementAid
            {
                settlement = QuestGen.slate.Get<Settlement>("alliedSettlement"),
                crisisKind = QuestGen.slate.Get<AlliedSettlementCrisisKind>("alliedCrisisKind"),
                requiredNutrition = AlliedSettlementAidQuestUtility.RequiredFoodNutrition,
                requiredMedicine = AlliedSettlementAidQuestUtility.RequiredMedicineUnits
            });
        }
    }

    public class QuestPart_AlliedSettlementAid : QuestPart
    {
        public Settlement settlement;
        public AlliedSettlementCrisisKind crisisKind;
        public float requiredNutrition;
        public int requiredMedicine;
        public float deliveredNutrition;
        public int deliveredMedicine;

        public float RemainingNutrition => Math.Max(0f, requiredNutrition - deliveredNutrition);
        public int RemainingMedicine => Math.Max(0, requiredMedicine - deliveredMedicine);

        public override string DescriptionPart
        {
            get
            {
                if (quest == null || quest.State != QuestState.Ongoing) return null;
                if (crisisKind == AlliedSettlementCrisisKind.FoodShortage)
                    return "LiASS_AidFoodProgress".Translate(deliveredNutrition.ToString("0.#"), requiredNutrition.ToString("0.#"));
                if (crisisKind == AlliedSettlementCrisisKind.DiseaseOutbreak)
                    return "LiASS_AidMedicineProgress".Translate(deliveredMedicine, requiredMedicine);
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
            Scribe_Values.Look(ref requiredNutrition, "requiredNutrition");
            Scribe_Values.Look(ref requiredMedicine, "requiredMedicine");
            Scribe_Values.Look(ref deliveredNutrition, "deliveredNutrition");
            Scribe_Values.Look(ref deliveredMedicine, "deliveredMedicine");
        }
    }

    public static class AlliedSettlementAidQuestUtility
    {
        public static float RequiredFoodNutrition => AlliedSettlementSurvivalMod.Current.foodNutrition;
        public static int RequiredMedicineUnits => AlliedSettlementSurvivalMod.Current.medicineUnits;

        public static bool TryCreate(AlliedSettlementWorldComponent component, AlliedSettlementState state,
            AlliedSettlementCrisisKind kind, out Quest quest)
        {
            quest = null;
            string defName = kind == AlliedSettlementCrisisKind.FoodShortage
                ? "LiAIChat_AlliedSettlementFoodAid"
                : kind == AlliedSettlementCrisisKind.DiseaseOutbreak
                    ? "LiAIChat_AlliedSettlementMedicineAid"
                    : null;
            QuestScriptDef def = defName == null ? null : DefDatabase<QuestScriptDef>.GetNamedSilentFail(defName);
            if (def == null || Find.QuestManager == null)
            {
                Log.ErrorOnce("[LiAIChat] Allied settlement aid quest definition is missing: " + defName,
                    defName == null ? 0 : defName.GetHashCode());
                return false;
            }

            try
            {
                var slate = new Slate();
                slate.Set("alliedSettlement", state.settlement);
                slate.Set("alliedCrisisKind", kind);
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
            bool food = part.crisisKind == AlliedSettlementCrisisKind.FoodShortage;
            if (!food && part.crisisKind != AlliedSettlementCrisisKind.DiseaseOutbreak) return;

            string label = food ? "LiASS_DeliverFood" : "LiASS_DeliverMedicine";
            string description = food
                ? "LiASS_DeliverFoodDescription".Translate(part.RemainingNutrition.ToString("0.#"))
                : "LiASS_DeliverMedicineDescription".Translate(part.RemainingMedicine);
            gizmos.Add(new Command_Action
            {
                defaultLabel = label.Translate(),
                defaultDesc = description,
                icon = UI.LiAIChatTextures.PermanentMigration,
                action = () => OpenDeliveryMenu(caravan, settlement, part)
            });
        }

        private static void OpenDeliveryMenu(Caravan caravan, Settlement settlement, QuestPart_AlliedSettlementAid part)
        {
            var options = new List<FloatMenuOption>();
            List<Thing> inventory = CaravanInventoryUtility.AllInventoryItems(caravan);
            if (part.crisisKind == AlliedSettlementCrisisKind.FoodShortage)
            {
                float remaining = part.RemainingNutrition;
                foreach (ThingDef def in inventory.Where(t => t.def.IsNutritionGivingIngestible &&
                    t.def.ingestible.HumanEdible && t.IngestibleNow)
                    .Select(t => t.def).Distinct().OrderBy(d => d.LabelCap))
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
            else if (part.crisisKind == AlliedSettlementCrisisKind.DiseaseOutbreak)
            {
                int remaining = part.RemainingMedicine;
                foreach (ThingDef def in inventory.Where(t => t.def.IsMedicine).Select(t => t.def)
                    .Distinct().OrderBy(d => d.LabelCap))
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

            bool food = part.crisisKind == AlliedSettlementCrisisKind.FoodShortage;
            if ((food && (!def.IsNutritionGivingIngestible || !def.ingestible.HumanEdible || part.RemainingNutrition <= 0f)) ||
                (!food && (part.crisisKind != AlliedSettlementCrisisKind.DiseaseOutbreak || !def.IsMedicine || part.RemainingMedicine <= 0)))
            {
                Messages.Message("LiASS_DeliveryUnavailable".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            int unitsLeft = requestedCount;
            List<Thing> taken = CaravanInventoryUtility.TakeThings(caravan, thing =>
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
            int medicineDelivered = food ? 0 : taken.Sum(thing => thing.stackCount);
            foreach (Thing thing in taken) thing.Destroy(DestroyMode.Vanish);

            if (food) part.deliveredNutrition = Math.Min(part.requiredNutrition, part.deliveredNutrition + nutritionDelivered);
            else part.deliveredMedicine = Math.Min(part.requiredMedicine, part.deliveredMedicine + medicineDelivered);
            caravan.RecacheInventory();
            Messages.Message("LiASS_AidDelivered".Translate(def.LabelCap,
                food ? nutritionDelivered.ToString("0.#") : medicineDelivered.ToString(), settlement.LabelCap),
                settlement, MessageTypeDefOf.PositiveEvent);

            bool complete = part.crisisKind == AlliedSettlementCrisisKind.FoodShortage
                ? part.RemainingNutrition <= 0.001f
                : part.RemainingMedicine <= 0;
            if (complete) component.CompleteCrisis(settlement, part.crisisKind);
        }
    }
}
