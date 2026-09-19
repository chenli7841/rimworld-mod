using RimWorld;
using System;

namespace LiAIChat.Archive
{
    public class ReadingOutcomeProperties_AncientArchiveKnowledge
        : ReadingOutcomeProperties
    {
        public float learningInterval = 2500f;

        public float learningStrength = 0.01f;

        public ReadingOutcomeProperties_AncientArchiveKnowledge()
        {
            doerClass =
                typeof(BookOutcomeDoer_AncientArchiveKnowledge);
        }

        public override Type DoerClass => throw new NotImplementedException();
    }
}