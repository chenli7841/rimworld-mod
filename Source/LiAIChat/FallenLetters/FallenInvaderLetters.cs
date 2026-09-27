using HarmonyLib;
using LiAIChat.AI;
using LiAIChat.Background;
using LiAIChat.Config;
using LiAIChat.Game;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace LiAIChat.FallenLetters
{
    public class FallenLetterPerson : IExposable
    {
        public string Id, Name, Faction, Role, Childhood, Adulthood, Traits, FirstLetter;
        public List<string> LaterLetters = new List<string>();
        public int PendingSiteTile = -1, NextLetterIndex = 1;
        public void ExposeData() { Scribe_Values.Look(ref Id,"id"); Scribe_Values.Look(ref Name,"name"); Scribe_Values.Look(ref Faction,"faction"); Scribe_Values.Look(ref Role,"role"); Scribe_Values.Look(ref Childhood,"childhood"); Scribe_Values.Look(ref Adulthood,"adulthood"); Scribe_Values.Look(ref Traits,"traits"); Scribe_Values.Look(ref FirstLetter,"firstLetter"); Scribe_Collections.Look(ref LaterLetters,"laterLetters",LookMode.Value); Scribe_Values.Look(ref PendingSiteTile,"pendingSiteTile",-1); Scribe_Values.Look(ref NextLetterIndex,"nextLetterIndex",1); if(LaterLetters==null) LaterLetters=new List<string>(); }
    }
    public class Thing_FallenInvaderLetter : ThingWithComps
    {
        public string PersonId; public int LetterIndex;
        public override void ExposeData(){ base.ExposeData(); Scribe_Values.Look(ref PersonId,"personId"); Scribe_Values.Look(ref LetterIndex,"letterIndex"); }
        public override string LabelNoCount { get { var p=FallenLetterManager.Get(PersonId); return p==null?"无名家书":"“"+p.Name+"”的家书（第"+(LetterIndex+1)+"封）"; } }
        public override string DescriptionDetailed { get { var p=FallenLetterManager.Get(PersonId); if(p==null)return "一封字迹褪色的家书。"; string text=LetterIndex==0?p.FirstLetter:(p.LaterLetters.Count>=LetterIndex?p.LaterLetters[LetterIndex-1]:null); return "寄件人："+p.Name+"\n阵营："+p.Faction+"\n职业："+p.Role+"\n幼年："+p.Childhood+"\n成年："+p.Adulthood+"\n特性："+p.Traits+"\n\n"+(text??"信纸仍在等待被译读。")+"\n\n每封家书都可能让孩子从无线电杂音中找到新的线索。"; } }
    }
    public static class FallenLetterManager
    {
        const int MaxLetters=10;
        static readonly HttpClient Client=new HttpClient();
        static LiAIChatGameComponent Game => Current.Game?.GetComponent<LiAIChatGameComponent>();
        public static FallenLetterPerson Get(string id)=>Game?.FallenLetterPeople?.FirstOrDefault(x=>x.Id==id);
        public static void DropFrom(Pawn pawn)
        {
            if(pawn?.Map==null || !pawn.RaceProps.Humanlike || pawn.Faction==null || pawn.Faction.IsPlayer || !Rand.Chance(0.08f)) return;
            var p=new FallenLetterPerson{Id="fallen-"+pawn.thingIDNumber+"-"+(Find.TickManager?.TicksGame??0),Name=pawn.Name?.ToStringFull??pawn.LabelShort,Faction=pawn.Faction.Name,Role=pawn.kindDef?.label??"士兵",Childhood=pawn.story?.Childhood?.title??"未知",Adulthood=pawn.story?.Adulthood?.title??"未知",Traits=pawn.story?.traits?.allTraits?.Select(t=>t.LabelCap).ToCommaList()??"未知",FirstLetter="致亲爱的人：如果这封信到了你手里，请相信我一直记得家里的灯火。这里的日子很长，我仍盼望有一天能带着平安回来。"};
            Game.FallenLetterPeople.Add(p); var letter=(Thing_FallenInvaderLetter)ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("LiAIChat_FallenInvaderLetter")); letter.PersonId=p.Id; GenPlace.TryPlaceThing(letter,pawn.Position,pawn.Map,ThingPlaceMode.Near);
            GenerateLetter(p.Id,0);
        }
        public static void TryDiscoverSignal()
        {
            if(!Find.Maps.Any(m=>m.IsPlayerHome && m.mapPawns.FreeColonistsSpawned.Any(child=>child.DevelopmentalStage==DevelopmentalStage.Child) && m.listerThings.ThingsOfDef(ThingDefOf.CommsConsole).Any())) return;
            if(!Rand.Chance(0.025f)) return;
            var p=Game.FallenLetterPeople.FirstOrDefault(x=>x.PendingSiteTile<0 && x.NextLetterIndex<MaxLetters && HasFirstLetter(x.Id)); if(p==null)return;
            PlanetTile tile; if(!TileFinder.TryFindTileWithDistance(Find.Maps.First(m=>m.IsPlayerHome).Tile,5,12,out tile,null,TileFinderMode.Near,true))return;
            var part=DefDatabase<SitePartDef>.GetNamedSilentFail("LiAIChat_FallenLetterSite"); if(part==null)return;
            Site site=SiteMaker.MakeSite(part,tile,Faction.OfMechanoids,true,300f,null); Find.WorldObjects.Add(site); p.PendingSiteTile=tile; Messages.Message("儿童在通讯台的杂音中捕捉到“"+p.Name+"”的另一封家书线索。地图上已标出位置；那里没有人类守卫。",MessageTypeDefOf.PositiveEvent);
            GenerateLetter(p.Id,p.NextLetterIndex);
        }
        static bool HasFirstLetter(string id)=>Find.Maps.Where(m=>m.IsPlayerHome).SelectMany(m=>m.listerThings.ThingsOfDef(DefDatabase<ThingDef>.GetNamedSilentFail("LiAIChat_FallenInvaderLetter"))??new List<Thing>()).OfType<Thing_FallenInvaderLetter>().Any(l=>l.PersonId==id&&l.LetterIndex==0);
        public static FallenLetterPerson ForSite(Map map)=>Get(Game?.FallenLetterPeople?.FirstOrDefault(x=>x.PendingSiteTile==map?.Tile)?.Id);
        public static void SpawnFollowUp(Map map, IntVec3 cell)
        { var p=ForSite(map); if(p==null)return; int index=p.NextLetterIndex++; p.PendingSiteTile=-1; while(p.LaterLetters.Count<index)p.LaterLetters.Add("致家人：我在这片陌生的土地上又想起了从前。童年的路、成年后的选择，以及没能说出口的歉意，都留在这一页里。愿你们记得，我并不只是一名倒下的入侵者，也是曾被人等待的人。"); var l=(Thing_FallenInvaderLetter)ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("LiAIChat_FallenInvaderLetter"));l.PersonId=p.Id;l.LetterIndex=index;GenPlace.TryPlaceThing(l,cell,map,ThingPlaceMode.Near); }
        static async void GenerateLetter(string id,int index)
        {
            var p=Get(id); if(p==null || string.IsNullOrWhiteSpace(Config.Config.OpenAI_API_KEY))return;
            try { string prompt="为RimWorld阵亡入侵者写一封中文家书。约1000个汉字，克制、具体、有生活感，不美化战争；只输出信正文。寄件人："+p.Name+"；阵营："+p.Faction+"；职业："+p.Role+"；幼年："+p.Childhood+"；成年："+p.Adulthood+"；特性："+p.Traits+"。这是第"+(index+1)+"封。"; string json="{\"model\":\"gpt-5.6-luna\",\"input\":\""+prompt.Replace("\\","\\\\").Replace("\"","\\\"")+"\",\"max_output_tokens\":1800}"; using(var req=new HttpRequestMessage(HttpMethod.Post,"https://api.openai.com/v1/responses")){req.Headers.Add("Authorization","Bearer "+Config.Config.OpenAI_API_KEY);req.Content=new StringContent(json,Encoding.UTF8,"application/json");using(var res=await Client.SendAsync(req).ConfigureAwait(false)){string text=OpenAIResponseParser.ExtractOutputText(await res.Content.ReadAsStringAsync().ConfigureAwait(false));MainThreadActionQueue.Enqueue(()=>{var person=Get(id);if(person==null||string.IsNullOrWhiteSpace(text))return;if(index==0)person.FirstLetter=text;else{while(person.LaterLetters.Count<index)person.LaterLetters.Add("");person.LaterLetters[index-1]=text;}});}} } catch(Exception ex){Log.Warning("[Li AI Chat] Fallen-letter generation failed: "+ex.Message);}
        }
    }
    [HarmonyPatch(typeof(Pawn),"Kill")] public static class FallenLetterDeathPatch { static void Postfix(Pawn __instance){ FallenLetterManager.DropFrom(__instance); } }
    public class GenStep_FallenLetterSite : GenStep { public override int SeedPart=>742061; public override void Generate(Map map,GenStepParams parms){ IntVec3 c=map.Center; FallenLetterManager.SpawnFollowUp(map,c); for(int i=0;i<4;i++){ PawnKindDef k=DefDatabase<PawnKindDef>.GetNamedSilentFail(new[]{"Militor","Scyther","Lancer"}.RandomElement()); if(k!=null)GenSpawn.Spawn(PawnGenerator.GeneratePawn(k,Faction.OfMechanoids),CellFinder.RandomClosewalkCellNear(c,map,8),map); } } }
}
