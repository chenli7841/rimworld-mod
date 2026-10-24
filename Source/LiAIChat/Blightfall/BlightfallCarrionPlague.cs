using RimWorld;
using RimWorld.Planet;
using Verse;

namespace LiAIChat.Blightfall
{
    public static class BlightfallCarrionPlagueUtility
    {
        public const int WaveDelayTicks = 60000;

        public static void Initialize(Map map)
        {
            Site site = map?.Parent as Site;
            WorldComponent_Blightfall world = WorldComponent_Blightfall.Current;
            BlightfallSiteState state = world?.GetSiteState(site);
            if (map == null || state == null || state.Type != BlightfallSiteType.CarrionPlague ||
                state.Status == BlightfallSiteStatus.Completed || state.Status == BlightfallSiteStatus.Removed ||
                state.CarrionPlagueResolved)
                return;

            if (state.CarrionPlagueWaveDueTick < 0 && Find.TickManager != null)
            {
                state.CarrionPlagueWaveDueTick = Find.TickManager.TicksGame + WaveDelayTicks;
                state.LastUpdatedTick = Find.TickManager.TicksGame;
            }

            if (!state.CarrionPlagueWarningSent && Find.LetterStack != null)
            {
                state.CarrionPlagueWarningSent = true;
                Find.LetterStack.ReceiveLetter(
                    "LiAIChat_BlightfallCarrionPlagueWarningTitle".Translate(),
                    "LiAIChat_BlightfallCarrionPlagueWarningText".Translate(site.Label),
                    LetterDefOf.ThreatBig,
                    site);
                Log.Message("[LiAIChat Blightfall] Carrion Plague warning issued for site " + site.ID +
                    "; waveDueTick=" + state.CarrionPlagueWaveDueTick + ".");
            }
        }

        public static void Tick(Map map)
        {
            if (map == null || map.IsPocketMap || Find.TickManager == null ||
                Find.TickManager.TicksGame % 250 != 0)
                return;

            Initialize(map);
            Site site = map.Parent as Site;
            BlightfallSiteState state = WorldComponent_Blightfall.Current?.GetSiteState(site);
            if (state == null || state.Type != BlightfallSiteType.CarrionPlague ||
                state.CarrionPlagueWaveSpawned || state.CarrionPlagueWaveDueTick < 0 ||
                Find.TickManager.TicksGame < state.CarrionPlagueWaveDueTick)
                return;

            SpawnWave(map, state);
        }

        private static void SpawnWave(Map map, BlightfallSiteState state)
        {
            // Filled by the staged horde implementation.
        }
    }
}
