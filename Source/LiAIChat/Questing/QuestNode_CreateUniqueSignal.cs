using RimWorld.QuestGen;
using System;
using Verse;

namespace LiAIChat.Questing
{
    public class QuestNode_CreateUniqueSignal : QuestNode
    {
        public string storeAs;

        public string prefix;

        protected override bool TestRunInt(
    Slate slate)
        {
            if (string.IsNullOrEmpty(storeAs))
                return false;

            string actualPrefix =
                string.IsNullOrEmpty(prefix)
                    ? "LiAIChat_Signal"
                    : prefix;

            string signal =
                actualPrefix +
                "_TEST_" +
                Guid.NewGuid()
                    .ToString("N");

            slate.Set(
                storeAs,
                signal);

            return true;
        }

        protected override void RunInt()
        {
            Slate slate =
                QuestGen.slate;

            if (string.IsNullOrEmpty(
                storeAs))
            {
                Log.Warning(
                    "[Li AI Chat] CreateUniqueSignal: " +
                    "storeAs was empty.");

                return;
            }

            string actualPrefix =
                string.IsNullOrEmpty(prefix)
                    ? "LiAIChat_Signal"
                    : prefix;

            string signal =
                actualPrefix +
                "_" +
                Guid.NewGuid()
                    .ToString("N");

            slate.Set(
                storeAs,
                signal);

            Log.Message(
                "[Li AI Chat] Created unique quest signal: " +
                signal +
                ", SlateKey=" +
                storeAs);
        }
    }
}