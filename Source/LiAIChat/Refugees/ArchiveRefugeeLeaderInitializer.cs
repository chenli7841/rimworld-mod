using LiAIChat.Models;
using LiAIChat.State;
using Verse;

namespace LiAIChat.Refugees
{
    public static class ArchiveRefugeeLeaderInitializer
    {
        public static void Initialize(
            Pawn pawn,
            ArchiveRefugeeGroupState group)
        {
            if (pawn == null ||
                group == null)
            {
                return;
            }

            PawnAIState state =
                PawnAIStateManager.GetState(pawn);

            if (state == null)
            {
                Log.Warning(
                    "[Li AI Chat] Refugee leader initializer: " +
                    "PawnAIState was null for " +
                    pawn.LabelShort);

                return;
            }

            state.AllowsPlayerConversation = true;

            state.RefugeeGroupId =
                group.GroupId;

            state.IsRefugeeGroupLeader =
                true;

            Log.Message(
                "[Li AI Chat] Refugee leader initialized: " +
                pawn.LabelShort +
                ", group=" +
                group.GroupId);
        }
    }
}