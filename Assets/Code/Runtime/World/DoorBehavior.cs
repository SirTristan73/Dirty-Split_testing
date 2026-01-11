using UnityEngine;
using Mirror;

public class DoorBehavior : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnDoorStateChanged))]
    private bool _isOpen = false;

    [SerializeField] private Transform _doorVisual;
    [SerializeField] private float _openAngle = 90f;

    public override void OnStartClient()
    {
        ApplyVisual();
    }

    [Command(requiresAuthority = false)]
    public void CmdToggleDoor()
    {
        Debug.Log("Toggling door state on server.");
        _isOpen = !_isOpen;
    }

    void OnDoorStateChanged(bool oldValue, bool newValue)
    {
        ApplyVisual();
    }

    void ApplyVisual()
    {
        float angle = _isOpen ? _openAngle : 0f;
        _doorVisual.localRotation = Quaternion.Euler(0, angle, 0);
    }
}
