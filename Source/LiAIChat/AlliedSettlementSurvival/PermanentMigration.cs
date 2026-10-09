using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace LiAIChat.AlliedSettlementSurvival
{
    public static class PermanentMigration
    {
        public static bool CanLead(Caravan caravan, Pawn pawn) =>
            caravan.IsOwner(pawn) && pawn.IsFreeNonSlaveColonist && !pawn.DeadOrDowned &&
            !pawn.InMentalState && pawn.ageTracker.Adult && !Temporary(pawn);

        private static bool Temporary(Pawn pawn) =>
            QuestUtility.IsQuestLodger(pawn) || QuestUtility.IsQuestHelper(pawn) ||
            QuestUtility.IsBorrowedByAnyFaction(pawn) || QuestUtility.IsReservedByQuestOrQuestBeingGenerated(pawn);

        public static string Rejection(Caravan caravan, Settlement settlement, Pawn pawn)
        {
            AlliedSettlementWorldComponent component = AlliedSettlementWorldComponent.Current;
            if (component == null || caravan == null || !caravan.Spawned || !caravan.IsPlayerControlled ||
                settlement == null || caravan.Tile != settlement.Tile || !component.Eligible(settlement) || settlement.HasMap)
                return "LiASS_Unavailable";
            AlliedSettlementState state = component.Get(settlement);
            if (state == null || state.strength <= 0f || state.strength >= 100f) return "LiASS_NoCapacity";
            bool freeColonist = pawn != null && pawn.IsFreeNonSlaveColonist;
            bool playerSlave = pawn != null && pawn.IsSlaveOfColony;
            if (pawn == null || !caravan.ContainsPawn(pawn) || (!freeColonist && !playerSlave) ||
                pawn.Faction != Faction.OfPlayer || !pawn.RaceProps.Humanlike || !pawn.ageTracker.Adult ||
                pawn.DeadOrDowned || pawn.InMentalState || pawn.IsPrisoner)
                return "LiASS_Ineligible";
            if (Temporary(pawn)) return "LiASS_Temporary";
            if (component.HasMigrated(pawn)) return "LiASS_AlreadyMigrated";
            if (!caravan.PawnsListForReading.Any(other => other != pawn && CanLead(caravan, other)))
                return "LiASS_LastLeader";
            return null;
        }

        public static void OpenMenu(Caravan caravan, Settlement settlement)
        {
            AlliedSettlementWorldComponent.Current?.Refresh();
            var options = new List<FloatMenuOption>();
            foreach (Pawn pawn in caravan.PawnsListForReading.Where(p => p.RaceProps.Humanlike).ToList())
            {
                Pawn candidate = pawn;
                string reason = Rejection(caravan, settlement, candidate);
                if (reason != null)
                {
                    options.Add(new FloatMenuOption(candidate.LabelShortCap + " — " + reason.Translate(), null));
                    continue;
                }
                AlliedSettlementState state = AlliedSettlementWorldComponent.Current.Get(settlement);
                float gain = SettlementStrengthPolicy.Clamp(state.strength + ContributionFor(candidate)) - state.strength;
                options.Add(new FloatMenuOption("LiASS_MigrantOption".Translate(candidate.LabelShortCap, gain.ToString("0.#")), () =>
                    Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                        "LiASS_Confirm".Translate(candidate.LabelShortCap, settlement.LabelCap, gain.ToString("0.#")),
                        () => Transfer(caravan, settlement, candidate), true))));
            }
            if (options.Count == 0) options.Add(new FloatMenuOption("LiASS_Ineligible".Translate(), null));
            Find.WindowStack.Add(new FloatMenu(options));
        }

        public static void Transfer(Caravan caravan, Settlement settlement, Pawn pawn)
        {
            AlliedSettlementWorldComponent component = AlliedSettlementWorldComponent.Current;
            component?.Refresh();
            // Revalidate after the confirmation dialog, including duplicate rewards and last owner.
            string reason = Rejection(caravan, settlement, pawn);
            if (reason != null)
            {
                Messages.Message(reason.Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            // Transfer only inventory. Worn apparel and equipped weapons remain on the migrant.
            // If a mod refuses an item, stop while the pawn and all items are still in the caravan.
            if (pawn.inventory != null)
            {
                foreach (Thing item in pawn.inventory.innerContainer.ToList())
                {
                    foreach (Pawn recipient in caravan.PawnsListForReading.Where(p => p != pawn && !p.Dead && p.inventory != null))
                    {
                        pawn.inventory.innerContainer.TryTransferToContainer(item, recipient.inventory.innerContainer, item.stackCount);
                        if (!pawn.inventory.innerContainer.Contains(item)) break;
                    }
                    if (pawn.inventory.innerContainer.Contains(item))
                    {
                        caravan.RecacheInventory();
                        Messages.Message("LiASS_InventoryFailed".Translate(), MessageTypeDefOf.RejectInput, false);
                        return;
                    }
                }
            }

            try
            {
                caravan.RemovePawn(pawn);
                pawn.SetFaction(settlement.Faction);
                if (!Find.WorldPawns.Contains(pawn)) Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.KeepForever);
                else Find.WorldPawns.ForcefullyKeptPawns.Add(pawn);
            }
            catch (Exception error)
            {
                // No reward is recorded until ownership and world storage both succeed.
                Log.Error("[LiAIChat] Permanent migration failed: " + error);
                if (pawn.Faction != Faction.OfPlayer) pawn.SetFaction(Faction.OfPlayer);
                if (!caravan.ContainsPawn(pawn)) caravan.AddPawn(pawn, true);
                caravan.RecacheInventory();
                Messages.Message("LiASS_TransferFailed".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            AlliedSettlementState state = component.Get(settlement);
            component.RecordMigration(pawn);
            state.strength = SettlementStrengthPolicy.Clamp(state.strength + ContributionFor(pawn));
            caravan.RecacheInventory();
            Messages.Message("LiASS_Success".Translate(pawn.LabelShortCap, settlement.LabelCap, state.strength.ToString("0.#")),
                settlement, MessageTypeDefOf.PositiveEvent);
        }

        public static float ContributionFor(Pawn pawn) =>
            pawn.IsSlaveOfColony ? SettlementStrengthPolicy.SlaveMigrantStrength : SettlementStrengthPolicy.MigrantStrength;
    }

    [HarmonyPatch(typeof(Caravan), nameof(Caravan.GetGizmos))]
    public static class CaravanPermanentMigrationPatch
    {
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Caravan __instance)
        {
            foreach (Gizmo gizmo in __result) yield return gizmo;
            if (!__instance.IsPlayerControlled) yield break;
            AlliedSettlementWorldComponent component = AlliedSettlementWorldComponent.Current;
            if (component == null) yield break;
            foreach (Settlement settlement in Find.WorldObjects.Settlements.Where(s => s.Tile == __instance.Tile && component.Eligible(s)))
            {
                Settlement target = settlement;
                yield return new Command_Action
                {
                    defaultLabel = "LiASS_Migrate".Translate(),
                    defaultDesc = "LiASS_MigrateDescription".Translate(target.LabelCap),
                    icon = LiAIChat.UI.LiAIChatTextures.PermanentMigration,
                    action = () => PermanentMigration.OpenMenu(__instance, target)
                };
            }
        }
    }
}
