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
            EventBus.EventBus.Trigger(new ShootEvent(true));   
        }
        else if (context.canceled)
        {
            EventBus.EventBus.Trigger(new ShootEvent(false));
        }
    }

    public void OnLookCallback(CallbackContext context)
    {
        EventBus.EventBus.Trigger(new LookEvent(context.ReadValue<Vector2>()));
    }

    public void OnSwitchWeaponCallback(CallbackContext context)
    {
        if (context.performed)
        {
            EventBus.EventBus.Trigger(new SwitchWeaponEvent());
        }
    }

    public void OnInteractCallback(CallbackContext context)
    {
        if (context.performed)
        {
            EventBus.EventBus.Trigger(new InteractEvent());
        }
    }
}
