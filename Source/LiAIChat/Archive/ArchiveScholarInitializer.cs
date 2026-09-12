using LiAIChat.Models;
using LiAIChat.State;
using Verse;

namespace LiAIChat.Archive
{
    public static class ArchiveScholarInitializer
    {
        public static Thing_AncientEarthArchiveFragment Initialize(
            Pawn pawn)
        {
            if (pawn == null)
            {
                return null;
            }

            // 1. 创建并给予 Archive
            Thing_AncientEarthArchiveFragment archive =
                GiveScholarArchive(pawn);

            if (archive == null)
            {
                return null;
            }

            // 2. 根据 Archive 初始化 Scholar 的 Knowledge
            ArchiveScholarKnowledgeInitializer
                .InitializeFromArchive(
                    pawn,
                    archive);

            // 3. 初始化 Scholar Profile
            ArchiveScholarProfileInitializer
                .Initialize(
                    pawn,
                    archive);

            Log.Message(
                "[Li AI Chat] Archive Scholar initialized: " +
                pawn.LabelShort);

            PawnAIState state =
    PawnAIStateManager.GetState(pawn);

            if (state != null)
            {
                state.AllowsPlayerConversation = true;
            }

            return archive;
        }


        public static Thing_AncientEarthArchiveFragment
            GiveScholarArchive(
                Pawn pawn)
        {
            if (pawn == null)
            {
                return null;
            }

            Thing_AncientEarthArchiveFragment archive =
                AncientArchiveFactory.Create(
                    ArchiveSourceType.ScholarCollection,
                    BuildSourceDescription(pawn));

            if (archive == null)
            {
                Log.Warning(
                    "[Li AI Chat] Failed to create scholar archive.");

                return null;
            }

            if (pawn.inventory == null)
            {
                Log.Warning(
                    "[Li AI Chat] Scholar pawn has no inventory tracker.");

                return null;
            }

            bool added =
                pawn.inventory.innerContainer.TryAdd(
                    archive);

            if (!added)
            {
                Log.Warning(
                    "[Li AI Chat] Failed to add archive " +
                    "to scholar inventory.");

                archive.Destroy();

                return null;
            }

            Log.Message(
                "[Li AI Chat] Scholar " +
                pawn.LabelShort +
                " received archive: " +
                archive.Content?.title);

            return archive;
        }


        private static string BuildSourceDescription(
            Pawn pawn)
        {
            return
                "Preserved in the private collection of " +
                pawn.LabelShort +
                ", a wandering scholar.";
        }
    }
}