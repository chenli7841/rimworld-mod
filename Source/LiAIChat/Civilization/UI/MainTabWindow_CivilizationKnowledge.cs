using RimWorld;
using UnityEngine;
using Verse;

namespace LiAIChat.Civilization.UI
{
    public class MainTabWindow_CivilizationKnowledge
        : MainTabWindow
    {
        public override Vector2 RequestedTabSize
        {
            get
            {
                return new Vector2(
                    1200f,
                    760f);
            }
        }

        public override void DoWindowContents(
            Rect inRect)
        {
            Text.Font = GameFont.Medium;

            Widgets.Label(
                new Rect(
                    0f,
                    0f,
                    inRect.width,
                    35f),
                "Civilization Knowledge");

            Text.Font = GameFont.Small;

            Widgets.Label(
                new Rect(
                    0f,
                    45f,
                    inRect.width,
                    30f),
                "Civilization knowledge tree is active.");
        }
    }
}