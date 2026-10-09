using System;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace LiAIChat.AlliedSettlementSurvival
{
    public class MainTabWindow_AlliedSettlements : MainTabWindow
    {
        private const float HeaderHeight = 42f;
        private const float RowHeight = 58f;
        private Vector2 scrollPosition;

        public override Vector2 RequestedTabSize => new Vector2(1080f, 680f);

        public override void DoWindowContents(Rect inRect)
        {
            AlliedSettlementWorldComponent component = AlliedSettlementWorldComponent.Current;
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, inRect.width, 34f), "LiASS_OverviewTitle".Translate());
            Text.Font = GameFont.Small;

            Rect listRect = new Rect(0f, 42f, inRect.width, inRect.height - 42f);
            if (component == null)
            {
                Widgets.Label(listRect, "LiASS_OverviewUnavailable".Translate());
                return;
            }

            var states = component.States.Where(state => state?.settlement != null)
                .OrderBy(state => state.settlement.LabelCap).ToList();
            if (states.Count == 0)
            {
                Widgets.Label(listRect, "LiASS_OverviewEmpty".Translate());
                return;
            }

            Rect viewRect = new Rect(0f, 0f, listRect.width - 18f, HeaderHeight + states.Count * RowHeight);
            Widgets.BeginScrollView(listRect, ref scrollPosition, viewRect);
            DrawHeader(viewRect.width);

            for (int i = 0; i < states.Count; i++)
                DrawRow(states[i], i, viewRect.width);

            Widgets.EndScrollView();
        }

        private static void DrawHeader(float width)
        {
            Rect rect = new Rect(0f, 0f, width, HeaderHeight);
            Widgets.DrawMenuSection(rect);
            Widgets.Label(new Rect(12f, 8f, 210f, 26f), "LiASS_OverviewSettlement".Translate());
            Widgets.Label(new Rect(230f, 8f, 145f, 26f), "LiASS_OverviewStrength".Translate());
            Widgets.Label(new Rect(385f, 8f, 165f, 26f), "LiASS_OverviewStatus".Translate());
            Widgets.Label(new Rect(560f, 8f, Math.Max(100f, width - 690f), 26f), "LiASS_OverviewCrisis".Translate());
            Widgets.Label(new Rect(width - 118f, 8f, 106f, 26f), "LiASS_OverviewAction".Translate());
        }

        private static void DrawRow(AlliedSettlementState state, int index, float width)
        {
            Settlement settlement = state.settlement;
            float y = HeaderHeight + index * RowHeight;
            Rect row = new Rect(0f, y, width, RowHeight);
            Widgets.DrawAltRect(row);

            string status = state.pendingCollapse
                ? "LiASS_Pending".Translate()
                : AlliedSettlementWorldComponent.Current.Eligible(settlement)
                    ? "LiASS_Active".Translate()
                    : "LiASS_Frozen".Translate();
            string crisis;
            if (state.crisisActive)
            {
                float daysLeft = Mathf.Max(0f, state.crisisDeadlineTick - Find.TickManager.TicksGame) /
                    (float)SettlementStrengthPolicy.TicksPerDay;
                crisis = "LiASS_OverviewActiveCrisis".Translate(
                    ("LiASS_CrisisKind_" + state.crisisKind).Translate(), daysLeft.ToString("0.0"));
            }
            else if (state.pendingCollapse)
            {
                crisis = "LiASS_OverviewCollapsed".Translate();
            }
            else
            {
                float daysToNext = Mathf.Max(0f, state.nextCrisisTick - Find.TickManager.TicksGame) /
                    (float)SettlementStrengthPolicy.TicksPerDay;
                crisis = "LiASS_OverviewNextCrisis".Translate(daysToNext.ToString("0.0"));
            }

            Widgets.Label(new Rect(12f, y + 6f, 210f, 46f), settlement.LabelCap);
            Widgets.Label(new Rect(230f, y + 6f, 145f, 46f),
                "LiASS_Strength".Translate(state.strength.ToString("0.0")));
            Widgets.Label(new Rect(385f, y + 6f, 165f, 46f), status);
            Widgets.Label(new Rect(560f, y + 6f, Math.Max(100f, width - 690f), 46f), crisis);
            if (Widgets.ButtonText(new Rect(width - 118f, y + 12f, 106f, 34f), "LiASS_OverviewGoTo".Translate()))
            {
                Find.WorldCameraDriver.JumpTo(settlement.Tile);
                Find.WorldSelector.ClearSelection();
                Find.WorldSelector.Select(settlement, true);
            }
        }
    }
}
