using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace LiAIChat.Blightfall
{
    public static class BlightfallCarrionPlagueUtility
    {
        public const int WaveDelayTicks = 60000;
        private const string InfectionHediffDefName = "LiAIChat_BlightInfected";

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
            if (map == null || state == null || state.CarrionPlagueWaveSpawned)
                return;

            HediffDef infectionDef = DefDatabase<HediffDef>.GetNamedSilentFail(InfectionHediffDefName);
            List<PawnKindDef> candidates = map.Biome?.AllWildAnimals?
                .Where(IsEligibleAnimal).Distinct().ToList() ?? new List<PawnKindDef>();
            if (candidates.Count == 0)
            {
                candidates = DefDatabase<PawnKindDef>.AllDefs.Where(IsEligibleAnimal).ToList();
            }
            if (infectionDef == null || candidates.Count == 0)
            {
                Log.Error("[LiAIChat Blightfall] Could not start Carrion Plague wave: infection hediff or animal kinds are unavailable.");
                state.CarrionPlagueWaveDueTick = Find.TickManager.TicksGame + 2500;
                return;
            }

            // Save the one-shot guard before generating pawns, so an interrupted
            // generation cannot create a second full wave after loading a save.
            state.CarrionPlagueWaveSpawned = true;
            int targetCount = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(8f, 22f,
                Mathf.Clamp01(state.Difficulty / 100f))), 8, 22);
            int spawnedCount = 0;
            for (int i = 0; i < targetCount; i++)
            {
                PawnKindDef kind = candidates.RandomElement();
                Pawn animal = null;
                try
                {
                    animal = PawnGenerator.GeneratePawn(kind, null);
                    if (animal == null || animal.health == null)
                        continue;

                    animal.health.AddHediff(HediffMaker.MakeHediff(infectionDef, animal));
                    IntVec3 cell = CellFinder.RandomSpawnCellForPawnNear(map.Center, map, 12);
                    GenSpawn.Spawn(animal, cell, map);
                    var record = new BlightfallCarrionAnimalRecord { PawnThingId = animal.thingIDNumber };
                    state.CarrionAnimals.Add(record);
                    animal.mindState?.mentalStateHandler?.TryStartMentalState(
                        Verse.AI.MentalStateDefOf.Manhunter,
                        "LiAIChat_BlightfallCarrionPlagueInfectedReason".Translate().ToString(),
                        true, true, false, null, false, false, false);
                    spawnedCount++;
                }
                catch (Exception exception)
                {
                    Log.Warning("[LiAIChat Blightfall] Failed to generate one Carrion Plague animal of kind " +
                        kind.defName + ": " + exception.Message);
                    if (animal != null && !animal.Spawned && !animal.Destroyed)
                        animal.Destroy(DestroyMode.Vanish);
                }
            }

            if (spawnedCount == 0)
            {
                state.CarrionPlagueWaveSpawned = false;
                state.CarrionPlagueWaveDueTick = Find.TickManager.TicksGame + 2500;
                Log.Error("[LiAIChat Blightfall] Carrion Plague wave generated no animals; will retry.");
                return;
            }

            Find.LetterStack?.ReceiveLetter(
                "LiAIChat_BlightfallCarrionPlagueArrivalTitle".Translate(),
                "LiAIChat_BlightfallCarrionPlagueArrivalText".Translate(spawnedCount),
                LetterDefOf.ThreatBig,
                map.Parent);
            Log.Message("[LiAIChat Blightfall] Carrion Plague wave spawned " + spawnedCount +
                " infected manhunters for site " + (map.Parent as Site)?.ID + ".");
        }

        private static bool IsEligibleAnimal(PawnKindDef kind)
        {
            return kind != null && kind.canArriveManhunter && kind.race?.race?.Animal == true &&
                kind.combatPower > 0f;
        }

        public static void NotifyAnimalKilled(Pawn animal)
        {
            if (animal == null || animal.Map == null)
                return;
            Site site = animal.Map.Parent as Site;
            BlightfallSiteState state = WorldComponent_Blightfall.Current?.GetSiteState(site);
            if (state == null || state.Type != BlightfallSiteType.CarrionPlague ||
                !state.CarrionPlagueWaveSpawned)
                return;

            BlightfallCarrionAnimalRecord record = state.CarrionAnimals.FirstOrDefault(item =>
                item.PawnThingId == animal.thingIDNumber);
            if (record == null)
                return;
            record.Dead = true;
            Corpse corpse = animal.Corpse;
            if (corpse != null)
            {
                record.CorpseThingId = corpse.thingIDNumber;
                MakePermanentRottingCorpse(corpse);
            }
        }

        public static string InspectString(Site site)
        {
            BlightfallSiteState state = WorldComponent_Blightfall.Current?.GetSiteState(site);
            if (site == null || state?.Type != BlightfallSiteType.CarrionPlague || state.CarrionPlagueResolved)
                return null;

            if (!state.CarrionPlagueWaveSpawned)
            {
                int remainingTicks = Math.Max(0, state.CarrionPlagueWaveDueTick -
                    (Find.TickManager?.TicksGame ?? 0));
                int remainingHours = Mathf.CeilToInt(remainingTicks / 2500f);
                return "LiAIChat_BlightfallCarrionPlagueStatusCountdown".Translate(remainingHours).ToString();
            }

            int alive = state.CarrionAnimals.Count(record => record != null && !record.Dead);
            int corpses = state.CarrionAnimals.Count(record => record != null && record.Dead && !record.Burned);
            return "LiAIChat_BlightfallCarrionPlagueStatusCleanup".Translate(alive, corpses).ToString();
        }
    }

    [HarmonyPatch(typeof(Pawn), "Kill")]
    internal static class BlightfallCarrionAnimalKilledPatch
    {
        private static void Postfix(Pawn __instance)
        {
            BlightfallCarrionPlagueUtility.NotifyAnimalKilled(__instance);
        }
    }

    [HarmonyPatch(typeof(Site), "GetInspectString")]
    internal static class BlightfallCarrionPlagueSiteInspectPatch
    {
        private static void Postfix(Site __instance, ref string __result)
        {
            string carrionStatus = BlightfallCarrionPlagueUtility.InspectString(__instance);
            if (!string.IsNullOrEmpty(carrionStatus))
                __result = string.IsNullOrEmpty(__result) ? carrionStatus : __result + "\n" + carrionStatus;
        }
    }
}
