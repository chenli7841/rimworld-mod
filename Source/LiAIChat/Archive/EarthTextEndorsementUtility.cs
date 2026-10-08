using LiAIChat.Models;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace LiAIChat.Archive
{
    public class Hediff_EarthTextEndorsement : Hediff
    {
        public string earthTextDefName;

        public override string LabelBase => EarthTextEndorsementUtility.IdentityLabel(earthTextDefName);

        public override string TipStringExtra
        {
            get
            {
                EarthTextDef text = DefDatabase<EarthTextDef>.GetNamedSilentFail(earthTextDefName);
                return text == null ? "对一部地球文献形成了鲜明、持久的认同。" :
                    "认同文献：《" + EarthTextEndorsementUtility.Title(text) + "》\n" + text.shortDescription;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref earthTextDefName, "earthTextDefName");
        }
    }

    public static class EarthTextEndorsementUtility
    {
        private const float FamiliarityThreshold = 0.70f;
        private const int MinimumSwitchInterval = 28 * GenDate.TicksPerDay;
        private const int MaximumSwitchInterval = 34 * GenDate.TicksPerDay;
        private const string HediffDefName = "LiAIChat_EarthTextEndorsement";

        public static void Update(Pawn pawn, PawnAIState state)
        {
            if (pawn == null || state == null || Find.TickManager == null)
                return;

            List<EarthTextDef> eligible = EligibleTexts(state);
            int now = Find.TickManager.TicksGame;
            EarthTextDef current = DefDatabase<EarthTextDef>.GetNamedSilentFail(state.EndorsedEarthTextId);
            bool currentEligible = current != null && eligible.Contains(current);

            if (eligible.Count == 0)
            {
                state.EndorsedEarthTextId = null;
                state.EndorsementSwitchTick = 0;
                SyncHediff(pawn, null);
                return;
            }

            if (!currentEligible || state.EndorsementSwitchTick <= now)
            {
                List<EarthTextDef> candidates = eligible.Where(text => text != current).ToList();
                if (candidates.Count == 0) candidates = eligible;
                EarthTextDef selected = candidates
                    .OrderByDescending(text => state.GetEarthTextFamiliarity(text.defName))
                    .ThenBy(text => text.defName)
                    .First();
                RecordStanceChange(state, current, selected, now);
                state.EndorsedEarthTextId = selected.defName;
                state.EndorsementSwitchTick = now + Rand.RangeInclusive(
                    MinimumSwitchInterval,
                    MaximumSwitchInterval);
                current = selected;
            }

            SyncHediff(pawn, current ?? DefDatabase<EarthTextDef>.GetNamedSilentFail(state.EndorsedEarthTextId));
        }

        public static void AppendConversationContext(StringBuilder builder, PawnAIState state)
        {
            if (builder == null || state == null || string.IsNullOrEmpty(state.EndorsedEarthTextId))
                return;

            EarthTextDef text = DefDatabase<EarthTextDef>.GetNamedSilentFail(state.EndorsedEarthTextId);
            if (text == null) return;

            builder.AppendLine();
            builder.AppendLine("PERSONAL LITERARY COMMITMENT (factual character state)");
            builder.AppendLine("- You identify as " + IdentityLabel(text.defName) + ".");
            builder.AppendLine("- You have deeply absorbed 《" + Title(text) + "》 by " + text.author + ".");
            builder.AppendLine("- Its central concerns: " + text.shortDescription);
            builder.AppendLine("- When relevant, take a clear, personally committed position shaped by this text. Do not default to a neutral compromise merely because several views are possible. You may be forceful or somewhat extreme, but remain a believable person rather than a slogan.");
            builder.AppendLine("- Actively support relevant answers by naming the text and paraphrasing its recovered passages. Use quotation marks only for exact words supplied below; never invent a quotation.");
            AppendStanceHistory(builder, state);

            DocumentRecovery.DocumentLine line = DocumentRecovery.GetLine(text.defName);
            RecoveredDocumentState recovered = DocumentRecovery.GetState(text.defName);
            if (line == null || recovered == null || recovered.UnlockedSectionIds == null || recovered.UnlockedSectionIds.Count == 0)
                return;

            builder.AppendLine("RECOVERED PASSAGES AVAILABLE FOR ACCURATE CITATION:");
            int count = Math.Min(2, recovered.UnlockedSectionIds.Count);
            for (int i = 0; i < count && i < line.Bodies.Length; i++)
            {
                string body = line.Bodies[i] ?? string.Empty;
                if (body.Length > 420) body = body.Substring(0, 420) + "…";
                builder.AppendLine("- " + line.Titles[i] + ": " + body);
            }
        }

        public static string IdentityLabel(string textDefName)
        {
            EarthTextDef text = DefDatabase<EarthTextDef>.GetNamedSilentFail(textDefName);
            string title = Title(text);
            switch (textDefName)
            {
                case "LiAIChat_Text_Kant_CritiqueOfPracticalReason": return "康德主义者";
                case "LiAIChat_Text_Kant_CritiqueOfPureReason": return "批判理性主义者";
                case "LiAIChat_Text_MarcusAurelius_Meditations": return "斯多葛主义者";
                case "LiAIChat_Text_Plato_Republic": return "柏拉图主义者";
                case "LiAIChat_Text_Aristotle_NicomacheanEthics": return "德性伦理主义者";
                case "LiAIChat_Text_Augustine_CityOfGod": return "奥古斯丁主义者";
                case "LiAIChat_Text_Rousseau_SocialContract": return "共和主义者";
                case "LiAIChat_Text_Smith_WealthOfNations": return "自由市场主义者";
                case "LiAIChat_Text_MarxEngels_CommunistManifesto": return "共产主义者";
                case "LiAIChat_Text_SunTzu_ArtOfWar": return "兵法主义者";
                default: return "《" + title + "》信奉者";
            }
        }

        public static string Title(EarthTextDef text)
        {
            if (text == null) return "未知文献";
            return !string.IsNullOrWhiteSpace(text.titleChinese) ? text.titleChinese : text.title;
        }

        private static List<EarthTextDef> EligibleTexts(PawnAIState state)
        {
            if (state.EarthTextFamiliarity == null) return new List<EarthTextDef>();
            return state.EarthTextFamiliarity
                .Where(pair => pair.Value >= FamiliarityThreshold)
                .Select(pair => DefDatabase<EarthTextDef>.GetNamedSilentFail(pair.Key))
                .Where(text => text != null)
                .ToList();
        }

        private static void RecordStanceChange(
            PawnAIState state,
            EarthTextDef previous,
            EarthTextDef current,
            int now)
        {
            if (current == null || (previous != null && previous.defName == current.defName))
                return;

            if (state.EarthTextStanceHistory == null)
                state.EarthTextStanceHistory = new List<EarthTextStanceChange>();

            state.EarthTextStanceHistory.Add(new EarthTextStanceChange
            {
                PreviousTextDefName = previous?.defName,
                CurrentTextDefName = current.defName,
                ChangedAtTick = now,
                PreviousFamiliarity = previous == null ? 0f : state.GetEarthTextFamiliarity(previous.defName),
                CurrentFamiliarity = state.GetEarthTextFamiliarity(current.defName)
            });
            if (state.EarthTextStanceHistory.Count > 6)
                state.EarthTextStanceHistory.RemoveRange(0, state.EarthTextStanceHistory.Count - 6);
        }

        private static void AppendStanceHistory(StringBuilder builder, PawnAIState state)
        {
            if (state.EarthTextStanceHistory == null || state.EarthTextStanceHistory.Count == 0)
                return;

            builder.AppendLine("RECORDED STANCE EVOLUTION:");
            foreach (EarthTextStanceChange change in state.EarthTextStanceHistory.Skip(
                Math.Max(0, state.EarthTextStanceHistory.Count - 3)))
            {
                EarthTextDef previous = DefDatabase<EarthTextDef>.GetNamedSilentFail(change.PreviousTextDefName);
                EarthTextDef current = DefDatabase<EarthTextDef>.GetNamedSilentFail(change.CurrentTextDefName);
                string before = previous == null ? "no earlier literary commitment" : IdentityLabel(previous.defName);
                string after = current == null ? "unknown" : IdentityLabel(current.defName);
                int daysAgo = Find.TickManager == null ? 0 : Mathf.Max(0, (Find.TickManager.TicksGame - change.ChangedAtTick) / GenDate.TicksPerDay);
                builder.AppendLine("- " + daysAgo + " days ago: " + before + " → " + after +
                    " (understanding " + change.PreviousFamiliarity.ToString("0.00") + " → " + change.CurrentFamiliarity.ToString("0.00") + ").");
            }
            builder.AppendLine("- If asked why your ideas changed, explain it as a personal intellectual journey: connect the contrast between the old and new texts with your temperament, present concerns, memories, and ordinary reading moments. You may create small, non-consequential reflective anecdotes, but do not fabricate major events, relationships, crimes, injuries, or named incidents.");
        }

        private static void SyncHediff(Pawn pawn, EarthTextDef text)
        {
            HediffDef def = DefDatabase<HediffDef>.GetNamedSilentFail(HediffDefName);
            if (def == null || pawn.health == null) return;
            Hediff existing = pawn.health.hediffSet.GetFirstHediffOfDef(def);
            if (text == null)
            {
                if (existing != null) pawn.health.RemoveHediff(existing);
                return;
            }

            Hediff_EarthTextEndorsement endorsement = existing as Hediff_EarthTextEndorsement;
            if (endorsement == null)
            {
                if (existing != null) pawn.health.RemoveHediff(existing);
                endorsement = HediffMaker.MakeHediff(def, pawn) as Hediff_EarthTextEndorsement;
                if (endorsement == null) return;
                pawn.health.AddHediff(endorsement);
            }
            endorsement.earthTextDefName = text.defName;
        }
    }
}
