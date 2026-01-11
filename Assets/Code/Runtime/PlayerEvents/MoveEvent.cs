using UnityEngine;

namespace EventBus
{
    public class MoveEvent : EventType
    {
        public Vector2 MovementInput  { get; private set; }

        public MoveEvent(Vector2 movementInput)
        {
            MovementInput = movementInput;
        }
    }
}