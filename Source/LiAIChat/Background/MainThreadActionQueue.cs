using System;
using System.Collections.Generic;
using Verse;

namespace LiAIChat.Background
{
    public static class MainThreadActionQueue
    {
        private static readonly Queue<Action>
            actions =
                new Queue<Action>();

        private static readonly object syncRoot =
            new object();

        public static void Enqueue(
            Action action)
        {
            if (action == null)
                return;

            lock (syncRoot)
            {
                actions.Enqueue(action);
            }
        }

        public static void Process()
        {
            while (true)
            {
                Action action = null;

                lock (syncRoot)
                {
                    if (actions.Count == 0)
                        break;

                    action =
                        actions.Dequeue();
                }

                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    Log.Warning(
                        "[Li AI Chat] Main-thread action failed: " +
                        ex);
                }
            }
        }
    }
}