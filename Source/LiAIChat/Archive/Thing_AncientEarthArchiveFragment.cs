using LiAIChat.Civilization;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using Verse;
using System.Collections;
using LiAIChat.UI;

namespace LiAIChat.Archive
{
    public class Thing_AncientEarthArchiveFragment
        : Book
    {
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos()) yield return gizmo;
            if (!DocumentRecovery.CanRecover(this)) yield break;
            DocumentRecovery.DocumentLine line = DocumentRecovery.GetLine(EarthTextDefName);
            RecoveredDocumentState state = DocumentRecovery.GetState(EarthTextDefName);
            yield return new Command_Action { defaultLabel = "阅读已复原正文", defaultDesc = "查看已恢复的现代中文节选解说。", action = () => Find.WindowStack.Add(new Dialog_RecoveredTextReader(EarthTextDefName)) };
            if (state.UnlockedSectionIds.Count < line.Titles.Length && state.DeliveryTick < 0)
                yield return new Command_Action { defaultLabel = "委托复原下一节", defaultDesc = "支付 " + DocumentRecovery.GetCost(state.UnlockedSectionIds.Count) + " 单位黄金，约一天后送达。", action = () => DocumentRecovery.RequestNext(this) };
        }
        private ArchiveContentDef content;
        private bool soldByPlayer;

        public bool SoldByPlayer => soldByPlayer;

        public override void PreTraded(RimWorld.TradeAction action, Pawn playerNegotiator,
            RimWorld.ITrader trader)
        {
            base.PreTraded(action, playerNegotiator, trader);
            if (action == RimWorld.TradeAction.PlayerSells)
                soldByPlayer = true;
            else if (action == RimWorld.TradeAction.PlayerBuys)
            {
                soldByPlayer = false;
                EnsureMetadata();
            }
        }

        public ArchiveContentDef Content =>
            content;

        private ArchiveSourceType sourceType =
    ArchiveSourceType.Unknown;

        private string sourceDescription = "";

        private int discoveryTick = -1;

        public ArchiveSourceType SourceType =>
    sourceType;

        public string SourceDescription =>
            sourceDescription;

        public int DiscoveryTick =>
            discoveryTick;

        /// <summary>
        /// content 和 identified 是两件完全不同的事情。
        /// content = LiAIChat_Archive_KantIntroduction 而 identified = false 表示：
        /// 这块 Archive 客观上从生成时就是 Kant 文献，只是殖民者和玩家尚未识别出来。
        /// Identify() 绝对不要重新随机 content。
        /// </summary>
        private bool identified = false;

        public bool Identified =>
            identified;

        private string earthTextDefName;

        public EarthTextDef EarthText
        {
            get
            {
                if (string.IsNullOrEmpty(earthTextDefName))
                {
                    return null;
                }

                return DefDatabase<EarthTextDef>.GetNamedSilentFail(
                    earthTextDefName);
            }
        }

        public override bool IsReadable
        {
            get
            {
                return Identified;
            }
        }

        public string EarthTextDefName
        {
            get
            {
                return earthTextDefName;
            }
        }

        public void SetEarthText(EarthTextDef text)
        {
            if (text == null)
            {
                return;
            }

            // Identity is immutable once assigned.
            if (!string.IsNullOrEmpty(earthTextDefName))
            {
                return;
            }

            earthTextDefName = text.defName;
        }

        public string KnowledgeTopicId
        {
            get
            {
                if (EarthText != null &&
                    !string.IsNullOrWhiteSpace(
                        EarthText.primaryTopicId))
                {
                    return EarthText.primaryTopicId;
                }

                if (Content != null)
                {
                    return Content.topicId;
                }

                return null;
            }
        }

        public string StudyIdentityId
        {
            get
            {
                if (EarthText != null)
                {
                    return EarthText.defName;
                }

                if (Content != null)
                {
                    return Content.defName;
                }

                return null;
            }
        }

        public void Identify()
        {
            if (identified)
            {
                return;
            }

            identified = true;

            CivilizationKnowledgeEvaluator
                .NotifyLibraryMayHaveChanged();
        }

        /// <summary>
        /// Older trader inventories could contain a raw ThingDef instance rather
        /// than an archive created by AncientArchiveFactory. Repair only missing
        /// fields, so an already assigned document can never be changed.
        /// </summary>
        public void EnsureMetadata()
        {
            if (content == null)
            {
                ArchiveSourceType selectionSource = sourceType == ArchiveSourceType.Unknown
                    ? ArchiveSourceType.Trader
                    : sourceType;
                content = ArchiveContentSelector.SelectForSource(selectionSource);
            }

            if (string.IsNullOrEmpty(earthTextDefName))
                SetEarthText(EarthTextSelector.SelectRandom());
        }

        /// <summary>
        /// 新生成 Archive 时随机决定里面是什么内容。
        /// </summary>
        public override void PostMake()
        {
            base.PostMake();

            if (sourceType ==
                ArchiveSourceType.Unknown)
            {
                sourceType =
                    ArchiveSourceType.Debug;

                sourceDescription =
                    "Generated for testing.";

                discoveryTick =
                    Find.TickManager?.TicksGame ?? -1;
            }
        }

        /// <summary>
        /// 现在 Archive Thing 自己也需要保存状态。为什么？假设 Sarah 地图上有：Archive #123 → Kant 保存游戏。如果 Load 后重新随机：Archive #123 → Roman Empire那就很离谱。
        /// 确保：Save 时 Kant↓ Load 时仍然 Kant
        /// </summary>
        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Defs.Look(ref content, "archiveContent");
            Scribe_Values.Look(ref soldByPlayer, "archiveSoldByPlayer", false);
            Scribe_Values.Look(ref sourceType, "archiveSourceType", ArchiveSourceType.Unknown);
            Scribe_Values.Look(ref sourceDescription, "archiveSourceDescription", "");
            Scribe_Values.Look(ref discoveryTick, "archiveDiscoveryTick", -1);
            Scribe_Values.Look(ref identified, "archiveIdentified", false);
            Scribe_Values.Look(ref earthTextDefName, "earthTextDefName", null);
        }
        public override string LabelNoCount
        {
            get
            {
                if (!Identified)
                {
                    return "未知远古地球文献";
                }
                string earthTextDisplay = EarthText == null ? "" : EarthText.title;

                if (!string.IsNullOrEmpty(earthTextDisplay))
                {
                    return earthTextDisplay;
                }

                return "远古地球文献残片";
            }
        }
        public override string DescriptionDetailed
        {
            get
            {
                if (identified && EarthText != null)
                    return EarthText.description;

                // Book.DescriptionDetailed can contain a generated description saved
                // before identification (including legacy, text-specific descriptions).
                // Never expose that cache while this archive's identity is unknown.
                return def.description;
            }
        }
        public override string GetInspectString()
        {
            if (identified)
            {
                return EarthText == null ? "" : EarthText.shortDescription;
            }
            else
            {
                return "内容尚未被识别。";
            }
        }

        public void SetProvenance(
    ArchiveSourceType type,
    string description)
        {
            sourceType = type;

            sourceDescription =
                description ?? "";

            discoveryTick =
                Find.TickManager?.TicksGame ?? -1;
        }

        public void SetContent(ArchiveContentDef newContent)
        {
            content = newContent;
        }
    }
}
