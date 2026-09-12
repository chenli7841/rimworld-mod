using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using Verse;

namespace LiAIChat.Archive
{
    public class Thing_AncientEarthArchiveFragment
        : ThingWithComps
    {
        private ArchiveContentDef content;

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
            identified = true;
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

        private void AssignRandomContent()
        {
            var allContents =
                DefDatabase<ArchiveContentDef>
                    .AllDefsListForReading;

            if (allContents == null ||
                allContents.Count == 0)
            {
                Log.Warning(
                    "[Li AI Chat] No ArchiveContentDefs available.");

                return;
            }

            content =
                allContents.RandomElement();
        }

        /// <summary>
        /// 现在 Archive Thing 自己也需要保存状态。为什么？假设 Sarah 地图上有：Archive #123 → Kant 保存游戏。如果 Load 后重新随机：Archive #123 → Roman Empire那就很离谱。
        /// 确保：Save 时 Kant↓ Load 时仍然 Kant
        /// </summary>
        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Defs.Look(ref content, "archiveContent");
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
                    return "Unknown Ancient Earth Archive";
                }
                string earthTextDisplay = GetEarthTextDisplayText();

                if (!string.IsNullOrEmpty(earthTextDisplay))
                {
                    return earthTextDisplay;
                }

                return "Ancient Earth Archive Fragment";
            }
        }
        public override string GetInspectString()
        {
            string baseText =
                base.GetInspectString();

            List<string> lines =
                new List<string>();

            if (content != null && identified)
            {
                string earthTextDisplay = GetEarthTextDisplayText();

                if (!string.IsNullOrEmpty(earthTextDisplay))
                {
                    if (!string.IsNullOrEmpty(baseText))
                    {
                        baseText += "\n";
                    }

                    baseText += earthTextDisplay;
                }
            }
            if (!string.IsNullOrWhiteSpace(
                baseText))
            {
                lines.Add(baseText);
            }

            if (content != null)
            {
                if (identified)
                {
                    lines.Add(
                        "Archive: " +
                        content.title);
                }
                else
                {
                    lines.Add(
                        "Archive: Unknown");

                    string difficulty =
                        ArchiveIdentificationCalculator
                            .GetDifficultyLabel(this);

                    lines.Add(
                        "Identification difficulty: " +
                        difficulty);
                }
            }

            if (!string.IsNullOrWhiteSpace(
                sourceDescription))
            {
                lines.Add(
                    "Origin: " +
                    sourceDescription);
            }

            return string.Join(
                "\n",
                lines);
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

        public void SetContent(
    ArchiveContentDef newContent)
        {
            content = newContent;
        }

        public string GetEarthTextDisplayText()
        {
            EarthTextDef text = EarthText;

            if (text == null)
            {
                return null;
            }

            string result = text.title;

            if (!string.IsNullOrEmpty(text.titleChinese))
            {
                result += "\n" + text.titleChinese;
            }

            if (!string.IsNullOrEmpty(text.author))
            {
                result += "\n\n" + text.author;
            }

            if (!string.IsNullOrEmpty(text.YearDisplay))
            {
                result += "\n" + text.YearDisplay;
            }

            return result;
        }
    }
}