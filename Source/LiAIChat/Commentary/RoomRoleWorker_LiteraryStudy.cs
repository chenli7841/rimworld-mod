using Verse;

namespace LiAIChat.Commentary
{
    public class RoomRoleWorker_LiteraryStudy : RoomRoleWorker
    {
        public override float GetScore(Room room)
        {
            if (room == null || room.PsychologicallyOutdoors || room.ContainedAndAdjacentThings == null) return 0f;
            foreach (Thing thing in room.ContainedAndAdjacentThings)
                if (thing != null && thing.def != null && thing.def.defName == "LiAIChat_WritingDesk") return 1000f;
            return 0f;
        }
    }
}
