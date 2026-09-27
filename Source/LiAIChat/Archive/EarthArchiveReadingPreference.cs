using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace LiAIChat.Archive
{
    public static class EarthArchiveReadingPreference
    {
        // Intellectual 0/5/10/15/20 corresponds to a 10/33/55/78/100%
        // chance to replace a normal leisure-reading choice with an archive.
        public static float PreferenceFor(Pawn pawn)
        {
            int intellectual = pawn?.skills?.GetSkill(SkillDefOf.Intellectual)?.Level ?? 0;
            return Mathf.Clamp01(0.10f + intellectual * 0.045f);
        }

        public static Book FindReadableArchive(Pawn pawn)
        {
            if (pawn?.Map == null)
            {
                return null;
            }

            List<Thing_AncientEarthArchiveFragment> candidates =
                pawn.Map.listerThings.ThingsOfDef(
                    ThingDefOfArchive.LiAIChat_AncientEarthArchiveFragment)
                    .OfType<Thing_AncientEarthArchiveFragment>()
                    .ToList();

            // Books in a bookcase are held rather than listed on the map.
            foreach (Building_Bookcase bookcase in
                pawn.Map.listerThings.AllThings.OfType<Building_Bookcase>())
            {
                candidates.AddRange(bookcase.HeldBooks
                    .OfType<Thing_AncientEarthArchiveFragment>());
            }

            string reason;
            return candidates
                .Where(book => book != null && book.Identified &&
                    BookUtility.CanReadBook(book, pawn, out reason))
                .InRandomOrder()
                .FirstOrDefault();
        }
    }

    // Runs after vanilla has applied the pawn's reading policy and verified
    // that leisure reading is possible. We only exchange the selected book,
    // so unavailable archives and normal policy restrictions still fall back
    // to the game's usual novel and entertainment-book selection.
    [HarmonyPatch(typeof(BookUtility), "TryGetRandomBookToRead")]
    public static class EarthArchiveReadingPreferencePatch
    {
        public static void Postfix(Pawn pawn, ref Book book, ref bool __result)
        {
            if (!__result || book == null || pawn == null ||
                !Rand.Chance(EarthArchiveReadingPreference.PreferenceFor(pawn)))
            {
                return;
            }

            Book archive = EarthArchiveReadingPreference.FindReadableArchive(pawn);
            if (archive != null)
            {
                book = archive;
            }
        }
    }
}
