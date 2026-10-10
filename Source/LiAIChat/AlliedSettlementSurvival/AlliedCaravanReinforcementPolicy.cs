using System;

namespace LiAIChat.AlliedSettlementSurvival
{
    public enum AlliedCaravanGuardRole
    {
        Guard,
        Elite,
        Leader
    }

    public static class AlliedCaravanReinforcementPolicy
    {
        public const float MaximumMilitaryAidStrengthLoss = 20f;
        public const float TopTierStrength = 90f;

        public static bool IsTopTier(float strength) => strength >= TopTierStrength;

        public static bool UsesTopTierCivilizedReinforcements(float strength, bool tribal) =>
            !tribal && IsTopTier(strength);

        public static int TopTierReinforcementCount(int playerColonistCount)
        {
            // Round up so every group of up to three player colonists can contribute
            // at least one reinforcement, including very small colonies.
            return Math.Max(1, (Math.Max(0, playerColonistCount) + 2) / 3);
        }

        public static int TribalAnimalCount(int playerColonistCount, bool thrumbo)
        {
            int divisor = thrumbo ? 4 : 3;
            return Math.Max(1, (Math.Max(0, playerColonistCount) + divisor - 1) / divisor);
        }

        public static int AdditionalGuardCount(float strength, bool tribal, int currentGuardCount, int tribalEliteTarget)
        {
            strength = Math.Max(0f, Math.Min(100f, strength));
            currentGuardCount = Math.Max(0, currentGuardCount);

            if (tribal)
            {
                if (strength < 50f) return 0;
                if (strength < 75f) return 4;
                if (strength < 90f) return Math.Max(0, 12 - currentGuardCount);
                int target = Math.Max(17, Math.Min(22, tribalEliteTarget));
                return Math.Max(0, target - currentGuardCount);
            }

            if (strength < 25f) return 0;
            if (strength < 50f) return 1;
            if (strength < 75f) return 2;
            if (strength < 90f) return 3;
            return 4;
        }

        public static int EliteGuardCount(float strength)
        {
            if (strength < 75f) return 0;
            return strength < 90f ? 1 : 2;
        }

        public static float DeathPenalty(AlliedCaravanGuardRole role)
        {
            switch (role)
            {
                case AlliedCaravanGuardRole.Leader: return 3f;
                case AlliedCaravanGuardRole.Elite: return 2f;
                default: return 1f;
            }
        }

        public static float CappedMilitaryAidDeathPenalty(
            float strengthLossAlreadyApplied, AlliedCaravanGuardRole role)
        {
            if (float.IsNaN(strengthLossAlreadyApplied) || float.IsInfinity(strengthLossAlreadyApplied))
                strengthLossAlreadyApplied = 0f;
            float remaining = Math.Max(0f,
                MaximumMilitaryAidStrengthLoss - Math.Max(0f, strengthLossAlreadyApplied));
            return Math.Min(DeathPenalty(role), remaining);
        }
    }
}
