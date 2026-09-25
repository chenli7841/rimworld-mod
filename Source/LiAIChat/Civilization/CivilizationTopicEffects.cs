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
        NegativeSocialMemory, ColonyMood, GatheringMood, ReadingGain
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
            foreach (var state in component.Active)
                if (state.effect == effect) factor *= state.factor;
            return factor;
        }

        public static float MoodOffset(Pawn pawn)
        {
            var component = CivilizationTopicGameComponent.Instance;
            if (!Eligible(pawn) || component == null) return 0f;
            return component.Active.Where(s => s.effect == CivilizationTopicEffect.ColonyMood).Sum(s => s.offset);
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
