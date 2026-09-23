using HarmonyLib;
using LiAIChat.Archive;
using LiAIChat.UI;
using RimWorld;
using RimWorld.QuestGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace LiAIChat.Patches
{
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetGizmos))]
    public static class PawnGizmoPatch
    {
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Pawn __instance)
        {
            
            foreach (Gizmo gizmo in __result)
            {
                yield return gizmo;
            }
            
            if (__instance == null)
            {
                yield break;
            }

            if (!__instance.RaceProps.Humanlike)
            {
                yield break;
            }

            if (__instance.IsColonistPlayerControlled)
            {
                yield return new Command_Action
                {
                    defaultLabel = "文明知识",
                    defaultDesc = "查看该殖民者的远古地球知识、主题与已研读文献。",
                    icon = LiAIChatTextures.StudyArchive,
                    action = () =>
                    {
                        Find.WindowStack.Add(
                            new Dialog_PawnCivilizationProfile(__instance));
                    }
                };
            }

            if (!PawnConversationEligibility
    .CanTalkToPlayer(__instance))
            {
                yield break;
            }

            yield return new Command_Action
            {
                defaultLabel = "Talk",
                defaultDesc = "Talk with this pawn using Li AI Chat.",
                icon = LiAIChatTextures.Talk,

                action = () =>
                {
                    Find.WindowStack.Add(
                        new LiAIChat.UI.Dialog_PawnChat(__instance)
                    );
                }
            };

            yield return new Command_Action
            {
                defaultLabel = "Identify Ancient Archive",
                defaultDesc = "Attempt to identify the contents of an unknown ancient Earth archive.",
                icon = LiAIChatTextures.StudyArchive,

                action = () =>
                {
                    AncientArchiveIdentificationUtility
                        .TryIdentifyArchive(__instance);
                }
            };
            if (Prefs.DevMode)
            {
                yield return new Command_Action
                {
                    defaultLabel = "DEBUG Archive Quest",

                    defaultDesc =
        "Generate the Li AI Chat archive test quest.",

                    action = () =>
                    {
                        GenerateArchiveTestQuest();
                    }
                };
            }
            if (Prefs.DevMode)
            {
                yield return new Command_Action
                {
                    defaultLabel = "DEBUG Scan Ancient Ruin",

                    defaultDesc =
                        "Scan the current map for Ancient Danger markers.",

                    action = () =>
                    {
                        AncientRuinDetector.ScanMap(
                            __instance.Map);
                    }
                };
            }
            if (Prefs.DevMode)
            {
                yield return new Command_Action
                {
                    defaultLabel =
                        "DEBUG Place Archive In Ruin",

                    defaultDesc =
                        "Place an Ancient Earth Archive "
                        + "inside an Ancient Danger room.",

                    action = () =>
                    {
                        AncientRuinDetector
                            .DebugPlaceArchiveInAncientRuin(
                                __instance.Map);
                    }
                };
            }
            if (Prefs.DevMode)
            {
                yield return new Command_Action
                {
                    defaultLabel = "DEBUG Archive Rarity Test",

                    defaultDesc =
                        "Test Ancient Archive content rarity weights.",

                    action = () =>
                    {
                        ArchiveContentSelectorDiagnostics.Run(
                            ArchiveSourceType.AncientPawn);

                        ArchiveContentSelectorDiagnostics.Run(
                            ArchiveSourceType.Trader);

                        ArchiveContentSelectorDiagnostics.Run(
                            ArchiveSourceType.QuestReward);

                        ArchiveContentSelectorDiagnostics.Run(
                            ArchiveSourceType.AncientRuin);
                    }
                };
            }
            
        }
        private static void GenerateArchiveTestQuest()
        {
            QuestScriptDef questDef =
                DefDatabase<QuestScriptDef>.GetNamed(
                    "LiAIChat_TestArchiveQuest");

            Slate slate = new Slate();

            Quest quest =
                QuestGen.Generate(
                    questDef,
                    slate);

            Log.Message(
                "[Li AI Chat] Generated quest: name="
                + quest.name
                + ", parts="
                + quest.PartsListForReading.Count);

            Find.QuestManager.Add(quest);

            Find.LetterStack.ReceiveLetter(
                "Ancient Earth Archive",
                "Information about a lost Ancient Earth archive has become available.",
                LetterDefOf.PositiveEvent,
                null,
                null,
                quest);

            Log.Message(
                "[Li AI Chat] Archive test quest added.");
        }
    }
}
