using HarmonyLib;
using LiAIChat.Dialogue;
using LiAIChat.Social;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace LiAIChat
{
    [StaticConstructorOnStartup]
    public static class Bootstrap
    {
        static Bootstrap()
        {

            QuestScriptDef testQuest =
    DefDatabase<QuestScriptDef>.GetNamedSilentFail(
        "LiAIChat_TestArchiveQuest");

            if (testQuest == null)
            {
                Log.Error(
                    "[Li AI Chat] Test QuestScriptDef NOT loaded.");
            }
            else
            {
                Log.Message(
                    "[Li AI Chat] Test QuestScriptDef loaded successfully.");
            }

            Log.Message("[Li AI Chat] My mod loaded!");
            var harmony = new Harmony("li.rimworld.aichat");
            harmony.PatchAll();
            Log.Message("[Li AI Chat] Harmony patches applied!");
            ProactiveDialogueManager.Initialize(Config.Config.OpenAI_API_KEY);
            IntellectualExchangeManager.Initialize(Config.Config.OpenAI_API_KEY);
        }
    }
}
