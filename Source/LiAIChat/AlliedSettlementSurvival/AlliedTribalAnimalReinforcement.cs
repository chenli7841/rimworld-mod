using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace LiAIChat.AlliedSettlementSurvival
{
    public static class AlliedTribalAnimalReinforcement
    {
        public static List<Pawn> Generate(Faction faction, PlanetTile tile, int playerColonistCount)
        {
            var candidates = new List<PawnKindDef>();
            PawnKindDef thrumbo = DefDatabase<PawnKindDef>.GetNamedSilentFail("Thrumbo");
            PawnKindDef elephant = DefDatabase<PawnKindDef>.GetNamedSilentFail("Elephant");
            if (thrumbo != null) candidates.Add(thrumbo);
            if (elephant != null) candidates.Add(elephant);
            if (faction == null || candidates.Count == 0) return new List<Pawn>();

            PawnKindDef kind = candidates.RandomElement();
            bool isThrumbo = kind == thrumbo;
            int count = AlliedCaravanReinforcementPolicy.TribalAnimalCount(playerColonistCount, isThrumbo);
            var pawns = new List<Pawn>(count);
            try
            {
                for (int i = 0; i < count; i++)
                {
                    Pawn animal = PawnGenerator.GeneratePawn(kind, faction, tile);
                    if (animal != null) pawns.Add(animal);
                }
            }
            catch (Exception error)
            {
                Log.Error("[LiAIChat] Could not generate allied tribal animal reinforcements: " + error);
            }

            if (pawns.Count > 0)
                Log.Message("[LiAIChat] Added " + pawns.Count + " " + kind.LabelCap +
                    " to allied tribal reinforcements.");
            return pawns;
        }
    }
}
