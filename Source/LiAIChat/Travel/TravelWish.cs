using System.Collections.Generic;
using Verse;

namespace LiAIChat.Travel
{
    public class TravelWish : IExposable
    {
        public int PartnerPawnId = -1;
        public int OriginTile = -1;
        public string BiomeDefName;
        public string DesiredSeason;
        public int ActivatedTick = -1;
        public List<string> BenefitDefNames = new List<string>();

        public bool Active => ActivatedTick >= 0;

        public void ExposeData()
        {
            Scribe_Values.Look(ref PartnerPawnId, "partnerPawnId", -1);
            Scribe_Values.Look(ref OriginTile, "originTile", -1);
            Scribe_Values.Look(ref BiomeDefName, "biomeDefName");
            Scribe_Values.Look(ref DesiredSeason, "desiredSeason");
            Scribe_Values.Look(ref ActivatedTick, "activatedTick", -1);
            Scribe_Collections.Look(ref BenefitDefNames, "benefitDefNames", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && BenefitDefNames == null)
                BenefitDefNames = new List<string>();
        }
    }
}
