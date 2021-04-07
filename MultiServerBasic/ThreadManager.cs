using System;
using System.Collections.Generic;

namespace MultiServerBasic
{
    public class ThreadManager
    {
        private readonly List<Action> actionToExecute = new List<Action>(); //List of further action to do that will be removed after the update
        private readonly List<Action> safeActionToExecuteCollector = new List<Action>(); //List of further action to do that will be removed after the update stored on this list when actionToExecute can be used
        private readonly List<Action> registeredActions = new List<Action>(); //List of further action to do that will be keep for the updates until you unregister it
        private readonly List<Action> safeRegisteredActionsCollector = new List<Action>(); //List of further action to do that will be keep for the updates until you unregister it when registeredActions can be used
        private readonly List<Action> safeUnregisteredActionsCollector = new List<Action>(); //List of further action to do that will be keep for the updates until you unregister it when registeredActions can be used
        
        private bool safeMode;
        
        /// <summary>Add an action to do on the main thread (will be removed after the update)</summary>
        /// <param name="action">The action to do</param>
        public void ExecuteOnMainThread(Action action) {
            if (!safeMode)
            {
                actionToExecute.Add(action);
            }
            else
            {
                safeActionToExecuteCollector.Add(action);
            }
            
        }

        /// <summary>
        /// Register an action to do on the main thread (will be keep for the updates until you unregister it
        /// /!\ BE CAREFUL WHEN REGISTER AN ACTION THAT CAN REGISTER OR UNREGISTER OTHER ACTIONS /!\
        /// </summary>
        /// <param name="action">The action to do</param>
        public void RegisterActionOnMainThread(Action action) {
            if (!safeMode)
            {
                registeredActions.Add(action);
            }
            else
            {
                if (safeUnregisteredActionsCollector.Contains(action))
                {
                    safeUnregisteredActionsCollector.Remove(action);
                }
                else
                {
                    safeRegisteredActionsCollector.Add(action);
                }
                    
            }
            
        }

        /// <summary>Unregister an action to remove it from the update loop</summary>
        /// <param name="action">The action to remove</param>
        public void UnregisterActionOnMainThread(Action action) {
            if (!safeMode)
            {
                registeredActions.Remove(action);
            }
            else
            {
                if (safeRegisteredActionsCollector.Contains(action))
                {
                    safeRegisteredActionsCollector.Remove(action);
                }
                else
                {
                    safeUnregisteredActionsCollector.Add(action);
                }
            }
        }

        /// <summary>Update the main thread</summary>
        public void Update()
        {
            safeMode = true; // make variable used unchanged during the execution.
            
            
            for (int i = 0; i < actionToExecute.Count; i++) {
                actionToExecute[i]();
            }

            for (int i = 0; i < registeredActions.Count; i++) {
                registeredActions[i]();
            }

            actionToExecute.Clear();
            
            #region safeModeReset
            safeMode = false;
            
            actionToExecute.AddRange(safeActionToExecuteCollector);
            registeredActions.AddRange(safeRegisteredActionsCollector);
            foreach (Action action in safeUnregisteredActionsCollector)
            {
                registeredActions.Remove(action);
            }
            
            #endregion
        }
    }
}