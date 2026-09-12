using LiAIChat.Models;
using LiAIChat.State;
using UnityEngine;

namespace LiAIChat.Archive
{
    public static class ArchiveScholarGiftDecisionUtility
    {
        public static bool Decide(
            PawnAIState state)
        {
            if (state == null ||
                state.ScholarStay == null ||
                state.ScholarProfile == null)
            {
                return false;
            }

            ArchiveScholarStayState stay =
                state.ScholarStay;

            // 决定一旦形成，就永远使用第一次结果。
            if (stay.ArchiveGiftDecided)
            {
                return stay.WillGiftArchive;
            }

            bool willGift =
                EvaluateGiftDecision(
                    stay);

            stay.ArchiveGiftDecided =
                true;

            stay.WillGiftArchive =
                willGift;

            return willGift;
        }


        private static bool EvaluateGiftDecision(
    ArchiveScholarStayState stay)
        {
            if (stay == null)
            {
                return false;
            }

            if (stay.HospitalityImpression <= -0.50f)
            {
                return false;
            }

            if (stay.HospitalityImpression >= 0.25f)
            {
                return true;
            }

            if (stay.MeaningfulConversationCount >= 2 &&
                stay.HospitalityImpression >= -0.10f)
            {
                return true;
            }

            if (stay.MeaningfulConversationCount >= 1 &&
                stay.HospitalityImpression >= 0.10f)
            {
                return true;
            }

            return false;
        }


        public static void ApplyHospitalityChange(
            PawnAIState state,
            float change)
        {
            if (state == null ||
                state.ScholarStay == null)
            {
                return;
            }

            ArchiveScholarStayState stay =
                state.ScholarStay;

            stay.HospitalityImpression =
                Mathf.Clamp(
                    stay.HospitalityImpression + change,
                    -1f,
                    1f);
        }


        public static void RecordMeaningfulConversation(
            PawnAIState state)
        {
            if (state == null ||
                state.ScholarStay == null)
            {
                return;
            }

            ArchiveScholarStayState stay =
                state.ScholarStay;

            if (!stay.Active)
            {
                return;
            }

            stay.MeaningfulConversationCount++;
        }
    }
}