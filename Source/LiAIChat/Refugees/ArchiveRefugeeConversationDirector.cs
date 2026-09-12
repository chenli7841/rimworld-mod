using LiAIChat.Models;
using LiAIChat.State;
using System;
using System.Threading.Tasks;
using UnityEngine;

namespace LiAIChat.Refugees
{
    public static class ArchiveRefugeeConversationDirector
    {
        public static async Task<ArchiveRefugeeConversationImpact>
            AnalyzeAsync(
                PawnAIState state,
                string playerMessage,
                string refugeeResponse)
        {
            if (state == null)
            {
                return null;
            }

            if (!state.IsRefugeeGroupLeader)
            {
                return null;
            }

            if (string.IsNullOrEmpty(
                state.RefugeeGroupId))
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(
                    playerMessage) ||
                string.IsNullOrWhiteSpace(
                    refugeeResponse))
            {
                return null;
            }

            ArchiveRefugeeConversationImpact impact =
                await AnalyzeWithAIAsync(
                    playerMessage,
                    refugeeResponse);

            if (impact == null)
            {
                return null;
            }

            impact.HospitalityDelta =
                Mathf.Clamp(
                    impact.HospitalityDelta,
                    -0.20f,
                    0.20f);

            return impact;
        }


        private static async Task<ArchiveRefugeeConversationImpact>
            AnalyzeWithAIAsync(
                string playerMessage,
                string refugeeResponse)
        {
            // Use the same Responses API / parsing pattern
            // already used by your Scholar conversation impact
            // implementation.

            throw new NotImplementedException();
        }
    }
}