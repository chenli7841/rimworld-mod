using LiAIChat.Models;

namespace LiAIChat.Archive
{
    public static class ArchiveScholarProactiveDialogue
    {
        public const string TriggerArrival =
            "scholar_arrival";

        public const string TriggerFarewell =
            "scholar_farewell";


        public static string TryGetTrigger(
            PawnContext pawnContext,
            PawnAIState state)
        {
            if (pawnContext == null ||
                state == null ||
                state.ScholarProfile == null ||
                state.ScholarStay == null)
            {
                return null;
            }

            ArchiveScholarStayState stay =
                state.ScholarStay;

            if (!stay.Active)
            {
                return null;
            }

            string arrivalTrigger =
                TryGetArrivalTrigger(
                    pawnContext,
                    stay);

            if (!string.IsNullOrEmpty(
                arrivalTrigger))
            {
                return arrivalTrigger;
            }

            string farewellTrigger =
    TryGetFarewellTrigger(
        pawnContext,
        stay);

            if (!string.IsNullOrEmpty(
                farewellTrigger))
            {
                ArchiveScholarGiftDecisionUtility
                    .Decide(state);
            }

            return farewellTrigger;
        }


        public static string BuildPromptInstruction(
            string trigger,
            PawnAIState state)
        {
            switch (trigger)
            {
                case TriggerArrival:
                    return
                        "You have recently been given temporary shelter " +
                        "by this colony. Start a natural conversation " +
                        "about why you have spent years preserving " +
                        "ancient Earth knowledge and why that work " +
                        "matters to you personally. " +
                        "Speak as yourself, not as a quest narrator. " +
                        "Do not simply explain game mechanics.";

                case TriggerFarewell:
                    {
                        if (state != null &&
                            state.ScholarStay != null &&
                            state.ScholarStay.ArchiveGiftDecided)
                        {
                            if (state.ScholarStay.WillGiftArchive)
                            {
                                return
                                    "Your temporary stay with this colony is nearing " +
                                    "its end. Start a natural farewell conversation. " +
                                    "Reflect on the colony's hospitality and your " +
                                    "experience during the stay. You have decided to " +
                                    "leave your Ancient Earth Archive with the colony " +
                                    "before departing. Speak about that decision " +
                                    "naturally and personally. Do not speak as a quest narrator.";
                            }

                            return
                                "Your temporary stay with this colony is nearing " +
                                "its end. Start a natural farewell conversation. " +
                                "Reflect on the colony's hospitality and your " +
                                "experience during the stay. You have decided to " +
                                "keep carrying your Ancient Earth Archive with you " +
                                "when you leave. Explain that decision naturally if " +
                                "appropriate, without sounding punitive or mechanical. " +
                                "Do not speak as a quest narrator.";
                        }

                        return
                            "Your temporary stay with this colony is nearing its end. " +
                            "Start a natural farewell conversation reflecting on your stay. " +
                            "Do not make a definite claim about whether the Ancient Earth " +
                            "Archive will be left behind.";
                    }

                default:
                    return null;
            }
        }


        public static void MarkTriggered(
            PawnAIState state,
            string trigger)
        {
            if (state == null ||
                state.ScholarStay == null ||
                string.IsNullOrEmpty(trigger))
            {
                return;
            }

            switch (trigger)
            {
                case TriggerArrival:
                    state.ScholarStay
                        .ArrivalDialogueTriggered = true;
                    break;

                case TriggerFarewell:
                    state.ScholarStay
                        .FarewellDialogueTriggered = true;
                    break;
            }
        }


        private static string TryGetArrivalTrigger(
            PawnContext pawnContext,
            ArchiveScholarStayState stay)
        {
            if (stay.ArrivalDialogueTriggered)
            {
                return null;
            }

            int totalTicks =
                stay.EndTick -
                stay.StartTick;

            if (totalTicks <= 0)
            {
                return null;
            }

            int elapsedTicks =
                pawnContext.CurrentGameTick -
                stay.StartTick;

            if (elapsedTicks < 0)
            {
                return null;
            }

            float progress =
                (float)elapsedTicks /
                totalTicks;

            // Trigger after roughly the first 10%
            // of the temporary stay.
            if (progress < 0.10f)
            {
                return null;
            }

            return TriggerArrival;
        }


        private static string TryGetFarewellTrigger(
    PawnContext pawnContext,
    ArchiveScholarStayState stay)
        {
            if (stay.FarewellDialogueTriggered)
            {
                return null;
            }

            int totalTicks =
                stay.EndTick -
                stay.StartTick;

            if (totalTicks <= 0)
            {
                return null;
            }

            int remainingTicks =
                stay.EndTick -
                pawnContext.CurrentGameTick;

            // Already finished or overdue.
            if (remainingTicks <= 0)
            {
                return null;
            }

            float remainingFraction =
                (float)remainingTicks /
                totalTicks;

            // Trigger during roughly the final 20%
            // of the temporary stay.
            if (remainingFraction > 0.20f)
            {
                return null;
            }

            return TriggerFarewell;
        }
    }
}