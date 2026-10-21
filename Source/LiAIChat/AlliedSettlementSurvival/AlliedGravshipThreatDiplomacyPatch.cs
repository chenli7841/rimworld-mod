using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using LiAIChat.Questing;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace LiAIChat.AlliedSettlementSurvival
{
    internal static class AlliedGravshipThreatDiplomacyUtility
    {
        private static readonly FieldInfo DestinationTileField = AccessTools.Field(typeof(Gravship), "destinationTile");

        internal static void NotifyArrival(Gravship gravship)
        {
            if (gravship == null || Find.QuestManager == null)
                return;

            PlanetTile tile = DestinationTileField?.GetValue(gravship) is PlanetTile destination && destination.Valid
                ? destination
                : gravship.Tile;
            QuestPart_AlliedSettlementThreat threat = Find.QuestManager.QuestsListForReading
                .Where(quest => quest != null && quest.State == QuestState.Ongoing)
                .SelectMany(quest => quest.PartsListForReading.OfType<QuestPart_AlliedSettlementThreat>())
                .FirstOrDefault(part => part.site != null && !part.site.Destroyed && part.site.Tile == tile);

            if (threat == null)
                return;

            AlliedSettlementState state = AlliedSettlementWorldComponent.Current?.Get(threat.settlement);
            if (state == null || !state.crisisActive || state.crisisKind != AlliedSettlementCrisisKind.HostileThreat)
                return;

            bool alreadyRecorded = state.gravshipRespondedToThreat;
            state.gravshipRespondedToThreat = true;
            if (alreadyRecorded)
                return;
            Log.Message("[LiAIChat] Gravship arrived at allied threat site; proximity goodwill exemption enabled for " +
                threat.settlement.Label + ".");
        }

        internal static bool HasProximityExemption(Settlement settlement, int now)
        {
            AlliedSettlementState state = AlliedSettlementWorldComponent.Current?.Get(settlement);
            if (state == null)
                return false;

            bool respondingToActiveThreat = state.crisisActive &&
                state.crisisKind == AlliedSettlementCrisisKind.HostileThreat && state.gravshipRespondedToThreat;
            return respondingToActiveThreat || now < state.proximityGoodwillExemptUntilTick;
        }
    }

    [HarmonyPatch(typeof(GravshipUtility), nameof(GravshipUtility.ArriveExistingMap))]
    internal static class AlliedGravshipExistingMapArrivalPatch
    {
        private static void Prefix(Gravship gravship) => AlliedGravshipThreatDiplomacyUtility.NotifyArrival(gravship);
        private static void Postfix(Gravship gravship) => AlliedGravshipThreatDiplomacyUtility.NotifyArrival(gravship);
    }

    [HarmonyPatch(typeof(GravshipUtility), nameof(GravshipUtility.ArriveNewMap))]
    internal static class AlliedGravshipNewMapArrivalPatch
    {
        private static void Prefix(Gravship gravship) => AlliedGravshipThreatDiplomacyUtility.NotifyArrival(gravship);
        private static void Postfix(Gravship gravship) => AlliedGravshipThreatDiplomacyUtility.NotifyArrival(gravship);
    }

    [HarmonyPatch(typeof(SettlementProximityGoodwillUtility), nameof(SettlementProximityGoodwillUtility.AppendProximityGoodwillOffsets))]
    internal static class AlliedSettlementProximityGoodwillExemptionPatch
    {
        private static void Postfix(List<Pair<Settlement, int>> outOffsets)
        {
            if (outOffsets == null || AlliedSettlementWorldComponent.Current == null)
                return;

            int now = Find.TickManager?.TicksGame ?? 0;
            outOffsets.RemoveAll(offset => offset.First != null &&
                AlliedGravshipThreatDiplomacyUtility.HasProximityExemption(offset.First, now));
        }
    }
}
