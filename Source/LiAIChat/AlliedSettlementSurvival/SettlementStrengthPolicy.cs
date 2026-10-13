using System;

namespace LiAIChat.AlliedSettlementSurvival
{
    public static class AlliedSettlementRuntimeSettings
    {
        public static float InitialStrength = 60f;
        public static float RecoveryCap = 80f;
        public static float RecoveryPerDay = 0.5f;
        public static float MigrantStrength = 10f;
        public static float SlaveMigrantStrength = 5f;
        public static int Radius = 30;
    }

    public static class SettlementStrengthPolicy
    {
        public static float InitialStrength => AlliedSettlementRuntimeSettings.InitialStrength;
        public static float NaturalRecoveryCap => AlliedSettlementRuntimeSettings.RecoveryCap;
        public static float RecoveryPerDay => AlliedSettlementRuntimeSettings.RecoveryPerDay;
        public static float MigrantStrength => AlliedSettlementRuntimeSettings.MigrantStrength;
        public static float SlaveMigrantStrength => AlliedSettlementRuntimeSettings.SlaveMigrantStrength;
        public static int Radius => AlliedSettlementRuntimeSettings.Radius;
        public const int TicksPerDay = 60000;

        public static float Clamp(float value) => Math.Max(0f, Math.Min(100f, value));

        public static float AidRequirementMultiplier(float strength)
        {
            strength = Clamp(strength);
            if (strength < 25f) return 1f;
            if (strength < 50f) return 1.25f;
            if (strength < 75f) return 1.5f;
            if (strength < 90f) return 1.75f;
            return 2f;
        }

        public static float ScaleFoodAidRequirement(float baseNutrition, float strength) =>
            Math.Max(1f, baseNutrition) * AidRequirementMultiplier(strength);

        public static int ScaleMedicineAidRequirement(int baseUnits, float strength) =>
            (int)Math.Ceiling(Math.Max(1, baseUnits) * AidRequirementMultiplier(strength));

        public static float Recover(float strength, int elapsedTicks, bool active, bool crisis)
        {
            strength = Clamp(strength);
            if (!active || crisis || strength <= 0f || strength >= NaturalRecoveryCap) return strength;
            return Math.Min(NaturalRecoveryCap, strength + Math.Max(0, elapsedTicks) * (RecoveryPerDay / TicksPerDay));
        }
    }
}
