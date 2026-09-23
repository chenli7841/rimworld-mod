using LiAIChat.Events;
using LiAIChat.Models;
using LiAIChat.State;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace LiAIChat.Travel
{
    public static class TravelWishManager
    {
        private const int StayDurationTicks = 480000;
        private static readonly string[] BenefitDefs = { "LiAIChat_TravelWish_Mood", "LiAIChat_TravelWish_Recovery", "LiAIChat_TravelWish_Industry", "LiAIChat_TravelWish_Combat" };

        public static void Check()
        {
            if (Find.TickManager == null) return;
            List<Pawn> pawns = Find.Maps.Where(map => map != null).SelectMany(map => map.mapPawns.FreeColonistsSpawned).Distinct().ToList();
            foreach (Pawn pawn in pawns)
            {
                PawnAIState state = PawnAIStateManager.TryGetExistingState(pawn);
                if (state == null) continue;
                if (state.TravelWish == null)
                {
                    TryCreateWish(pawn, state, pawns);
                    continue;
                }
                UpdateWish(pawn, state, pawns);
            }
        }

        private static void TryCreateWish(Pawn pawn, PawnAIState state, List<Pawn> pawns)
        {
            if (pawn.ageTracker == null || pawn.ageTracker.AgeBiologicalYears < 13 || !Rand.Chance(0.035f)) return;
            Pawn partner = FindPartner(pawn, pawns);
            if (partner != null && Rand.Chance(0.55f))
            {
                PawnAIState partnerState = PawnAIStateManager.TryGetExistingState(partner);
                if (partnerState != null && partnerState.TravelWish == null)
                {
                    TravelWish shared = CreateWish(partner.thingIDNumber, null, pawn.Map.Tile);
                    state.TravelWish = shared;
                    partnerState.TravelWish = CreateWish(pawn.thingIDNumber, shared);
                    AnnounceWish(pawn.LabelShort + " 与 " + partner.LabelShort, shared);
                    return;
                }
            }
            state.TravelWish = CreateWish(-1, null, pawn.Map.Tile);
            AnnounceWish(pawn.LabelShort, state.TravelWish);
        }

        private static TravelWish CreateWish(int partnerId, TravelWish template = null, int originTile = -1)
        {
            if (template != null)
                return new TravelWish { PartnerPawnId = partnerId, OriginTile = template.OriginTile, BiomeDefName = template.BiomeDefName, DesiredSeason = template.DesiredSeason, BenefitDefNames = new List<string>(template.BenefitDefNames) };
            List<BiomeDef> biomes = DefDatabase<BiomeDef>.AllDefsListForReading.Where(biome => biome != null && !biome.defName.NullOrEmpty() && !biome.defName.Contains("Ocean") && !biome.defName.Contains("Lake")).ToList();
            TravelWish wish = new TravelWish { PartnerPawnId = partnerId, OriginTile = originTile, BiomeDefName = biomes.RandomElement().defName, DesiredSeason = new[] { "Spring", "Summer", "Fall", "Winter" }.RandomElement() };
            wish.BenefitDefNames = BenefitDefs.InRandomOrder().Take(Rand.RangeInclusive(2, 3)).ToList();
            return wish;
        }

        private static void UpdateWish(Pawn pawn, PawnAIState state, List<Pawn> pawns)
        {
            TravelWish wish = state.TravelWish;
            if (wish.Active)
            {
                if (Find.TickManager.TicksGame - wish.ActivatedTick >= StayDurationTicks || !MatchesStay(pawn, state, pawns))
                    EndWish(pawn, state, pawns);
                return;
            }
            if (MatchesStay(pawn, state, pawns))
            {
                wish.ActivatedTick = Find.TickManager.TicksGame;
                ApplyBenefits(pawn, wish);
                Pawn partner = FindPawn(wish.PartnerPawnId, pawns);
                if (partner != null) ApplyBenefits(partner, PawnAIStateManager.TryGetExistingState(partner).TravelWish);
                Messages.Message(pawn.LabelShort + " 的旅居愿望已得到满足。", pawn, MessageTypeDefOf.PositiveEvent);
                ColonyEventLog.Record("旅居愿望", pawn.LabelShort + " 开始了一段心仪的旅居。", 2, pawn, "travel-wish:" + pawn.thingIDNumber + ":" + wish.ActivatedTick);
            }
        }

        private static bool MatchesStay(Pawn pawn, PawnAIState state, List<Pawn> pawns)
        {
            TravelWish wish = state.TravelWish;
            Map map = pawn.Map;
            if (map == null || map.Biome == null || (wish.OriginTile >= 0 && map.Tile == wish.OriginTile) || map.Biome.defName != wish.BiomeDefName || GenLocalDate.Season(map).ToString() != wish.DesiredSeason) return false;
            Pawn partner = FindPawn(wish.PartnerPawnId, pawns);
            if (partner != null && partner.Map != map) return false;
            Building_Bed bed = pawn.ownership?.OwnedBed;
            Room room = pawn.ownership?.OwnedRoom;
            if (bed == null || room == null || room.PsychologicallyOutdoors) return false;
            if (partner != null && partner.ownership?.OwnedRoom != room) return false;
            int requiredSlots = partner == null ? 1 : 2;
            int slots = room.ContainedAndAdjacentThings.OfType<Building_Bed>().Sum(item => item.SleepingSlotsCount);
            return slots >= requiredSlots;
        }

        private static void ApplyBenefits(Pawn pawn, TravelWish wish)
        {
            if (pawn?.health == null || wish == null) return;
            foreach (string defName in wish.BenefitDefNames)
            {
                HediffDef def = DefDatabase<HediffDef>.GetNamedSilentFail(defName);
                if (def != null && pawn.health.hediffSet.GetFirstHediffOfDef(def) == null)
                    pawn.health.AddHediff(HediffMaker.MakeHediff(def, pawn));
            }
        }

        private static void EndWish(Pawn pawn, PawnAIState state, List<Pawn> pawns)
        {
            TravelWish wish = state.TravelWish;
            RemoveBenefits(pawn, wish);
            Pawn partner = FindPawn(wish.PartnerPawnId, pawns);
            if (partner != null)
            {
                PawnAIState partnerState = PawnAIStateManager.TryGetExistingState(partner);
                if (partnerState?.TravelWish != null) { RemoveBenefits(partner, partnerState.TravelWish); partnerState.TravelWish = null; }
            }
            state.TravelWish = null;
            Messages.Message(pawn.LabelShort + " 的旅居加成已经结束；日后可能会产生新的旅行愿望。", pawn, MessageTypeDefOf.NeutralEvent);
        }

        private static void RemoveBenefits(Pawn pawn, TravelWish wish)
        {
            if (pawn?.health == null || wish == null) return;
            foreach (string defName in wish.BenefitDefNames)
            {
                HediffDef def = DefDatabase<HediffDef>.GetNamedSilentFail(defName);
                Hediff hediff = def == null ? null : pawn.health.hediffSet.GetFirstHediffOfDef(def);
                if (hediff != null) pawn.health.RemoveHediff(hediff);
            }
        }

        private static Pawn FindPartner(Pawn pawn, List<Pawn> pawns)
        {
            return pawns.FirstOrDefault(other => other != pawn && pawn.relations != null &&
                (pawn.relations.DirectRelationExists(PawnRelationDefOf.Spouse, other) || pawn.relations.DirectRelationExists(PawnRelationDefOf.Lover, other)));
        }
        private static Pawn FindPawn(int id, List<Pawn> pawns) { return id < 0 ? null : pawns.FirstOrDefault(pawn => pawn.thingIDNumber == id); }
        private static void AnnounceWish(string owner, TravelWish wish)
        {
            BiomeDef biome = DefDatabase<BiomeDef>.GetNamedSilentFail(wish.BiomeDefName);
            Messages.Message(owner + " 渴望在" + wish.DesiredSeason + "前往“" + (biome?.label ?? wish.BiomeDefName) + "”旅居，并拥有一间足够床位的私人房间。", MessageTypeDefOf.NeutralEvent);
        }
    }
}
