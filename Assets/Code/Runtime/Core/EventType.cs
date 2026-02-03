using UnityEngine;

namespace EventBus
{
    public class EventType
    {
        public float _cooldown { get; private set; }

        public EventType(float cooldown = 0)
        {
            _cooldown = cooldown;
        }

    }
}