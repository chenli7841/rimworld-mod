using Verse;

namespace LiAIChat.Refugees
{
    public static class ArchiveRefugeeArchiveDecisionUtility
    {
        public static bool Decide(
            ArchiveRefugeeGroupState group)
        {
            if (group == null)
            {
                return false;
            }

            if (group.ArchiveDispositionDecided)
            {
                return group.WillLeaveArchive;
            }

            bool result =
                Evaluate(group);

            group.WillLeaveArchive =
                result;

            group.ArchiveDispositionDecided =
                true;

            Log.Message(
                "[Li AI Chat] Refugee archive disposition decided: " +
                "group=" +
                group.GroupId +
                ", leaveArchive=" +
                result +
                ", hospitality=" +
                group.HospitalityImpression.ToString("0.00") +
                ", meaningful=" +
                group.MeaningfulConversationCount +
                ", situation=" +
                group.SituationType +
                ", event=" +
                GetEventType(group));

            return result;
        }


        private static bool Evaluate(
            ArchiveRefugeeGroupState group)
        {
            float hospitality =
                group.HospitalityImpression;

            int meaningful =
                group.MeaningfulConversationCount;


            // -------------------------------------------------
            // Strongly negative relationship:
            // they keep the Archive.
            // -------------------------------------------------

            if (hospitality <= -0.40f)
            {
                return false;
            }


            // -------------------------------------------------
            // Strong positive relationship:
            // high confidence they leave it.
            // -------------------------------------------------

            if (hospitality >= 0.45f)
            {
                return true;
            }


            // -------------------------------------------------
            // Moderate positive relationship plus
            // meaningful engagement.
            // -------------------------------------------------

            if (hospitality >= 0.20f &&
                meaningful >= 1)
            {
                return true;
            }


            // -------------------------------------------------
            // Several meaningful conversations can compensate
            // for a merely neutral relationship.
            // -------------------------------------------------

            if (meaningful >= 2 &&
                hospitality >= -0.05f)
            {
                return true;
            }


            // -------------------------------------------------
            // Situation-specific small modifiers.
            // These do not override major trust problems.
            // -------------------------------------------------

            float contextualScore =
                CalculateContextualScore(group);

            float finalScore =
                hospitality +
                contextualScore;

            if (meaningful >= 1)
            {
                finalScore += 0.10f;
            }

            return finalScore >= 0.25f;
        }


        private static float CalculateContextualScore(
            ArchiveRefugeeGroupState group)
        {
            float score = 0f;


            // -------------------------------------------------
            // Background
            // -------------------------------------------------

            switch (group.SituationType)
            {
                case "FailedSettlement":

                    // They may value the possibility that
                    // something survives even if their own
                    // settlement did not.
                    score += 0.05f;
                    break;


                case "LongTermWanderers":

                    // Long-term wanderers may be slightly less
                    // attached to physical possession if they
                    // trust a stable place.
                    score += 0.05f;
                    break;


                case "DisplacedFamily":

                    // Keep neutral.
                    break;


                case "ScatteredSurvivors":

                    // More cautious about relinquishing one of
                    // the few things they still possess.
                    score -= 0.05f;
                    break;
            }


            // -------------------------------------------------
            // Resource condition
            // -------------------------------------------------

            switch (group.ResourceCondition)
            {
                case "Stable":
                    score += 0.03f;
                    break;

                case "Critical":
                    score -= 0.05f;
                    break;
            }


            // -------------------------------------------------
            // Stay event
            // -------------------------------------------------

            if (group.StayEvent != null &&
                group.StayEvent.Triggered)
            {
                switch (group.StayEvent.EventType)
                {
                    case "ArchiveAnxiety":

                        // If archive anxiety was never resolved,
                        // they remain more protective.
                        if (!group.StayEvent.Resolved)
                        {
                            score -= 0.10f;
                        }

                        break;


                    case "FutureUncertainty":

                        // Neutral for ownership.
                        break;


                    case "GroupTension":

                        // Slightly less willing to make a major
                        // irreversible collective decision.
                        if (!group.StayEvent.Resolved)
                        {
                            score -= 0.05f;
                        }

                        break;
                }
            }


            return score;
        }


        private static string GetEventType(
            ArchiveRefugeeGroupState group)
        {
            if (group == null ||
                group.StayEvent == null)
            {
                return "none";
            }

            return group.StayEvent.EventType;
        }
    }
}