using LiAIChat.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace LiAIChat.Events
{
    public static class ColonyEventLog
    {
        private const int MaximumRecords = 40;
        private const int DuplicateWindowTicks = 60000;
        private const int OrdinaryRetentionTicks = 1200000;
        private const int ImportantRetentionTicks = 3600000;

        public static void Record(string category, string summary, int importance = 1,
            Pawn relatedPawn = null, string deduplicationKey = null)
        {
            if (Current.Game == null || string.IsNullOrWhiteSpace(summary)) return;
            LiAIChatGameComponent component = Current.Game.GetComponent<LiAIChatGameComponent>();
            if (component == null) return;
            if (component.ColonyEvents == null) component.ColonyEvents = new List<ColonyEventRecord>();

            int now = Find.TickManager == null ? -1 : Find.TickManager.TicksGame;
            string key = string.IsNullOrWhiteSpace(deduplicationKey) ? (category ?? "event") + ":" + summary : deduplicationKey;
            ColonyEventRecord duplicate = component.ColonyEvents.LastOrDefault(record => record != null && record.DeduplicationKey == key &&
                (now < 0 || record.CreatedTick < 0 || now - record.CreatedTick < DuplicateWindowTicks));
            if (duplicate != null) { duplicate.CreatedTick = now; return; }

            component.ColonyEvents.Add(new ColonyEventRecord
            {
                Category = category ?? "事件", Summary = Truncate(summary.Trim(), 180), DeduplicationKey = key,
                Importance = Math.Max(1, Math.Min(3, importance)), CreatedTick = now,
                RelatedPawnId = relatedPawn == null ? -1 : relatedPawn.thingIDNumber
            });
            Prune(component.ColonyEvents, now);
        }

        public static string BuildConversationContext(Pawn pawn, int maximumEvents = 5)
        {
            if (Current.Game == null) return string.Empty;
            LiAIChatGameComponent component = Current.Game.GetComponent<LiAIChatGameComponent>();
            if (component?.ColonyEvents == null || component.ColonyEvents.Count == 0) return string.Empty;
            int now = Find.TickManager == null ? -1 : Find.TickManager.TicksGame;
            List<ColonyEventRecord> events = component.ColonyEvents.Where(record => record != null && !string.IsNullOrWhiteSpace(record.Summary))
                .OrderByDescending(record => Relevance(record, pawn, now)).Take(maximumEvents).ToList();
            if (events.Count == 0) return string.Empty;

            StringBuilder context = new StringBuilder();
            context.AppendLine("RECENT COLONY EVENTS");
            context.AppendLine("These are factual colony-wide developments. Mention them only when naturally relevant to the conversation.");
            foreach (ColonyEventRecord record in events) context.AppendLine("- " + DescribeAge(record, now) + ": " + record.Summary);
            return context.ToString();
        }

        private static double Relevance(ColonyEventRecord record, Pawn pawn, int now)
        {
            double score = record.Importance * 10000000d;
            if (pawn != null && record.RelatedPawnId == pawn.thingIDNumber) score += 5000000d;
            if (now >= 0 && record.CreatedTick >= 0) score -= Math.Min(now - record.CreatedTick, 4000000);
            return score;
        }
        private static string DescribeAge(ColonyEventRecord record, int now)
        {
            if (now < 0 || record.CreatedTick < 0) return "Recently";
            int days = Math.Max(0, (now - record.CreatedTick) / 60000);
            return days == 0 ? "Today" : days + " days ago";
        }
        private static void Prune(List<ColonyEventRecord> records, int now)
        {
            if (records == null) return;
            if (now >= 0) records.RemoveAll(record => record == null || (record.CreatedTick >= 0 && now - record.CreatedTick >
                (record.Importance >= 3 ? ImportantRetentionTicks : OrdinaryRetentionTicks)));
            if (records.Count > MaximumRecords) records.RemoveRange(0, records.Count - MaximumRecords);
        }
        private static string Truncate(string value, int maximumLength)
        {
            return value.Length <= maximumLength ? value : value.Substring(0, maximumLength - 1) + "…";
        }
    }
}
