using RimWorld;
using Verse;

namespace LiAIChat.Models
{
    public class PawnLifeEvent : IExposable
    {
        public string Type = "";
        public string Description = "";

        public int SubjectPawnId = -1;

        public int CreatedTick = 0;

        public string SubjectFactionName;

        public bool SubjectWasHostileToPlayer;

        public bool SubjectWasPlayerFaction;

        public string SubjectRelationshipDefName;
        public string SubjectRelationshipLabel;

        /// <summary>
        /// 避免一个LifeEvent 永远让 Pawn 反复主动找你
        /// </summary>
        public bool ProactiveDialogueUsed = false;

        public string DeathContext;
        public PawnLifeEvent()
        {
        }

        public PawnLifeEvent(
            string type,
            string description,
            int subjectPawnId,
            int createdTick)
        {
            Type = type;
            Description = description;
            SubjectPawnId = subjectPawnId;
            CreatedTick = createdTick;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(
                ref Type,
                "type",
                "");

            Scribe_Values.Look(
                ref Description,
                "description",
                "");

            Scribe_Values.Look(
                ref SubjectPawnId,
                "subjectPawnId",
                -1);

            Scribe_Values.Look(
                ref CreatedTick,
                "createdTick",
                0);

            Scribe_Values.Look(
                ref ProactiveDialogueUsed,
                "proactiveDialogueUsed",
                false);

            Scribe_Values.Look(
                ref SubjectFactionName,
                "subjectFactionName");

            Scribe_Values.Look(
                ref SubjectWasHostileToPlayer,
                "subjectWasHostileToPlayer",
                false);

            Scribe_Values.Look(
                ref SubjectWasPlayerFaction,
                "subjectWasPlayerFaction",
                false);

            Scribe_Values.Look(
                ref SubjectRelationshipDefName,
                "subjectRelationshipDefName");

            Scribe_Values.Look(
                ref SubjectRelationshipLabel,
                "subjectRelationshipLabel");

            Scribe_Values.Look(
                ref DeathContext,
                "deathContext");
        }
    }
}