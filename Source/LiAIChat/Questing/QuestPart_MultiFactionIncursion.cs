using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace LiAIChat.Questing
{
    public class FactionRelationSnapshot : IExposable
    {
        public Faction source;
        public Faction other;
        public FactionRelationKind kind;
        public int baseGoodwill;

        public void ExposeData()
        {
            Scribe_References.Look(ref source, "source");
            Scribe_References.Look(ref other, "other");
            Scribe_Values.Look(ref kind, "kind");
            Scribe_Values.Look(ref baseGoodwill, "baseGoodwill");
        }
    }

    public class QuestPart_MultiFactionIncursion : QuestPartActivable
    {
        public Map map;
        public Faction humanFactionA;
        public Faction humanFactionB;
        public Faction mechanoidFaction;
        public float pointsPerFaction;
        public List<Pawn> attackers = new List<Pawn>();
        public List<FactionRelationSnapshot> relations = new List<FactionRelationSnapshot>();
        public bool spawned;
        public bool relationsRestored;

        public override bool RequiresAccepter => true;

        public override string DescriptionPart
        {
            get
            {
                if (!spawned)
                    return "三支互相敌对的武装将从不同方向同时入侵。接受后，他们会互相攻击，也会攻击殖民地。";
                int standing = attackers.Count(pawn => pawn != null && !pawn.Dead && !pawn.Downed);
                return "多方混战仍在持续。仍有 " + standing + " 名外来武装保持战斗能力。";
            }
        }

        public override void PreQuestAccept()
        {
            base.PreQuestAccept();
            if (!SpawnIncursion())
            {
                Messages.Message("多方入侵未能形成：其中一支武装无法集结。", MessageTypeDefOf.NegativeEvent);
                RestoreRelations();
                quest.End(QuestEndOutcome.Fail, true);
            }
        }

        private bool SpawnIncursion()
        {
            if (map == null || humanFactionA == null || humanFactionB == null || mechanoidFaction == null)
                return false;

            List<Pawn> first = GenerateGroup(humanFactionA);
            List<Pawn> second = GenerateGroup(humanFactionB);
            List<Pawn> mechs = GenerateGroup(mechanoidFaction);
            if (first.Count == 0 || second.Count == 0 || mechs.Count == 0)
            {
                DestroyUnspawned(first);
                DestroyUnspawned(second);
                DestroyUnspawned(mechs);
                return false;
            }

            SetMutualHostility(humanFactionA, humanFactionB);
            SetMutualHostility(humanFactionA, mechanoidFaction);
            SetMutualHostility(humanFactionB, mechanoidFaction);

            SpawnGroup(first, humanFactionA, Rot4.North);
            SpawnGroup(second, humanFactionB, Rot4.East);
            SpawnGroup(mechs, mechanoidFaction, Rot4.South);
            attackers.AddRange(first);
            attackers.AddRange(second);
            attackers.AddRange(mechs);
            spawned = true;

            Find.LetterStack.ReceiveLetter("多方入侵：交火开始",
                humanFactionA.Name + "、" + humanFactionB.Name + " 与机械族同时闯入殖民地。三方彼此敌对，混战已经开始。",
                LetterDefOf.ThreatBig, map.Parent);
            return true;
        }

        private List<Pawn> GenerateGroup(Faction faction)
        {
            PawnGroupMakerParms parms = new PawnGroupMakerParms
            {
                faction = faction,
                groupKind = PawnGroupKindDefOf.Combat,
                points = pointsPerFaction,
                tile = map.Tile,
                generateFightersOnly = true
            };
            return PawnGroupMakerUtility.GeneratePawns(parms, true).ToList();
        }

        private void SpawnGroup(List<Pawn> group, Faction faction, Rot4 edge)
        {
            IntVec3 root;
            if (!CellFinder.TryFindRandomEdgeCellWith(cell => cell.Standable(map), map, edge, 0f, out root))
                root = CellFinder.RandomEdgeCell(edge, map);

            foreach (Pawn pawn in group)
            {
                IntVec3 cell = CellFinder.RandomSpawnCellForPawnNear(root, map, 10);
                GenSpawn.Spawn(pawn, cell, map);
            }
            LordMaker.MakeNewLord(faction,
                new LordJob_AssaultColony(faction, false, false, false, false, false, false, false), map, group);
        }

        private void SetMutualHostility(Faction first, Faction second)
        {
            RememberRelation(first, second);
            RememberRelation(second, first);
            first.SetRelationDirect(second, FactionRelationKind.Hostile, false, "多方入侵", null);
            second.SetRelationDirect(first, FactionRelationKind.Hostile, false, "多方入侵", null);
        }

        private void RememberRelation(Faction source, Faction other)
        {
            if (relations.Any(snapshot => snapshot.source == source && snapshot.other == other))
                return;
            FactionRelation relation = source.RelationWith(other, false);
            relations.Add(new FactionRelationSnapshot
            {
                source = source,
                other = other,
                kind = relation == null ? FactionRelationKind.Neutral : relation.kind,
                baseGoodwill = relation == null ? 0 : relation.baseGoodwill
            });
        }

        private static void DestroyUnspawned(IEnumerable<Pawn> pawns)
        {
            foreach (Pawn pawn in pawns)
                if (pawn != null && !pawn.Destroyed)
                    pawn.Destroy(DestroyMode.Vanish);
        }

        public override void QuestPartTick()
        {
            base.QuestPartTick();
            if (quest.State != QuestState.Ongoing || !spawned || Find.TickManager.TicksGame % 250 != 0)
                return;

            if (attackers.All(pawn => pawn == null || pawn.Dead || pawn.Downed || !pawn.Spawned))
            {
                RestoreRelations();
                Find.LetterStack.ReceiveLetter("多方入侵结束",
                    "外来武装已被击溃或失去战斗能力。殖民地暂时恢复了平静。",
                    LetterDefOf.PositiveEvent, map.Parent);
                quest.End(QuestEndOutcome.Success, true);
            }
        }

        private void RestoreRelations()
        {
            if (relationsRestored) return;
            foreach (FactionRelationSnapshot snapshot in relations)
            {
                if (snapshot?.source == null || snapshot.other == null) continue;
                FactionRelation relation = new FactionRelation(snapshot.other, snapshot.kind)
                {
                    baseGoodwill = snapshot.baseGoodwill
                };
                snapshot.source.SetRelation(relation);
            }
            relationsRestored = true;
        }

        public override void Cleanup()
        {
            RestoreRelations();
            base.Cleanup();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref map, "map");
            Scribe_References.Look(ref humanFactionA, "humanFactionA");
            Scribe_References.Look(ref humanFactionB, "humanFactionB");
            Scribe_References.Look(ref mechanoidFaction, "mechanoidFaction");
            Scribe_Values.Look(ref pointsPerFaction, "pointsPerFaction");
            Scribe_Collections.Look(ref attackers, "attackers", LookMode.Reference);
            Scribe_Collections.Look(ref relations, "relations", LookMode.Deep);
            Scribe_Values.Look(ref spawned, "spawned", false);
            Scribe_Values.Look(ref relationsRestored, "relationsRestored", false);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (attackers == null) attackers = new List<Pawn>();
                if (relations == null) relations = new List<FactionRelationSnapshot>();
            }
        }
    }
}
