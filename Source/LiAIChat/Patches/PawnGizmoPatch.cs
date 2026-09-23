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
using Verse.AI;

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

                Pawn annotator = __instance.Map?.mapPawns.AllPawnsSpawned.FirstOrDefault(p =>
                {
                    LiAIChat.Models.PawnAIState state;
                    return LiAIChat.Questing.LostAnnotatorRecruitmentUtility.IsCandidate(p, out state);
                });
                if (annotator != null)
                {
                    Thing_AncientEarthArchiveFragment book =
                        LiAIChat.Questing.LostAnnotatorRecruitmentUtility.FindBook(__instance.Map);
                    bool valid = book != null && !__instance.Drafted &&
                        __instance.CanReserveAndReach(book, PathEndMode.Touch, Danger.Some) &&
                        __instance.CanReserve(annotator);
                    yield return new Command_Action
                    {
                        defaultLabel = "与注疏者共同解读",
                        defaultDesc = valid ? "与暂住学者共同解读一本文献。这是邀请他永久留下的条件之一。" :
                            "需要一本文献、可行动的殖民者与暂住学者。",
                        icon = LiAIChatTextures.StudyArchive,
                        action = () =>
                        {
                            if (!valid)
                            {
                                Messages.Message("暂时无法开始共同解读。", MessageTypeDefOf.RejectInput);
                                return;
                            }
                            __instance.jobs.TryTakeOrderedJob(JobMaker.MakeJob(
                                DefDatabase<JobDef>.GetNamed("LiAIChat_LostAnnotatorJointStudy"), book, annotator));
                        }
                    };
                }
            }

            var lostAnnotatorState = LiAIChat.State.PawnAIStateManager.TryGetExistingState(__instance);
            if (lostAnnotatorState != null && lostAnnotatorState.IsLostAnnotator &&
                !lostAnnotatorState.LostAnnotatorRescued && __instance.Map != null && !__instance.Map.IsPlayerHome)
            {
                yield return new Command_Action
                {
                    defaultLabel = "接应失落注疏者",
                    defaultDesc = "接应这名被困学者。他会加入你的队伍，可随商队一同返回殖民地。",
                    icon = LiAIChatTextures.StudyArchive,
                    action = () =>
                    {
                        bool defendersRemain = __instance.Map.mapPawns.AllPawnsSpawned.Any(p =>
                            !p.Dead && p.Faction != null && p.Faction.HostileTo(Faction.OfPlayer));
                        if (defendersRemain)
                        {
                            Messages.Message("敌对守卫仍在看守营地。学者拒绝在文献未获安全前离开。", __instance, MessageTypeDefOf.RejectInput);
                            return;
                        }
                        __instance.SetFaction(Faction.OfPlayer);
                        __instance.jobs.EndCurrentJob(JobCondition.InterruptForced);
                        if (__instance.guest != null)
                            __instance.guest.SetGuestStatus(null, GuestStatus.Guest);
                        Messages.Message(__instance.LabelShort + " 接受了接应，并加入队伍准备返程。", __instance, MessageTypeDefOf.PositiveEvent);
                    }
                };
            }

            LiAIChat.Models.PawnAIState recruitmentState;
            if (LiAIChat.Questing.LostAnnotatorRecruitmentUtility.IsCandidate(__instance, out recruitmentState) &&
                __instance.Map != null && __instance.Map.IsPlayerHome)
            {
                bool readyToStay = LiAIChat.Questing.LostAnnotatorRecruitmentUtility.MeetsRequirements(__instance);
                yield return new Command_Action
                {
                    defaultLabel = "邀请学者永久留下",
                    defaultDesc = LiAIChat.Questing.LostAnnotatorRecruitmentUtility.RequirementSummary(__instance),
                    icon = LiAIChatTextures.StudyArchive,
                    action = () =>
                    {
                        if (!readyToStay)
                        {
                            Messages.Message("尚未满足学者留下的条件。", MessageTypeDefOf.RejectInput);
                            return;
                        }
                        recruitmentState.LostAnnotatorPermanentMember = true;
                        if (recruitmentState.ScholarStay != null) recruitmentState.ScholarStay.Active = false;
                        Messages.Message(__instance.LabelShort + " 接受了邀请，决定以学者身份留在殖民地。", __instance, MessageTypeDefOf.PositiveEvent);
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
