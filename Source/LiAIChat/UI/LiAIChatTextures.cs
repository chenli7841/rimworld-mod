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
    }
}