using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace LiAIChat.Civilization
{
    public enum CivilizationTopicEffect
    {
        Stat, SocialFight, MoodDecline, InjuredMoodRecovery, PainMood,
        NegativeSocialMemory, ColonyMood, GatheringMood, ReadingGain,
        WeaponRange, ClearMentalBreak
    }

    public static class CivilizationTopicEffects
    {
        public static bool Eligible(Pawn pawn)
        {
            return pawn != null && pawn.IsColonistPlayerControlled && !pawn.Dead;
        }

        public static float Factor(Pawn pawn, CivilizationTopicEffect effect)
        {
            var component = CivilizationTopicGameComponent.Instance;
            if (!Eligible(pawn) || component == null) return 1f;
            float factor = 1f;
            foreach (var bonus in component.ActiveBonuses)
                if (bonus.effect == effect) factor *= bonus.factor;
            return factor;
        }

        public static float MoodOffset(Pawn pawn)
        {
            var component = CivilizationTopicGameComponent.Instance;
            if (!Eligible(pawn) || component == null) return 0f;
            return component.ActiveBonuses.Where(b => b.effect == CivilizationTopicEffect.ColonyMood).Sum(b => b.offset);
        }

        public static int ApplyImmediateEffects(
            System.Collections.Generic.IEnumerable<CivilizationTopicBonus> bonuses)
        {
            if (bonuses == null || !bonuses.Any(b => b.effect == CivilizationTopicEffect.ClearMentalBreak))
            {
                return 0;
            }

            int recovered = 0;
            foreach (Pawn pawn in
                PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists)
            {
                if (!pawn.InMentalState || pawn.MentalState == null)
                {
                    continue;
                }

                pawn.MentalState.RecoverFromState();
                recovered++;
            }

            return recovered;
        }
    }

    [HarmonyPatch(typeof(Pawn_InteractionsTracker), "SocialFightChance")]
    public static class CivilizationTopicSocialFightPatch
    {
        public static void Postfix(Pawn ___pawn, Pawn initiator, ref float __result)
        {
            // Apply once even when both participants benefit from the colony effect.
            __result *= Mathf.Min(CivilizationTopicEffects.Factor(___pawn, CivilizationTopicEffect.SocialFight),
                CivilizationTopicEffects.Factor(initiator, CivilizationTopicEffect.SocialFight));
        }
    }

    [HarmonyPatch(typeof(Need_Mood), "NeedInterval")]
    public static class CivilizationTopicMoodChangePatch
    {
        public static void Prefix(Need_Mood __instance, out float __state) { __state = __instance.CurLevel; }

        public static void Postfix(Need_Mood __instance, Pawn ___pawn, float __state)
        {
            if (!CivilizationTopicEffects.Eligible(___pawn)) return;
            float delta = __instance.CurLevel - __state;
            if (delta < 0f)
                __instance.CurLevel = __state + delta * CivilizationTopicEffects.Factor(___pawn, CivilizationTopicEffect.MoodDecline);
            else if (delta > 0f)
            {
                float recovery = CivilizationTopicEffects.Factor(___pawn, CivilizationTopicEffect.InjuredMoodRecovery);
                if (recovery > 1f && ___pawn.health.hediffSet.hediffs.Any(h => h is Hediff_Injury && !h.IsPermanent()))
                    __instance.CurLevel = Mathf.Min(__instance.CurInstantLevel, __state + delta * recovery);
            }
        }
    }

    [HarmonyPatch(typeof(Thought), "MoodOffset")]
    public static class CivilizationTopicPainPatch
    {
        public static void Postfix(Thought __instance, ref float __result)
        {
            if (__result < 0f && __instance.def.defName == "Pain")
                __result *= CivilizationTopicEffects.Factor(__instance.pawn, CivilizationTopicEffect.PainMood);
        }
    }

    [HarmonyPatch(typeof(Thought_MemorySocial), "OpinionOffset")]
    public static class CivilizationTopicSocialMemoryPatch
    {
        public static void Postfix(Thought_MemorySocial __instance, ref float __result)
        {
            if (__result < 0f)
                __result *= CivilizationTopicEffects.Factor(__instance.pawn, CivilizationTopicEffect.NegativeSocialMemory);
        }
    }

    [HarmonyPatch(typeof(Thought_Memory), "MoodOffset")]
    public static class CivilizationTopicGatheringPatch
    {
        public static void Postfix(Thought_Memory __instance, ref float __result)
        {
            if (__result <= 0f) return;
            string name = __instance.def.defName;
            if (__instance is Thought_AttendedRitual || name == "AttendedParty"
                || name == "AttendedWedding" || name == "AttendedConcert")
                __result *= CivilizationTopicEffects.Factor(__instance.pawn, CivilizationTopicEffect.GatheringMood);
        }
    }

    [HarmonyPatch(typeof(VerbProperties), "AdjustedRange")]
    public static class CivilizationTopicWeaponRangePatch
    {
        public static void Postfix(VerbProperties __instance, Thing attacker, ref float __result)
        {
            if (__instance.Ranged)
            {
                __result *= CivilizationTopicEffects.Factor(
                    attacker as Pawn,
                    CivilizationTopicEffect.WeaponRange);
            }
        }
    }

    public class ThoughtWorker_CivilizationTopic : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            return CivilizationTopicEffects.MoodOffset(p) > 0f;
        }
    }

    public class Thought_CivilizationTopic : Thought_Situational
    {
        public override float MoodOffset() { return CivilizationTopicEffects.MoodOffset(pawn); }
    }
}
