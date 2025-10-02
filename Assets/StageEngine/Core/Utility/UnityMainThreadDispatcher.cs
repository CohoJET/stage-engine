using System;
using System.Collections.Concurrent;

namespace StageEngine.Core.Utility
{
    /// <summary>
    /// Helper class to execute actions on Unity's main thread from background threads
    /// </summary>
    public class UnityMainThreadDispatcher : Singleton<UnityMainThreadDispatcher>
    {
        private static readonly ConcurrentQueue<Action> ExecutionQueue = new ConcurrentQueue<Action>();

        private void Update()
        {
            // Execute all queued actions on the main thread
            while (ExecutionQueue.TryDequeue(out Action action))
            {
                try
                {
                    action?.Invoke();
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error executing main thread action: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Enqueue an action to be executed on the main thread during the next Update
        /// </summary>
        public void Enqueue(Action action)
        {
            if (action == null) return;
            ExecutionQueue.Enqueue(action);
        }

        /// <summary>
        /// Check if we're currently on the main thread
        /// </summary>
        public bool IsMainThread()
        {
            return System.Threading.Thread.CurrentThread.ManagedThreadId == 1;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}