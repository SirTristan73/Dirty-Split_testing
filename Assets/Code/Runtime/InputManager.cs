using UnityEngine;
using static UnityEngine.InputSystem.InputAction;
using EventBus;

public class InputManager : MonoBehaviour
{
    public void OnMoveCallback(CallbackContext context)
    {
        EventBus.EventBus.Trigger(new MoveEvent(context.ReadValue<Vector2>()));
    }

    public void OnShootCallback(CallbackContext context)
    {
        if (context.performed)
        {
            EventBus.EventBus.Trigger(new ShootEvent());   
        }
    }

    public void OnLookCallback(CallbackContext context)
    {
        EventBus.EventBus.Trigger(new LookEvent(context.ReadValue<Vector2>()));
    }
}
