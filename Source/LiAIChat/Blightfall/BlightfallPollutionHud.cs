using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace LiAIChat.Blightfall
{
    [HarmonyPatch(typeof(MapInterface), nameof(MapInterface.MapInterfaceOnGUI_AfterMainTabs))]
    public static class BlightfallPollutionHudPatch
    {
        private const float PanelWidth = 270f;
        private const float PanelHeight = 34f;

        public static void Postfix()
        {
            Map map = Find.CurrentMap;
            if (map?.pollutionGrid == null || !ModsConfig.BiotechActive)
                return;

            Rect panel = new Rect((Verse.UI.screenWidth - PanelWidth) * 0.5f, 42f, PanelWidth, PanelHeight);
            Widgets.DrawWindowBackground(panel);

            GameFont previousFont = Text.Font;
            TextAnchor previousAnchor = Text.Anchor;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(panel,
                "LiAIChat_BlightfallLocalPollutionCount".Translate(map.pollutionGrid.TotalPollution));
            Text.Anchor = previousAnchor;
            Text.Font = previousFont;
        }
    }
}
