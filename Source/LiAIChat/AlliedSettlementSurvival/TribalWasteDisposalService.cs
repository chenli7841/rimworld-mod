using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace LiAIChat.AlliedSettlementSurvival
{
    public sealed class TribalWasteDisposalState : IExposable
    {
        public Settlement settlement;
        public int periodStartTick = -1;
        public int disposedPacks;

        public void ExposeData()
        {
            Scribe_References.Look(ref settlement, "settlement");
            Scribe_Values.Look(ref periodStartTick, "periodStartTick", -1);
            Scribe_Values.Look(ref disposedPacks, "disposedPacks", 0);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
                disposedPacks = Math.Max(0, Math.Min(TribalWasteDisposalService.PeriodCapacity, disposedPacks));
        }
    }

    public sealed class WorldComponent_TribalWasteDisposal : WorldComponent
    {
        private List<TribalWasteDisposalState> states = new List<TribalWasteDisposalState>();

        public static WorldComponent_TribalWasteDisposal Current =>
            Find.World?.GetComponent<WorldComponent_TribalWasteDisposal>();

        public WorldComponent_TribalWasteDisposal(World world) : base(world) { }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref states, "liTribalWasteDisposalStates", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && states == null)
                states = new List<TribalWasteDisposalState>();
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
                states.RemoveAll(state => state == null || state.settlement == null);
        }

        public int RemainingCapacity(Settlement settlement)
        {
            TribalWasteDisposalState state = StateFor(settlement);
            RefreshPeriod(state);
            return Math.Max(0, TribalWasteDisposalService.PeriodCapacity - state.disposedPacks);
        }

        public int PacksDisposedThisPeriod(Settlement settlement)
        {
            TribalWasteDisposalState state = StateFor(settlement);
            RefreshPeriod(state);
            return state.disposedPacks;
        }

        public void RecordDisposal(Settlement settlement, int packCount)
        {
            TribalWasteDisposalState state = StateFor(settlement);
            RefreshPeriod(state);
            state.disposedPacks = Math.Min(TribalWasteDisposalService.PeriodCapacity,
                state.disposedPacks + Math.Max(0, packCount));
        }

        private TribalWasteDisposalState StateFor(Settlement settlement)
        {
            TribalWasteDisposalState state = states.FirstOrDefault(item => item.settlement == settlement);
            if (state != null) return state;

            state = new TribalWasteDisposalState
            {
                settlement = settlement,
                periodStartTick = CurrentPeriodStartTick()
            };
            states.Add(state);
            return state;
        }

        private static void RefreshPeriod(TribalWasteDisposalState state)
        {
            int currentPeriodStart = CurrentPeriodStartTick();
            if (state.periodStartTick != currentPeriodStart)
            {
                state.periodStartTick = currentPeriodStart;
                state.disposedPacks = 0;
            }
        }

        private static int CurrentPeriodStartTick()
        {
            int now = Find.TickManager?.TicksGame ?? 0;
            return now / TribalWasteDisposalService.PeriodTicks * TribalWasteDisposalService.PeriodTicks;
        }
    }

    public static class TribalWasteDisposalService
    {
        public const int SilverPerPack = 30;
        public const int PeriodCapacity = 150;
        public const int PeriodTicks = 15 * GenDate.TicksPerDay;

        public static bool HasService(Settlement settlement)
        {
            if (!ModsConfig.BiotechActive || settlement == null || settlement.Destroyed || settlement.HasMap ||
                settlement.Faction == null || settlement.Faction == Faction.OfPlayer ||
                settlement.Faction.HostileTo(Faction.OfPlayer) || settlement.Faction.def.techLevel != TechLevel.Neolithic)
                return false;

            // A stable half of eligible tribal settlements offer the service. WorldObject IDs survive saves.
            return settlement.ID % 2 == 0;
        }

        public static Settlement ActiveServiceSettlement
        {
            get
            {
                Settlement settlement = TradeSession.trader as Settlement;
                return HasService(settlement) ? settlement : null;
            }
        }

        public static bool IsWastepack(Thing thing)
        {
            return ModsConfig.BiotechActive && thing?.def?.defName == "Wastepack";
        }

        public static bool IsWastepack(Tradeable tradeable)
        {
            return tradeable != null && tradeable.ThingDef?.defName == "Wastepack";
        }

        public static int RemainingCapacity(Settlement settlement)
        {
            return WorldComponent_TribalWasteDisposal.Current?.RemainingCapacity(settlement) ?? 0;
        }

        public static int TransferCount(Tradeable tradeable)
        {
            int count = tradeable?.CountToTransfer ?? 0;
            return count < 0 ? -count : count;
        }

        public static int SelectedWastepackCount(TradeDeal deal)
        {
            if (deal?.AllTradeables == null) return 0;
            return deal.AllTradeables.Where(tradeable => IsWastepack(tradeable) &&
                tradeable.ActionToDo == TradeAction.PlayerSells).Sum(TransferCount);
        }

        public static void DisposeSoldWastepacks(Settlement settlement, Thing thing, int count)
        {
            WorldComponent_TribalWasteDisposal component = WorldComponent_TribalWasteDisposal.Current;
            int remaining = component?.RemainingCapacity(settlement) ?? 0;
            if (component == null || thing == null || count <= 0 || count > remaining)
            {
                Log.Error("[LiAIChat] Rejected an over-capacity tribal waste disposal trade.");
                return;
            }

            Thing disposed = count >= thing.stackCount ? thing : thing.SplitOff(count);
            disposed.Destroy(DestroyMode.Vanish);
            component.RecordDisposal(settlement, count);
        }

    }

    [HarmonyPatch(typeof(TradeDeal), "InSellablePosition")]
    public static class TribalWasteDisposalSellablePositionPatch
    {
        public static void Postfix(Thing __0, ref string __1, ref bool __result)
        {
            Settlement settlement = TribalWasteDisposalService.ActiveServiceSettlement;
            if (!TribalWasteDisposalService.IsWastepack(__0) || settlement == null) return;

            if (TribalWasteDisposalService.RemainingCapacity(settlement) > 0)
            {
                __result = true;
                __1 = null;
            }
            else
            {
                __result = false;
                __1 = "LiTWD_NoCapacity".Translate();
            }
        }
    }

    [HarmonyPatch(typeof(Tradeable), "get_TraderWillTrade")]
    public static class TribalWasteDisposalTraderWillTradePatch
    {
        public static void Postfix(Tradeable __instance, ref bool __result)
        {
            Settlement settlement = TribalWasteDisposalService.ActiveServiceSettlement;
            if (TribalWasteDisposalService.IsWastepack(__instance) && settlement != null)
                __result = TribalWasteDisposalService.RemainingCapacity(settlement) > 0;
        }
    }

    [HarmonyPatch(typeof(Tradeable), nameof(Tradeable.GetPriceFor))]
    public static class TribalWasteDisposalPricePatch
    {
        public static void Postfix(Tradeable __instance, TradeAction __0, ref float __result)
        {
            Settlement settlement = TribalWasteDisposalService.ActiveServiceSettlement;
            if (TribalWasteDisposalService.IsWastepack(__instance) && settlement != null &&
                __0 == TradeAction.PlayerSells)
                __result = -TribalWasteDisposalService.SilverPerPack;
        }
    }

    [HarmonyPatch(typeof(Tradeable), "set_CountToTransfer")]
    public static class TribalWasteDisposalCountToTransferPatch
    {
        public static void Prefix(Tradeable __instance, ref int __0)
        {
            Settlement settlement = TribalWasteDisposalService.ActiveServiceSettlement;
            if (!TribalWasteDisposalService.IsWastepack(__instance) || settlement == null) return;

            int capacity = TribalWasteDisposalService.RemainingCapacity(settlement);
            if (Math.Abs((long)__0) > capacity)
                __0 = Math.Sign(__0) * capacity;
        }
    }

    [HarmonyPatch(typeof(TradeDeal), nameof(TradeDeal.TryExecute))]
    public static class TribalWasteDisposalTradeAcceptancePatch
    {
        public static bool Prefix(ref bool __0)
        {
            Settlement settlement = TribalWasteDisposalService.ActiveServiceSettlement;
            if (settlement == null) return true;

            int selected = TribalWasteDisposalService.SelectedWastepackCount(TradeSession.deal);
            if (selected <= TribalWasteDisposalService.RemainingCapacity(settlement)) return true;

            __0 = false;
            Messages.Message("LiTWD_Insufficient".Translate(), MessageTypeDefOf.RejectInput, false);
            return false;
        }
    }

    [HarmonyPatch(typeof(Settlement), nameof(Settlement.GiveSoldThingToTrader))]
    public static class TribalWasteDisposalSettlementReceiptPatch
    {
        public static bool Prefix(Settlement __instance, Thing thing, int count)
        {
            if (!TribalWasteDisposalService.IsWastepack(thing) ||
                !TribalWasteDisposalService.HasService(__instance)) return true;

            TribalWasteDisposalService.DisposeSoldWastepacks(__instance, thing, count);
            return false;
        }
    }
}
