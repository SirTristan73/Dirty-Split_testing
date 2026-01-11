using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using Unity.Collections;
using UnityEngine;

namespace EventBus
{
    public static class EventBus
    {
        private static Dictionary<Type, Delegate> assignedActions = new();


        public static void Trigger(EventType data)
        {
            Type type = data.GetType();
            if (assignedActions.TryGetValue(type, out Delegate existingAction))
            {
                existingAction?.DynamicInvoke(data);
            }
        }


        public static void SubscribeToEvent<T>(Action<T> action) where T : EventType
        {
            Type type = typeof(T);

            if (assignedActions.ContainsKey(type))
            {
                assignedActions[type] = Delegate.Combine(assignedActions[type], action);
            }
            else
            {
                assignedActions[type] = action;
            }
        }


        public static void UnsubscribeFromEvent<T>(Action<T> action) where T : EventType
        {
            Type type = typeof(T);

            if (assignedActions.ContainsKey(type))
            {
                assignedActions[type] = Delegate.Remove(assignedActions[type], action);
            }
        }
    }

}