using RimWorld;
using System;

namespace LiAIChat.Archive
{
    public class ReadingOutcomeProperties_AncientArchiveKnowledge
        : ReadingOutcomeProperties
    {
        public float learningInterval = 2500f;

        public float learningStrength = 0.01f;

        public override Type DoerClass =>
            typeof(BookOutcomeDoer_AncientArchiveKnowledge);
    }
}