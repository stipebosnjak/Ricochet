using System;
using System.Collections.Generic;

namespace DedicatedServer
{
    class ThreadManager
    {
        private static readonly List<Action> ToBeExecuted = new List<Action>();
        private static readonly List<Action> ToBeCopiedAndExecuted = new List<Action>();
        private static bool _actionToExecuteOnMainThread = false;

        /// <summary>Sets an action to be executed on the main thread.</summary>
        /// <param name="action">The action to be executed on the main thread.</param>
        public static void ExecuteOnMainThread(Action action)
        {
            if (action == null)
            {
                Console.WriteLine("No action to execute on main thread!");
                return;
            }

            lock (ToBeExecuted)
            {
                ToBeExecuted.Add(action);
                _actionToExecuteOnMainThread = true;
            }
        }

        /// <summary>Executes all code meant to run on the main thread. NOTE: Call this ONLY from the main thread.</summary>
        public static void UpdateMain()
        {
            if (_actionToExecuteOnMainThread)
            {
                ToBeCopiedAndExecuted.Clear();
                lock (ToBeExecuted)
                {
                    ToBeCopiedAndExecuted.AddRange(ToBeExecuted);
                    ToBeExecuted.Clear();
                    _actionToExecuteOnMainThread = false;
                }

                for (int i = 0; i < ToBeCopiedAndExecuted.Count; i++)
                {
                    ToBeCopiedAndExecuted[i]();
                }
            }
        }
    }
}
