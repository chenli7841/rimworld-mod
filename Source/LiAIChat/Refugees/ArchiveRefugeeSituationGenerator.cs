using System.Collections.Generic;
using Verse;

namespace LiAIChat.Refugees
{
    public static class ArchiveRefugeeSituationGenerator
    {
        public static void Generate(
            ArchiveRefugeeGroupState group,
            IList<Pawn> pawns)
        {
            if (group == null ||
                pawns == null ||
                pawns.Count == 0)
            {
                return;
            }

            GenerateSituationType(group);

            GenerateTravelHardship(group);

            GenerateResourceCondition(group);

            AnalyzeAgeComposition(
                group,
                pawns);
        }


        private static void GenerateSituationType(
            ArchiveRefugeeGroupState group)
        {
            int roll =
                Rand.RangeInclusive(0, 3);

            switch (roll)
            {
                case 0:
                    group.SituationType =
                        "DisplacedFamily";
                    break;

                case 1:
                    group.SituationType =
                        "ScatteredSurvivors";
                    break;

                case 2:
                    group.SituationType =
                        "LongTermWanderers";
                    break;

                default:
                    group.SituationType =
                        "FailedSettlement";
                    break;
            }
        }


        private static void GenerateTravelHardship(
            ArchiveRefugeeGroupState group)
        {
            float roll =
                Rand.Value;

            if (roll < 0.20f)
            {
                group.TravelHardship =
                    "Light";
            }
            else if (roll < 0.60f)
            {
                group.TravelHardship =
                    "Moderate";
            }
            else if (roll < 0.90f)
            {
                group.TravelHardship =
                    "Severe";
            }
            else
            {
                group.TravelHardship =
                    "Exhausting";
            }
        }


        private static void GenerateResourceCondition(
            ArchiveRefugeeGroupState group)
        {
            float roll =
                Rand.Value;

            if (roll < 0.15f)
            {
                group.ResourceCondition =
                    "Stable";
            }
            else if (roll < 0.55f)
            {
                group.ResourceCondition =
                    "Poor";
            }
            else if (roll < 0.85f)
            {
                group.ResourceCondition =
                    "VeryPoor";
            }
            else
            {
                group.ResourceCondition =
                    "Critical";
            }
        }


        private static void AnalyzeAgeComposition(
            ArchiveRefugeeGroupState group,
            IList<Pawn> pawns)
        {
            int youngest =
                int.MaxValue;

            int oldest =
                int.MinValue;

            for (int i = 0;
                 i < pawns.Count;
                 i++)
            {
                Pawn pawn =
                    pawns[i];

                if (pawn == null ||
                    pawn.ageTracker == null)
                {
                    continue;
                }

                int age =
                    pawn.ageTracker
                        .AgeBiologicalYears;

                if (age < youngest)
                {
                    youngest = age;
                }

                if (age > oldest)
                {
                    oldest = age;
                }
            }

            if (youngest != int.MaxValue)
            {
                group.YoungestAge =
                    youngest;
            }

            if (oldest != int.MinValue)
            {
                group.OldestAge =
                    oldest;
            }
        }
    }
}