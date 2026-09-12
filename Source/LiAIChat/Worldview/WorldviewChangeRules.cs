namespace LiAIChat.Worldview
{
    public static class WorldviewChangeRules
    {
        // Director 输出绝对上限
        public const float DirectorDeltaLimit = 0.05f;

        // 一次普通对话真正允许的最大变化
        public const float MaxAppliedDeltaPerConversation = 0.015f;

        // Knowledge 可以稍快，因为“学到知识”
        // 不等于“改变信仰”
        public const float MaxKnowledgeDeltaPerConversation = 0.03f;

        // 接近极端值以后，变化逐渐困难
        public const float ExtremeResistanceStart = 0.80f;

        // 非常微弱的变化直接忽略
        public const float MinimumMeaningfulDelta = 0.001f;
    }
}