using System;
using System.Collections.Generic;

namespace MultiServerBasic
{
    public class ThreadManager
    {
        private readonly List<Action> actionToExecute = new List<Action>(); //List of further action to do that will be removed after the update
        private readonly List<Action> registeredActions = new List<Action>(); //List of further action to do that will be keep for the updates until you unregister it

        /// <summary>Add an action to do on the main thread (will be removed after the update)</summary>
        /// <param name="action">The action to do</param>
        public void ExecuteOnMainThread(Action action) {
            actionToExecute.Add(action);
        }

        /// <summary>
        /// Register an action to do on the main thread (will be keep for the updates until you unregister it)
        /// /!\ BE CAREFUL WHEN REGISTER AN ACTION THAT CAN REGISTER OR UNREGISTER OTHER ACTIONS /!\
        /// </summary>
        /// <param name="action">The action to do</param>
        public void RegisterActionOnMainThread(Action action) {
            registeredActions.Add(action);
        }

        /// <summary>Unregister an action to remove it from the update loop</summary>
        /// <param name="action">The action to remove</param>
        public void UnregisterActionOnMainThread(Action action) {
            registeredActions.Remove(action);
        }

        /// <summary>Update the main thread</summary>
        public void Update()
        {
            //Change the storage of action to not let the actions add an action and do it in the same update or made an internal loop
            Action[] bufferActionToExecute = actionToExecute.ToArray(); 
            actionToExecute.Clear();
            
            for (int i = 0; i < bufferActionToExecute.Length; i++) {
                bufferActionToExecute[i]();
            }

            for (int i = 0; i < registeredActions.Count; i++) {
                registeredActions[i]();
            }
        }
    }
}