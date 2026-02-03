using UnityEngine;
using EventBus;
using EventType = EventBus.EventType;

public class ShootEvent : EventType
{
    public bool IsShooting { get; private set; }
    public ShootEvent(bool isShooting)
    {
        IsShooting = isShooting;
    }
}
