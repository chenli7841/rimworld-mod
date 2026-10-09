using UnityEngine;
using Verse;

namespace LiAIChat.UI
{
    [StaticConstructorOnStartup]
    public static class LiAIChatTextures
    {
        public static readonly Texture2D Talk =
            ContentFinder<Texture2D>.Get(
                "UI/Commands/Talk");

        public static readonly Texture2D StudyArchive =
            ContentFinder<Texture2D>.Get(
                "UI/Commands/StudyArchive");

        public static readonly Texture2D CreateBook =
            ContentFinder<Texture2D>.Get(
                "UI/Commands/CreateBook");

        public static readonly Texture2D EditBook =
            ContentFinder<Texture2D>.Get(
                "UI/Commands/EditBook");

        public static readonly Texture2D PermanentMigration =
            ContentFinder<Texture2D>.Get(
                "UI/Commands/PermanentMigration");
    }
}
