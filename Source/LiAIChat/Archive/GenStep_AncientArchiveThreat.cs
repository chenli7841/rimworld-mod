using Verse;

namespace LiAIChat.Archive
{
    public class GenStep_AncientArchiveThreat
        : GenStep
    {
        public override int SeedPart =>
            78412632;

        public override void Generate(
            Map map,
            GenStepParams parms)
        {
            if (map == null)
                return;

            if (parms.sitePart == null)
                return;

            float threatPoints =
                parms.sitePart.parms.threatPoints;

            if (threatPoints <= 0f)
                return;

            // 下一步：
            // 根据 threatPoints 生成 security defenders
        }
    }
}