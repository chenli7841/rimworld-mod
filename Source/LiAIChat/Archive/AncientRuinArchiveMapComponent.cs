using Verse;

namespace LiAIChat.Archive
{
    public class AncientRuinArchiveMapComponent : MapComponent
    {
        public bool SpawnProcessed = false;

        public AncientRuinArchiveMapComponent(Map map)
            : base(map)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(
                ref SpawnProcessed,
                "liAIChatAncientRuinArchiveSpawnProcessed",
                false);
        }
    }
}