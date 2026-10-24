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
        private static readonly HashSet<int> CremationInProgressCorpseIds = new HashSet<int>();

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
            if (state == null || state.Type != BlightfallSiteType.CarrionPlague || state.CarrionPlagueResolved)
                return;

            if (!state.CarrionPlagueWaveSpawned && state.CarrionPlagueWaveDueTick >= 0 &&
                Find.TickManager.TicksGame >= state.CarrionPlagueWaveDueTick)
                SpawnWave(map, state);

            ReconcileCorpses(map, state);
            TryComplete(site, state);
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
                    spawnedCount++;
                    bool enteredManhunterState = animal.mindState != null &&
                        animal.mindState.mentalStateHandler != null && animal.mindState.mentalStateHandler.TryStartMentalState(
                        MentalStateDefOf.Manhunter,
                        "LiAIChat_BlightfallCarrionPlagueInfectedReason".Translate().ToString(),
                        true, true, false, null, false, false, false);
                    if (!enteredManhunterState)
                        animal.mindState?.mentalStateHandler?.TryStartMentalState(
                            MentalStateDefOf.ManhunterPermanent,
                            "LiAIChat_BlightfallCarrionPlagueInfectedReason".Translate().ToString(),
                            true, true, false, null, false, false, false);
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
            if (animal == null)
                return;
            WorldComponent_Blightfall world = WorldComponent_Blightfall.Current;
            BlightfallSiteState state = world?.SiteStates.FirstOrDefault(candidate => candidate != null &&
                candidate.Type == BlightfallSiteType.CarrionPlague &&
                candidate.CarrionAnimals.Any(item => item != null && item.PawnThingId == animal.thingIDNumber));
            Site site = state == null ? null : Find.WorldObjects?.Sites.FirstOrDefault(candidate =>
                candidate != null && candidate.ID == state.SiteId);
            if (state == null || !state.CarrionPlagueWaveSpawned)
                return;

            BlightfallCarrionAnimalRecord record = state.CarrionAnimals.FirstOrDefault(item => item != null &&
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
            if (site != null)
                TryComplete(site, state);
        }

        public static bool IsTrackedCorpse(Corpse corpse)
        {
            int thingId = corpse?.thingIDNumber ?? -1;
            return thingId >= 0 && WorldComponent_Blightfall.Current?.SiteStates.Any(state =>
                state != null && state.Type == BlightfallSiteType.CarrionPlague && !state.CarrionPlagueResolved &&
                state.CarrionAnimals.Any(record => record != null && record.CorpseThingId == thingId)) == true;
        }

        public static bool IsTrackedCorpseId(int corpseThingId)
        {
            return corpseThingId >= 0 && WorldComponent_Blightfall.Current?.SiteStates.Any(state =>
                state != null && state.Type == BlightfallSiteType.CarrionPlague && !state.CarrionPlagueResolved &&
                state.CarrionAnimals.Any(record => record != null && record.CorpseThingId == corpseThingId)) == true;
        }

        public static void KeepCorpseRotting(Corpse corpse)
        {
            if (!IsTrackedCorpse(corpse))
                return;
            MakePermanentRottingCorpse(corpse);
        }

        private static void MakePermanentRottingCorpse(Corpse corpse)
        {
            CompRottable rottable = corpse?.GetComp<CompRottable>();
            if (rottable != null && RottableUtility.GetRotStage(corpse) != RotStage.Rotting)
                rottable.RotImmediately(RotStage.Rotting);
        }

        private static void ReconcileCorpses(Map map, BlightfallSiteState state)
        {
            if (map == null || state?.CarrionAnimals == null)
                return;
            foreach (BlightfallCarrionAnimalRecord record in state.CarrionAnimals)
            {
                if (record == null || !record.Dead || record.Burned)
                    continue;
                Corpse corpse = record.CorpseThingId >= 0
                    ? map.listerThings.AllThings.OfType<Corpse>().FirstOrDefault(candidate =>
                        candidate.thingIDNumber == record.CorpseThingId)
                    : map.listerThings.AllThings.OfType<Corpse>().FirstOrDefault(candidate =>
                        candidate.InnerPawn?.thingIDNumber == record.PawnThingId);
                if (corpse == null)
                    continue;
                record.CorpseThingId = corpse.thingIDNumber;
                MakePermanentRottingCorpse(corpse);
            }
        }

        private static void TryComplete(Site site, BlightfallSiteState state)
        {
            if (site == null || state == null || state.Type != BlightfallSiteType.CarrionPlague ||
                state.CarrionPlagueResolved || !state.CarrionPlagueWaveSpawned || state.CarrionAnimals.Count == 0 ||
                state.CarrionAnimals.Any(record => record == null || !record.Dead || !record.Burned))
                return;

            WorldComponent_Blightfall world = WorldComponent_Blightfall.Current;
            bool alreadyPurified = world != null && !world.IsTilePolluted(state.TileId);
            if (world == null || (!alreadyPurified &&
                (!world.CanPurifyTile(state.TileId) || !world.PurifyTile(state.TileId))))
            {
                Log.Warning("[LiAIChat Blightfall] Carrion Plague conditions met but tile " + state.TileId +
                    " cannot currently be purified.");
                return;
            }

            state.CarrionPlagueResolved = true;
            world.UpdateSiteStatus(site, BlightfallSiteStatus.Completed);
            Find.LetterStack?.ReceiveLetter(
                "LiAIChat_BlightfallSpecialSiteCompleteTitle".Translate(),
                "LiAIChat_BlightfallCarrionPlagueComplete".Translate(),
                LetterDefOf.PositiveEvent,
                site);
            Log.Message("[LiAIChat Blightfall] Carrion Plague site " + site.ID + " completed and tile " +
                state.TileId + " purified.");
        }

        public static bool CanDestroyTrackedCorpse(Corpse corpse)
        {
            if (!IsTrackedCorpse(corpse))
                return true;
            return FireUtility.IsBurning(corpse) || CremationInProgressCorpseIds.Contains(corpse.thingIDNumber);
        }

        public static void NotifyTrackedCorpseBurned(Corpse corpse)
        {
            if (!IsTrackedCorpse(corpse))
                return;
            WorldComponent_Blightfall world = WorldComponent_Blightfall.Current;
            BlightfallSiteState state = world?.SiteStates.FirstOrDefault(candidate => candidate != null &&
                candidate.Type == BlightfallSiteType.CarrionPlague && !candidate.CarrionPlagueResolved &&
                candidate.CarrionAnimals.Any(record => record != null &&
                    record.CorpseThingId == corpse.thingIDNumber));
            BlightfallCarrionAnimalRecord record = state?.CarrionAnimals.FirstOrDefault(candidate =>
                candidate != null && candidate.CorpseThingId == corpse.thingIDNumber);
            if (state == null || record == null)
                return;
            record.Dead = true;
            record.Burned = true;
            Site site = Find.WorldObjects?.Sites.FirstOrDefault(candidate => candidate != null &&
                candidate.ID == state.SiteId);
            TryComplete(site, state);
        }

        public static bool IsCremationRecipe(RecipeDef recipe)
        {
            return recipe?.defName != null &&
                (recipe.defName.IndexOf("Cremat", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 recipe.defName.IndexOf("Incinerat", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        public static void BeginCremationConsume(Thing ingredient, RecipeDef recipe)
        {
            if (ingredient is Corpse corpse && IsTrackedCorpse(corpse) && IsCremationRecipe(recipe))
                CremationInProgressCorpseIds.Add(corpse.thingIDNumber);
        }

        public static void EndCremationConsume(Thing ingredient, RecipeDef recipe)
        {
            if (ingredient is Corpse corpse && IsCremationRecipe(recipe))
                CremationInProgressCorpseIds.Remove(corpse.thingIDNumber);
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

    [HarmonyPatch(typeof(CompRottable), nameof(CompRottable.CompTickInterval))]
    internal static class BlightfallCarrionPermanentRotIntervalPatch
    {
        private static bool Prefix(CompRottable __instance)
        {
            Corpse corpse = __instance?.parent as Corpse;
            if (!BlightfallCarrionPlagueUtility.IsTrackedCorpse(corpse))
                return true;
            BlightfallCarrionPlagueUtility.KeepCorpseRotting(corpse);
            return false;
        }
    }

    [HarmonyPatch(typeof(CompRottable), nameof(CompRottable.CompTickRare))]
    internal static class BlightfallCarrionPermanentRotRarePatch
    {
        private static bool Prefix(CompRottable __instance)
        {
            Corpse corpse = __instance?.parent as Corpse;
            if (!BlightfallCarrionPlagueUtility.IsTrackedCorpse(corpse))
                return true;
            BlightfallCarrionPlagueUtility.KeepCorpseRotting(corpse);
            return false;
        }
    }

    [HarmonyPatch(typeof(Corpse), nameof(Corpse.Destroy), new Type[] { typeof(DestroyMode) })]
    internal static class BlightfallCarrionCorpseDestroyPatch
    {
        private static bool Prefix(Corpse __instance)
        {
            return BlightfallCarrionPlagueUtility.CanDestroyTrackedCorpse(__instance);
        }

        private static void Postfix(Corpse __instance)
        {
            if (__instance?.Destroyed == true)
                BlightfallCarrionPlagueUtility.NotifyTrackedCorpseBurned(__instance);
        }
    }

    [HarmonyPatch(typeof(Corpse), "get_IngestibleNow")]
    internal static class BlightfallCarrionCorpseIngestionBlockPatch
    {
        private static void Postfix(Corpse __instance, ref bool __result)
        {
            if (BlightfallCarrionPlagueUtility.IsTrackedCorpse(__instance))
                __result = false;
        }
    }

    [HarmonyPatch(typeof(Corpse), nameof(Corpse.ButcherProducts))]
    internal static class BlightfallCarrionNoButcheringPatch
    {
        private static bool Prefix(Corpse __instance, ref IEnumerable<Thing> __result)
        {
            if (!BlightfallCarrionPlagueUtility.IsTrackedCorpse(__instance))
                return true;
            __result = Enumerable.Empty<Thing>();
            return false;
        }
    }

    [HarmonyPatch(typeof(RecipeWorker), nameof(RecipeWorker.ConsumeIngredient))]
    internal static class BlightfallCarrionCremationConsumePatch
    {
        private static void Prefix(Thing ingredient, RecipeDef recipe)
        {
            BlightfallCarrionPlagueUtility.BeginCremationConsume(ingredient, recipe);
        }

        private static void Postfix(Thing ingredient, RecipeDef recipe)
        {
            BlightfallCarrionPlagueUtility.EndCremationConsume(ingredient, recipe);
        }

        private static Exception Finalizer(Thing ingredient, RecipeDef recipe, Exception __exception)
        {
            BlightfallCarrionPlagueUtility.EndCremationConsume(ingredient, recipe);
            return __exception;
        }
    }

    [HarmonyPatch(typeof(Tradeable), nameof(Tradeable.AddThing))]
    internal static class BlightfallCarrionCorpseTradeBlockPatch
    {
        private static bool Prefix(Thing __0)
        {
            return !(__0 is Corpse corpse && BlightfallCarrionPlagueUtility.IsTrackedCorpse(corpse));
        }
    }
}
