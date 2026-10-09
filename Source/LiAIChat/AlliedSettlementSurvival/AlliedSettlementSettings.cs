using UnityEngine;
using Verse;

namespace LiAIChat.AlliedSettlementSurvival
{
    public sealed class AlliedSettlementSettings : ModSettings
    {
        public bool systemEnabled = true;
        public bool removeAtZero = true;
        public int radius = 30;
        public float initialStrength = 60f;
        public float recoveryCap = 80f;
        public float recoveryPerDay = 0.5f;
        public int protectionDays = 15;
        public int minimumCrisisIntervalDays = 20;
        public int maximumCrisisIntervalDays = 35;
        public int crisisDeadlineDays = 10;
        public int maximumConcurrentCrises = 2;
        public float failedCrisisStrengthLoss = 20f;
        public float foodNutrition = 150f;
        public int medicineUnits = 10;
        public float migrantStrength = 10f;
        public float slaveMigrantStrength = 5f;
        public float threatPoints = 500f;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref systemEnabled, "systemEnabled", true);
            Scribe_Values.Look(ref removeAtZero, "removeAtZero", true);
            Scribe_Values.Look(ref radius, "radius", 30);
            Scribe_Values.Look(ref initialStrength, "initialStrength", 60f);
            Scribe_Values.Look(ref recoveryCap, "recoveryCap", 80f);
            Scribe_Values.Look(ref recoveryPerDay, "recoveryPerDay", 0.5f);
            Scribe_Values.Look(ref protectionDays, "protectionDays", 15);
            Scribe_Values.Look(ref minimumCrisisIntervalDays, "minimumCrisisIntervalDays", 20);
            Scribe_Values.Look(ref maximumCrisisIntervalDays, "maximumCrisisIntervalDays", 35);
            Scribe_Values.Look(ref crisisDeadlineDays, "crisisDeadlineDays", 10);
            Scribe_Values.Look(ref maximumConcurrentCrises, "maximumConcurrentCrises", 2);
            Scribe_Values.Look(ref failedCrisisStrengthLoss, "failedCrisisStrengthLoss", 20f);
            Scribe_Values.Look(ref foodNutrition, "foodNutrition", 150f);
            Scribe_Values.Look(ref medicineUnits, "medicineUnits", 10);
            Scribe_Values.Look(ref migrantStrength, "migrantStrength", 10f);
            Scribe_Values.Look(ref slaveMigrantStrength, "slaveMigrantStrength", 5f);
            Scribe_Values.Look(ref threatPoints, "threatPoints", 500f);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
                Normalize();
        }

        public void Normalize()
        {
            radius = Mathf.Clamp(radius, 1, 100);
            initialStrength = Mathf.Clamp(initialStrength, 1f, 100f);
            recoveryCap = Mathf.Clamp(recoveryCap, 0f, 100f);
            recoveryPerDay = Mathf.Clamp(recoveryPerDay, 0f, 5f);
            protectionDays = Mathf.Clamp(protectionDays, 0, 120);
            minimumCrisisIntervalDays = Mathf.Clamp(minimumCrisisIntervalDays, 1, 365);
            maximumCrisisIntervalDays = Mathf.Clamp(maximumCrisisIntervalDays, minimumCrisisIntervalDays, 365);
            crisisDeadlineDays = Mathf.Clamp(crisisDeadlineDays, 1, 60);
            maximumConcurrentCrises = Mathf.Clamp(maximumConcurrentCrises, 1, 10);
            failedCrisisStrengthLoss = Mathf.Clamp(failedCrisisStrengthLoss, 1f, 100f);
            foodNutrition = Mathf.Clamp(foodNutrition, 1f, 10000f);
            medicineUnits = Mathf.Clamp(medicineUnits, 1, 1000);
            migrantStrength = Mathf.Clamp(migrantStrength, 1f, 100f);
            slaveMigrantStrength = Mathf.Clamp(slaveMigrantStrength, 1f, 100f);
            threatPoints = Mathf.Clamp(threatPoints, 300f, 10000f);
            AlliedSettlementRuntimeSettings.InitialStrength = initialStrength;
            AlliedSettlementRuntimeSettings.RecoveryCap = recoveryCap;
            AlliedSettlementRuntimeSettings.RecoveryPerDay = recoveryPerDay;
            AlliedSettlementRuntimeSettings.MigrantStrength = migrantStrength;
            AlliedSettlementRuntimeSettings.SlaveMigrantStrength = slaveMigrantStrength;
            AlliedSettlementRuntimeSettings.Radius = radius;
        }
    }

    public sealed class AlliedSettlementSurvivalMod : Mod
    {
        private static readonly AlliedSettlementSettings fallbackSettings = new AlliedSettlementSettings();
        private static AlliedSettlementSurvivalMod instance;
        private AlliedSettlementSettings settings;
        private Vector2 settingsScrollPosition;

        public static AlliedSettlementSettings Current => instance?.settings ?? fallbackSettings;

        public AlliedSettlementSurvivalMod(ModContentPack content) : base(content)
        {
            instance = this;
            settings = GetSettings<AlliedSettlementSettings>();
            settings.Normalize();
        }

        public override string SettingsCategory() => "LiASS_SettingsCategory".Translate();

        public override void DoSettingsWindowContents(Rect inRect)
        {
            settings.Normalize();
            var contentRect = new Rect(0f, 0f, inRect.width - 18f, 1050f);
            Widgets.BeginScrollView(inRect, ref settingsScrollPosition, contentRect);
            var listing = new Listing_Standard();
            listing.Begin(contentRect);

            listing.CheckboxLabeled("LiASS_Setting_Enabled".Translate(), ref settings.systemEnabled,
                "LiASS_Setting_EnabledTip".Translate());
            listing.CheckboxLabeled("LiASS_Setting_RemoveAtZero".Translate(), ref settings.removeAtZero,
                "LiASS_Setting_RemoveAtZeroTip".Translate());
            listing.GapLine();

            listing.Label("LiASS_SettingsScope".Translate());
            settings.radius = Mathf.RoundToInt(Slider(listing, "LiASS_Setting_Radius", settings.radius, 1f, 100f));
            listing.GapLine();

            listing.Label("LiASS_SettingsStrength".Translate());
            settings.initialStrength = Slider(listing, "LiASS_Setting_InitialStrength", settings.initialStrength, 1f, 100f);
            settings.recoveryCap = Slider(listing, "LiASS_Setting_RecoveryCap", settings.recoveryCap, 0f, 100f);
            settings.recoveryPerDay = Slider(listing, "LiASS_Setting_RecoveryPerDay", settings.recoveryPerDay, 0f, 5f);
            settings.migrantStrength = Slider(listing, "LiASS_Setting_MigrantStrength", settings.migrantStrength, 1f, 100f);
            settings.slaveMigrantStrength = Slider(listing, "LiASS_Setting_SlaveMigrantStrength", settings.slaveMigrantStrength, 1f, 100f);
            listing.GapLine();

            listing.Label("LiASS_SettingsCrises".Translate());
            settings.protectionDays = Mathf.RoundToInt(Slider(listing, "LiASS_Setting_ProtectionDays", settings.protectionDays, 0f, 120f));
            settings.minimumCrisisIntervalDays = Mathf.RoundToInt(Slider(listing, "LiASS_Setting_MinimumInterval", settings.minimumCrisisIntervalDays, 1f, 365f));
            settings.maximumCrisisIntervalDays = Mathf.RoundToInt(Slider(listing, "LiASS_Setting_MaximumInterval", settings.maximumCrisisIntervalDays, settings.minimumCrisisIntervalDays, 365f));
            settings.crisisDeadlineDays = Mathf.RoundToInt(Slider(listing, "LiASS_Setting_DeadlineDays", settings.crisisDeadlineDays, 1f, 60f));
            settings.maximumConcurrentCrises = Mathf.RoundToInt(Slider(listing, "LiASS_Setting_MaximumConcurrent", settings.maximumConcurrentCrises, 1f, 10f));
            settings.failedCrisisStrengthLoss = Slider(listing, "LiASS_Setting_FailedLoss", settings.failedCrisisStrengthLoss, 1f, 100f);
            listing.GapLine();

            listing.Label("LiASS_SettingsAid".Translate());
            settings.foodNutrition = Slider(listing, "LiASS_Setting_FoodNutrition", settings.foodNutrition, 1f, 1000f);
            settings.medicineUnits = Mathf.RoundToInt(Slider(listing, "LiASS_Setting_MedicineUnits", settings.medicineUnits, 1f, 100f));
            listing.GapLine();

            listing.Label("LiASS_SettingsThreat".Translate());
            settings.threatPoints = Slider(listing, "LiASS_Setting_ThreatPoints", settings.threatPoints, 300f, 5000f);
            listing.End();
            Widgets.EndScrollView();
            settings.Normalize();
        }

        private static float Slider(Listing_Standard listing, string key, float value, float min, float max)
        {
            string label = key.Translate(value.ToString("0.#"));
            return listing.SliderLabeled(label, value, min, max, 0.52f);
        }
    }
}
