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
    }
}
