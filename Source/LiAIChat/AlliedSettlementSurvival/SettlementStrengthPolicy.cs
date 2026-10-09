using System;

namespace LiAIChat.AlliedSettlementSurvival
{
    public static class SettlementStrengthPolicy
    {
        public const float InitialStrength = 60f;
        public const float NaturalRecoveryCap = 80f;
        public const float RecoveryPerDay = 0.5f;
        public const float MigrantStrength = 10f;
        public const int Radius = 30;
        public const int TicksPerDay = 60000;

        public static float Clamp(float value) => Math.Max(0f, Math.Min(100f, value));

        public static float Recover(float strength, int elapsedTicks, bool active, bool crisis)
        {
            strength = Clamp(strength);
            if (!active || crisis || strength <= 0f || strength >= NaturalRecoveryCap) return strength;
            return Math.Min(NaturalRecoveryCap, strength + Math.Max(0, elapsedTicks) * (RecoveryPerDay / TicksPerDay));
        }
    }
}
