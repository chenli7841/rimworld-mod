using Verse;

namespace LiAIChat.Models
{
    /// <summary>
    /// Meaning ≠ Mood
    /// 他可能拥有：豪华卧室，Fine Meal，娱乐充足，没有危险；但开始问：“所以我们就这样活到死吗？”
    /// Meaning ≠ Christianity，比如：“我知道自己应该相信什么，但现在我不知道怎么继续活。”
    /// </summary>
    public class MeaningState : IExposable
    {
        /// <summary>
        /// 我现在有没有值得投入生命去做的事情？
        /// 比如0.8 - 我要把殖民地医院建起来。0.2 - 每天就是吃饭、工作、睡觉。我不知道这些到底为了什么。
        /// </summary>
        public float Purpose = 0.5f;

        /// <summary>
        /// 我觉得自己属于这里吗？我和别人真正连接吗？
        /// 比如0.15 - 他们需要的是我的枪法，不是我这个人。
        /// 注意它不是简单等于 Social Opinion：一个 Pawn 可以大家都喜欢他，但他自己仍然觉得：我不属于这里。
        /// </summary>
        public float Belonging = 0.5f;

        /// <summary>
        /// 我相信未来值得期待吗？
        /// 比如0.8 - 我现在很痛苦，但我相信事情不会永远这样。0.2 - 现在生活挺舒服，但我觉得最后什么都不会留下。
        /// </summary>
        public float Hope = 0.5f;

        /// <summary>
        /// 我的人生、经历、价值观，在我自己看来是否能组成一个说得通的整体？
        /// 比如0.85 - 我知道自己经历了什么，也大概知道这些事情意味着什么。0.2 - 我曾经相信努力会得到回报，但好人还是死了，坏人活着。我不知道这个世界到底有什么规律。
        /// 重大创伤以后，Coherence 很可能下降。哲学、宗教、成熟的人生叙事则可能帮助它恢复。
        /// </summary>
        public float Coherence = 0.5f;

        /// <summary>
        /// 这个 Pawn 是否觉得自己的生命连接到某种超越个人即时利益的更大意义？
        /// 它不等于：BeliefInGod
        /// 一个无神论 Pawn 也可以：Transcendence = 0.80，BeliefInGod = 0.05 - 人类文明、真理、下一代、科学、正义，比自己个人生命更重要。
        /// 一个宗教 Pawn 也可能：BeliefInGod = 0.90，Transcendence = 0.30 - 他理论上相信神，却没有觉得自己的日常生活真的连接到这个信仰。
        /// </summary>
        public float Transcendence = 0.3f;

        public MeaningState()
        {
        }

        public void ExposeData()
        {
            Scribe_Values.Look(
                ref Purpose,
                "purpose",
                0.5f);

            Scribe_Values.Look(
                ref Belonging,
                "belonging",
                0.5f);

            Scribe_Values.Look(
                ref Hope,
                "hope",
                0.5f);

            Scribe_Values.Look(
                ref Coherence,
                "coherence",
                0.5f);

            Scribe_Values.Look(
                ref Transcendence,
                "transcendence",
                0.3f);
        }
    }
}