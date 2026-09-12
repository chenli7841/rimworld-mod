using System.Collections.Generic;
using Verse;

namespace LiAIChat.Refugees
{
    public class ArchiveRefugeeGroupComponent
        : GameComponent
    {
        public List<ArchiveRefugeeGroupState> Groups =
            new List<ArchiveRefugeeGroupState>();

        public ArchiveRefugeeGroupComponent(
            Verse.Game game)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Collections.Look(
                ref Groups,
                "archiveRefugeeGroups",
                LookMode.Deep);

            if (Groups == null)
            {
                Groups =
                    new List<ArchiveRefugeeGroupState>();
            }
        }
    }
}