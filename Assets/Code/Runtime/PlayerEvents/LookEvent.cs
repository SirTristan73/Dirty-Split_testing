using UnityEngine;
using EventBus;
using EventType = EventBus.EventType;

public class LookEvent : EventType
{
    public Vector2 LookInput { get; private set; }

    public LookEvent(Vector2 lookInput)
    {
        LookInput = lookInput;
    }
}
