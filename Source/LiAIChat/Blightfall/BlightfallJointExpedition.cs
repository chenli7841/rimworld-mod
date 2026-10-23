using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using LiAIChat.AlliedSettlementSurvival;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace LiAIChat.Blightfall
{
    [HarmonyPatch(typeof(Building_CommsConsole), nameof(Building_CommsConsole.GetFloatMenuOptions))]
    public static class BlightfallJointExpeditionCommsPatch
    {
        public static IEnumerable<FloatMenuOption> Postfix(IEnumerable<FloatMenuOption> __result,
            Building_CommsConsole __instance, Pawn selPawn)
        {
            if (__result != null)
                foreach (FloatMenuOption option in __result)
                    yield return option;

            Map map = __instance?.Map;
            Site site = map?.Parent as Site;
            WorldComponent_Blightfall campaign = WorldComponent_Blightfall.Current;
            BlightfallSiteState siteState = campaign?.GetSiteState(site);
            if (siteState == null || siteState.Status == BlightfallSiteStatus.Completed ||
                siteState.Status == BlightfallSiteStatus.Removed || selPawn == null ||
                !selPawn.IsColonistPlayerControlled || !__instance.CanUseCommsNow)
                yield break;

            List<AlliedSettlementState> sources = AlliedSettlementWorldComponent.Current?.States
                .Where(state => state != null && state.settlement != null && state.active &&
                    !state.pendingCollapse && state.strength > 0f && state.settlement.Faction != null &&
                    state.settlement.Tile.Valid && state.settlement.Tile.Layer == site.Tile.Layer &&
                    state.settlement.Faction != Faction.OfPlayer && !state.settlement.Faction.HostileTo(Faction.OfPlayer) &&
                    state.settlement.Faction.RelationKindWith(Faction.OfPlayer) == FactionRelationKind.Ally)
                .OrderByDescending(state => state.strength)
                .ToList() ?? new List<AlliedSettlementState>();

            if (sources.Count == 0)
            {
                yield return new FloatMenuOption("LiAIChat_BlightfallNoAlliedExpeditionSource".Translate(), null);
                yield break;
            }

            yield return new FloatMenuOption("LiAIChat_BlightfallRequestJointExpedition".Translate(), () =>
                OpenSourceMenu(site, map, sources));
        }

        internal static void OpenSourceMenu(Site site, Map map, List<AlliedSettlementState> sources)
        {
            BlightfallSiteState siteState = WorldComponent_Blightfall.Current?.GetSiteState(site);
            if (siteState == null || siteState.AlliedExpeditionRequested)
            {
                Messages.Message("LiAIChat_BlightfallExpeditionAlreadyRequested".Translate(),
                    MessageTypeDefOf.RejectInput, false);
                return;
            }

            List<FloatMenuOption> options = sources.Select(source => new FloatMenuOption(
                "LiAIChat_BlightfallExpeditionSourceOption".Translate(source.settlement.Faction.Name,
                    source.settlement.LabelCap, source.strength.ToString("0")),
                () => RequestSupport(site, map, source))).ToList();
            Find.WindowStack.Add(new FloatMenu(options));
        }

        private static void RequestSupport(Site site, Map map, AlliedSettlementState source)
        {
            WorldComponent_Blightfall campaign = WorldComponent_Blightfall.Current;
            BlightfallSiteState siteState = campaign?.GetSiteState(site);
            AlliedSettlementWorldComponent allied = AlliedSettlementWorldComponent.Current;
            if (site == null || map == null || site.Map != map || siteState == null || source?.settlement == null ||
                allied == null || !source.active || source.pendingCollapse || source.strength <= 0f ||
                source.settlement.Faction == null || source.settlement.Faction.HostileTo(Faction.OfPlayer) ||
                !source.settlement.Tile.Valid || source.settlement.Tile.Layer != site.Tile.Layer ||
                !allied.Eligible(source.settlement) || siteState.AlliedExpeditionRequested)
            {
                Messages.Message("LiAIChat_BlightfallExpeditionUnavailable".Translate(),
                    MessageTypeDefOf.RejectInput, false);
                return;
            }

            // Persist the one-request-per-site lock before invoking the incident worker,
            // so re-entrant UI clicks cannot create a second group.
            siteState.AlliedExpeditionRequested = true;
            siteState.AlliedExpeditionRequestTick = Find.TickManager.TicksGame;
            siteState.AlliedExpeditionFaction = source.settlement.Faction;

            IncidentParms parms = StorytellerUtility.DefaultParmsNow(IncidentDefOf.RaidFriendly.category, map);
            parms.faction = source.settlement.Faction;
            parms.pawnGroupKind = PawnGroupKindDefOf.Combat;
            parms.raidStrategy = RaidStrategyDefOf.ImmediateAttackFriendly;
            parms.raidArrivalMode = PawnsArrivalModeDefOf.EdgeWalkIn;
            parms.raidArrivalModeForQuickMilitaryAid = true;
            if (IncidentDefOf.RaidFriendly.Worker.TryExecute(parms))
            {
                siteState.AlliedExpeditionConsumed = true;
                Messages.Message("LiAIChat_BlightfallExpeditionRequested".Translate(
                    source.settlement.Faction.Name, site.Label), site, MessageTypeDefOf.PositiveEvent);
                Log.Message("[LiAIChat Blightfall] Joint expedition requested from " +
                    source.settlement.Faction.Name + " at " + site.Label + ".");
            }
            else
            {
                siteState.AlliedExpeditionConsumed = true;
                Messages.Message("LiAIChat_BlightfallExpeditionFailed".Translate(),
                    MessageTypeDefOf.RejectInput, false);
                Log.Warning("[LiAIChat Blightfall] Friendly military aid incident could not execute at " +
                    site.Label + "; the site request remains consumed to prevent repeated spawning.");
            }
        }
    }

    // Most generated Site maps have no comms console. Expose the same interaction
    // from a selected colonist's command bar so an expedition can still call for aid.
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetGizmos))]
    public static class BlightfallJointExpeditionPawnGizmoPatch
    {
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Pawn __instance)
        {
            if (__result != null)
                foreach (Gizmo gizmo in __result)
                    yield return gizmo;

            if (__instance == null || !__instance.IsColonistPlayerControlled || __instance.Map == null)
                yield break;

            Site site = __instance.Map.Parent as Site;
            BlightfallSiteState siteState = WorldComponent_Blightfall.Current?.GetSiteState(site);
            if (siteState == null || siteState.Status == BlightfallSiteStatus.Completed ||
                siteState.Status == BlightfallSiteStatus.Removed || siteState.AlliedExpeditionRequested)
                yield break;

            List<AlliedSettlementState> sources = AlliedSettlementWorldComponent.Current?.States
                .Where(state => state != null && state.settlement != null && state.active &&
                    !state.pendingCollapse && state.strength > 0f && state.settlement.Faction != null &&
                    state.settlement.Tile.Valid && state.settlement.Tile.Layer == site.Tile.Layer &&
                    state.settlement.Faction != Faction.OfPlayer &&
                    !state.settlement.Faction.HostileTo(Faction.OfPlayer) &&
                    state.settlement.Faction.RelationKindWith(Faction.OfPlayer) == FactionRelationKind.Ally)
                .OrderByDescending(state => state.strength)
                .ToList() ?? new List<AlliedSettlementState>();
            if (sources.Count == 0)
                yield break;

            yield return new Command_Action
            {
                defaultLabel = "LiAIChat_BlightfallRequestJointExpedition".Translate(),
                defaultDesc = "LiAIChat_BlightfallJointExpeditionGizmoDesc".Translate(),
                action = () => BlightfallJointExpeditionCommsPatch.OpenSourceMenu(
                    site, __instance.Map, sources)
            };
        }
    }

    [HarmonyPatch(typeof(IncidentWorker_RaidFriendly), "CanFireNowSub")]
    public static class BlightfallJointExpeditionIncidentEligibilityPatch
    {
        public static bool Prefix(IncidentParms parms, ref bool __result)
        {
            Map map = parms?.target as Map;
            Site site = map?.Parent as Site;
            BlightfallSiteState siteState = WorldComponent_Blightfall.Current?.GetSiteState(site);
            if (parms == null || !parms.raidArrivalModeForQuickMilitaryAid || siteState == null ||
                !siteState.AlliedExpeditionRequested)
                return true;

            if (siteState.AlliedExpeditionConsumed)
            {
                __result = false;
                return false;
            }

            __result = map != null && parms.faction != null && parms.faction.RelationKindWith(Faction.OfPlayer) ==
                FactionRelationKind.Ally;
            return false;
        }
    }
}
