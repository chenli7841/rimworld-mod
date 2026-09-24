using System.Collections.Generic;
using System.Linq;
using LiAIChat.Game;
using RimWorld;
using Verse;

namespace LiAIChat.Archive
{
    public class RecoveredDocumentState : IExposable
    {
        public string TextDefName;
        public int UnlockedCount;
        public int DeliveryTick = -1;
        public List<string> UnlockedSectionIds = new List<string>();
        public string PendingSectionId;
        public void ExposeData() { Scribe_Values.Look(ref TextDefName, "textDefName"); Scribe_Values.Look(ref UnlockedCount, "unlockedCount", 0); Scribe_Values.Look(ref DeliveryTick, "deliveryTick", -1); Scribe_Collections.Look(ref UnlockedSectionIds, "unlockedSectionIds", LookMode.Value); Scribe_Values.Look(ref PendingSectionId, "pendingSectionId"); if (Scribe.mode == LoadSaveMode.PostLoadInit && UnlockedSectionIds == null) UnlockedSectionIds = new List<string>(); }
    }

    public static class DocumentRecovery
    {
        public const string Kant = "LiAIChat_Text_Kant_CritiqueOfPureReason";
        public static readonly string[] Titles = { "批判计划：理性应当审问自身", "空间与时间：经验的先决形式", "范畴与经验：世界何以能够被认识" };
        public static readonly int[] Costs = { 20, 35, 50 };
        public static readonly string[] Bodies = {
            "这是一份现代中文节选解说，而非任何既有译本的逐句翻译。康德所说的“批判”，不是简单挑错，也不是怀疑一切。他把问题放在知识本身之前：当我们说自己知道某件事时，这个判断凭什么成立？数学为何显得必然，自然科学为何能够发现规律，而关于灵魂、宇宙整体与神的推论又为何总会陷入争执？他认为理性很强大，却也容易把自己的能力误用到没有经验可供检验的地方。因而哲学首先要绘出一张边界图：哪些认识能够正当地提出，哪些问题虽诱人却不能被当作知识断言。这个计划并非贬低理性；恰恰相反，只有知道自己的权限，理性才不会把猜测伪装成确证。对殖民者而言，这意味着从残缺文献推出远古地球的结论时，应同时问：证据给了我们什么，解释又额外加进了什么。",
            "康德提出一个反直觉的看法：空间和时间并不只是摆在我们面前、等待被动记录的巨大容器。我们当然通过感官接触对象，但任何对象要成为“对我们显现的对象”，总已经被安排在这里或那里、先后或同时的秩序里。空间使外在事物能够被定位，时间使变化、持续与先后成为可能。由此，几何和算术的某些判断才会带有超出单次经验的稳定性；它们讨论的不是某一块石头恰好如何，而是经验得以呈现时必须采用的形式。康德并没有说外部世界是虚构的。他说的是：我们所能认识的世界，总是经过人类感受方式所给予的形式。把这一点带回殖民地，地图、日程和距离确实帮助人行动，却也提醒人：工具提供的框架并不等于事物脱离观察者后的全部样貌。",
            "仅有感官材料还不构成可理解的经验。若一连串颜色、声响和触感只是散乱经过心灵，我们无法判断“同一件事物正在变化”，更无法说某个事件导致了另一个事件。康德把使经验成为判断的基本概念称为范畴，例如因果、实体、数量与可能性。它们不是从某一次观察中慢慢抽取出来的习惯标签，而是知性组织经验时不可少的规则。正因如此，我们能够寻找规律、区分偶然与必然，并把不同观察归入同一个对象。不过范畴只能用于可能经验的范围；当人试图用因果关系证明宇宙整体必有开端，或证明灵魂必是不灭实体时，规则已经越过了可供它使用的材料。此处的教训不是放弃宏大问题，而是在讨论它们时承认：思想的意义可以超过知识的确证。"
        };
        private static readonly Dictionary<string, DocumentLine> OtherLines = new Dictionary<string, DocumentLine>
        {
            { "LiAIChat_Text_Kant_CritiqueOfPracticalReason", new DocumentLine("《实践理性批判》", new[] { "道德法则与自由", "义务为何不等于服从", "至善、希望与实践理性" }, new[] {
                "这份节选解说把问题从“我们能知道什么”转向“我们应当做什么”。康德认为，道德行动的价值不能只看结果是否有利，也不能只看行为是否合乎习惯。关键在于行动者是否能把自己采用的准则当作人人都应遵守的规则。若一个人只在获利时诚实，他并非真正尊重诚实；若他在无人监督时仍拒绝把他人当作工具，才显示出理性能够给欲望设限。自由在这里不是随心所欲，而是能够由自己认可的道德法则来约束自己。",
                "义务常被误解为冷酷地压抑感情。康德的意思更精确：同情、亲情和愿望都可以支持好行为，却不能成为唯一基础，因为它们会变化。一个人帮助受伤者，可能出于怜悯，也可能在疲惫和厌烦时仍认为对方不该被遗弃。后者显示出对人格的尊重。这样的原则会要求殖民者在资源紧张时仍思考：我希望所有人都采用这条规则吗？它是否把某些人仅当作可消耗的手段？",
                "实践理性并不承诺世界会自动奖赏善人。康德承认现实常使德行与幸福分离，因此人会希望二者最终能够协调。他把这种希望与“至善”联系起来，却也强调这不是经验知识。这里留下的不是一套保证胜利的道德算法，而是一种在不确定、危险和诱惑中仍追问行动理由的训练。" }) },
            { "LiAIChat_Text_MarcusAurelius_Meditations", new DocumentLine("《沉思录》", new[] { "写给自己的统治者笔记", "可控制与不可控制", "共同体、死亡与当下职责" }, new[] {
                "《沉思录》不是向公众宣布的学说，更像一位身负重担的人反复写给自己的提醒。作者既拥有权力，也面对战争、疾病、损失和自己的坏脾气。他不把哲学当作逃离责任的装饰，而当作在责任最重时保持清醒的方法。每一段自我劝告都承认：人会疲惫、会愤怒、会被赞美或羞辱牵着走，因此需要不断回到自己能承担的行动。",
                "斯多葛传统要求区分两类事：判断、意图和行动属于自己可以训练的部分；天气、他人的选择、死亡和名声则不完全受自己支配。区分并非叫人冷漠，而是避免把精力耗在无法命令的世界上。殖民地遭遇袭击或歉收时，作者会提醒读者承认损失，同时问自己此刻仍能做出怎样合乎职责的安排。",
                "这本笔记把人放在共同体和时间之中。别人令人厌烦，并不自动免除我们公正相待的责任；死亡并不因为可怕就变得不自然。作者反复把注意力拉回当下的小行动：把手头职责完成，把判断说清，不为短暂的掌声背叛长期原则。它并不消除悲伤，却试图使悲伤不必决定人的全部行为。" }) },
            { "LiAIChat_Text_Augustine_CityOfGod", new DocumentLine("《上帝之城》", new[] { "帝国危机与两座城", "爱决定共同体的方向", "历史、希望与地上政治" }, new[] {
                "奥古斯丁写作时，一个长期统治广阔地域的帝国正在震动。有人责怪新信仰削弱了旧秩序，他则拒绝把任何政权的成败直接等同于神意。《上帝之城》由此提出两种“城”：它们首先不是地图上的两个国家，而是两种爱的组织方式。一种以自我、支配和短暂荣耀为中心，另一种以对上帝和邻人的爱为归宿。",
                "这一区分不意味着所有现实城市都毫无价值。法律、和平和共同生活仍然重要；人在有限的世界里需要合作、秩序与保护。只是任何国家都不能宣称自己就是最终善，不能要求绝对忠诚，也不能以扩张和胜利证明自身永恒。对处于艰难环境的殖民地而言，这提供一种提醒：安全与繁荣值得追求，但它们不足以单独说明共同体为何存在。",
                "奥古斯丁的历史观把希望放在任何地上帝国之外，因此既能批判现实，也能避免把一次失败当作意义的终结。这并不是消极等待，而是在不完美制度中实践公正、怜悯和克制。书中保存的争论仍尖锐：当城市要求牺牲个人、信仰或真相来换取存续时，人应当如何衡量忠诚？" }) }
        };
        public class DocumentLine { public string Name; public string[] Titles; public string[] Bodies; public DocumentLine(string name, string[] titles, string[] bodies) { Name=name; Titles=titles; Bodies=bodies; } }
        public static DocumentLine GetLine(string text) { DocumentLine xmlLine = GetXmlLine(text); if (xmlLine != null) return xmlLine; if (text == Kant) return new DocumentLine("《纯粹理性批判》", Titles, Bodies); if (text == "LiAIChat_Text_Kant_CritiqueOfPracticalReason") return BuildPracticalReason(); DocumentLine line; return OtherLines.TryGetValue(text, out line) ? line : null; }
        private static DocumentLine GetXmlLine(string text)
        {
            List<DocumentRecoverySectionDef> sections = DefDatabase<DocumentRecoverySectionDef>.AllDefsListForReading.Where(x => x.textDefName == text).OrderBy(x => x.order).ToList();
            if (sections.Count == 0) return null;
            EarthTextDef book = DefDatabase<EarthTextDef>.GetNamedSilentFail(text);
            return new DocumentLine(book?.titleChinese.NullOrEmpty() == false ? "《" + book.titleChinese + "》" : book?.title ?? text, sections.Select(x => x.title).ToArray(), sections.Select(x => x.body).ToArray());
        }
        public static int GetCost(int sectionIndex) { return 200 + sectionIndex * 100; }
        private static DocumentLine BuildPracticalReason()
        {
            return new DocumentLine("《实践理性批判》", new[] {
                "问题的转向：我应当做什么", "善意志与行动理由", "义务并非外在命令", "准则能否成为普遍法则", "人格不可只当作手段", "自由与自律", "欲望、幸福与道德", "敬重：道德法则的感受", "责任冲突中的判断", "恶与自我欺骗", "道德共同体的设想", "最高善的问题", "幸福为何不能替代德行", "灵魂不朽作为实践希望", "上帝理念的实践位置", "法、政治与人的尊严", "教育与品格训练", "失败后仍须行动", "批判的边界", "结语：在星空与法则之间" }, new[] {
                "本书把哲学的重心放在行动上：即使我们无法把所有世界问题变成知识，人仍必须在每一次选择中给出理由。实践理性讨论的不是怎样最聪明地达到目标，而是哪些目标与做法值得一个能对自己负责的人采用。",
                "康德所谓善意志，不是单纯的好心情。它指人在结果不确定时，仍努力依据自己认为正当的原则行动。能力、勇气和财富都可能被恶用；行动的道德价值取决于人是否愿意让理由经得起公开检验。",
                "义务容易令人想到命令与惩罚，但这里的义务来自理性主体对自身原则的认可。一个人不是因为害怕首领才不欺骗，而是理解到若人人都把谎言当工具，承诺本身就会失去意义。",
                "普遍法则的检验要求人问：我现在采用的准则，能否被所有处境相似的人共同采用？这并不提供机械答案，却会暴露许多例外请求：当我为自己保留特权时，究竟愿不愿意让别人也这样做？",
                "人既有目标也有尊严。把他人当作手段并非永远错误，例如合作和雇佣都涉及彼此的能力；问题在于是否同时承认对方能同意、拒绝、提出理由，并拥有不应被交易掉的价值。",
                "自由不是欲望想要什么就立刻取得什么。自律意味着行动者能够审查冲动，选择一条自己也愿承认其正当性的规则。这样的自由更难，却使责任成为可能。",
                "幸福值得追求，但幸福的内容因人而异，也会被环境改变。若把快乐或繁荣当作唯一尺度，强者可能轻易牺牲弱者。道德并不要求厌恶幸福，而要求不以他人的人格为代价购买幸福。",
                "敬重不是畏惧权力，而是意识到道德法则对自己的要求。它常让人感到不舒服，因为它限制借口；也正因此，它能在利益和情绪摇摆时保留一种稳定的行动方向。",
                "责任冲突很少能靠一句格言解决。康德式思考要求把情境说清：谁受影响、我作出的例外能否公开、有没有把某人仅当作工具。判断仍需勇气，原则的价值在于防止人把方便误认为正当。",
                "恶并不总以明显残酷的形式出现。人常先承认一条原则，再悄悄为自己添加例外：这次情况特殊、没人会知道、以后再补偿。批判实践理性要求持续辨认这种自我欺骗。",
                "道德共同体不是一个已经完美的国家，而是一种互相把对方当作立法者的关系。它要求制度能给人理由，也要求个人不把公共规则只看作妨碍自己获利的障碍。",
                "最高善把德行与幸福放在同一问题中：理性希望善的生活不必永远与幸福敌对。然而现实并不保证这种协调，因此它首先是一种实践方向，而不是可由观察证明的世界事实。",
                "幸福不能替代德行，因为它告诉我们想要什么，却不能单独说明该不该这样获得。一个富足的殖民地仍可能建立在剥削、背叛或恐惧上；繁荣本身不能洗净取得它的理由。",
                "灵魂不朽在本书中不是经验报告，而是实践理性面对无限道德要求时形成的希望。人永远无法宣布自己已完全善良；这一理念表达的是持续改进而非一张可以验证的生存证明。",
                "上帝理念同样不是从道德直接推导出的科学结论。它在实践中承载着一种希望：道德努力并非与世界完全无关。康德刻意保留这一区别，以免信念伪装成知识。",
                "尊严会延伸到政治与法律：制度应保护人作为能提出理由的主体，而非只把他们统计为劳力、兵源或财富。法律可以强制外在行为，却不能替代个人对行动理由的反省。",
                "品格训练不是把儿童塑造成听话工具。它应逐渐培养他们理解规则、感受他人处境，并能够说明自己为何赞成或反对某种做法。服从如果永远不能转化为判断，就还不是自律。",
                "当善意行动失败时，人仍可能受到诱惑，认为原则毫无用处。康德的回答并不浪漫：结果值得认真对待，但一次失败不能自动证明背叛、残酷或欺骗变得正当。",
                "批判为实践理性划出边界。它允许人以自由、责任和希望来组织生活，却不允许把这些需要直接宣布为可测量的知识。边界不是削弱意义，而是让不同类型的断言不彼此冒充。",
                "全书最后留下的形象是：人在广阔、冷漠且难以穷尽的宇宙中，仍能听见内在的道德要求。它不承诺轻松，却要求每个人把自己当作能够承担理由、也应被他人尊重的存在。" });
        }
        public static RecoveredDocumentState GetState(string text)
        {
            LiAIChatGameComponent game = Current.Game.GetComponent<LiAIChatGameComponent>();
            RecoveredDocumentState state = game.RecoveredDocuments.FirstOrDefault(x => x.TextDefName == text);
            if (state == null) { state = new RecoveredDocumentState { TextDefName = text }; game.RecoveredDocuments.Add(state); }
            DocumentLine line = GetLine(text);
            if (line != null && state.UnlockedSectionIds != null)
                while (state.UnlockedSectionIds.Count < state.UnlockedCount && state.UnlockedSectionIds.Count < line.Titles.Length)
                    state.UnlockedSectionIds.Add(GetSectionId(text, state.UnlockedSectionIds.Count));
            state.UnlockedCount = state.UnlockedSectionIds.Count;
            return state;
        }
        public static string GetSectionId(string text, int index) { return text + ".section." + (index + 1).ToString("00"); }
        public static bool CanRecover(Thing_AncientEarthArchiveFragment archive) => archive != null && archive.Identified && GetLine(archive.EarthTextDefName) != null;
        public static void RequestNext(Thing_AncientEarthArchiveFragment archive)
        {
            string text = archive.EarthTextDefName; DocumentLine line = GetLine(text); RecoveredDocumentState state = GetState(text);
            if (state.DeliveryTick > 0 || state.UnlockedSectionIds.Count >= line.Titles.Length) return;
            int cost = GetCost(state.UnlockedSectionIds.Count); int remaining = cost;
            int total = Find.Maps.Where(m => m.IsPlayerHome).Sum(m => m.listerThings.ThingsOfDef(ThingDefOf.Gold).Sum(g => g.stackCount));
            if (total < cost) { Messages.Message("需要 " + cost + " 单位黄金以委托复原下一节。", MessageTypeDefOf.RejectInput); return; }
            foreach (Map map in Find.Maps.Where(m => m.IsPlayerHome)) foreach (Thing gold in map.listerThings.ThingsOfDef(ThingDefOf.Gold).ToList()) { int take = System.Math.Min(remaining, gold.stackCount); gold.stackCount -= take; remaining -= take; if (gold.stackCount == 0) gold.Destroy(); if (remaining == 0) break; }
            state.DeliveryTick = Find.TickManager.TicksGame + 60000;
            state.PendingSectionId = GetSectionId(text, state.UnlockedSectionIds.Count);
            Messages.Message("黄金已交给译稿密钥商。复原结果将在约一天后送达。", MessageTypeDefOf.PositiveEvent);
        }
        public static void Tick()
        {
            LiAIChatGameComponent game = Current.Game?.GetComponent<LiAIChatGameComponent>(); if (game == null) return;
            foreach (RecoveredDocumentState state in game.RecoveredDocuments.Where(x => x.DeliveryTick > 0 && x.DeliveryTick <= Find.TickManager.TicksGame).ToList()) { DocumentLine line = GetLine(state.TextDefName); int index = state.UnlockedSectionIds.Count; state.DeliveryTick = -1; state.UnlockedSectionIds.Add(state.PendingSectionId ?? GetSectionId(state.TextDefName, index)); state.PendingSectionId = null; state.UnlockedCount = state.UnlockedSectionIds.Count; Messages.Message("加密译稿已恢复：" + line.Titles[index], MessageTypeDefOf.PositiveEvent); }
        }
    }
}
