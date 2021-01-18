using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts
{
    public class ThreadManager : MonoBehaviour
    {
        private static readonly List<Action> executeOnMainThread = new List<Action>();
        private static readonly List<Action> ExecuteCopiedOnMainThread = new List<Action>();
        private static bool _actionToExecuteOnMainThread = false;

        private void Update()
        {
            UpdateMain();
        }

        /// <summary>Sets an action to be executed on the main thread.</summary>
        /// <param name="_action">The action to be executed on the main thread.</param>
        public static void ExecuteOnMainThread(Action _action)
        {
            if (_action == null)
            {
                Debug.Log("No action to execute on main thread!");
                return;
            }

            lock (executeOnMainThread)
            {
                executeOnMainThread.Add(_action);
                _actionToExecuteOnMainThread = true;
            }
        }

        /// <summary>Executes all code meant to run on the main thread. NOTE: Call this ONLY from the main thread.</summary>
        public static void UpdateMain()
        {
            if (_actionToExecuteOnMainThread)
            {
                ExecuteCopiedOnMainThread.Clear();
                lock (executeOnMainThread)
                {
                    ExecuteCopiedOnMainThread.AddRange(executeOnMainThread);
                    executeOnMainThread.Clear();
                    _actionToExecuteOnMainThread = false;
                }

                foreach (var t in ExecuteCopiedOnMainThread)
                {
                    t();
                }
            }
        }
    }
}