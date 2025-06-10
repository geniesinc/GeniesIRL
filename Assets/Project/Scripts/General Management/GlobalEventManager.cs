using System;
using System.Collections.Generic;

namespace GeniesIRL 
{
    /// <summary>
    /// Facilitates the triggering and listening of global events throughout the game. To access a list of events to trigger 
    /// or subscribe to, look in the GeniesIRL.GlobalEvents namespace.
    /// </summary>
    public static class GlobalEventManager
    {
        // Dictionary to hold event types and their respective delegate lists.
        private static readonly Dictionary<Type, Delegate> eventTable = new Dictionary<Type, Delegate>();

        ///Subscribe to an event of a specific type.
        public static void Subscribe<T>(Action<T> listener)
        {
            var eventType = typeof(T);
            if (!eventTable.ContainsKey(eventType))
            {
                eventTable[eventType] = null;
            }
            eventTable[eventType] = (Action<T>)eventTable[eventType] + listener;
        }

        // Unsubscribe from an event of a specific type.
        public static void Unsubscribe<T>(Action<T> listener)
        {
            var eventType = typeof(T);
            if (eventTable.ContainsKey(eventType))
            {
                eventTable[eventType] = (Action<T>)eventTable[eventType] - listener;
                if (eventTable[eventType] == null)
                {
                    eventTable.Remove(eventType);
                }
            }
        }

        // Trigger an event of a specific type with arguments.
        public static void Trigger<T>(T eventArgs)
        {
            var eventType = typeof(T);
            if (eventTable.ContainsKey(eventType))
            {
                var callback = eventTable[eventType] as Action<T>;
                callback?.Invoke(eventArgs);
            }
        }
    }

}
