using UnityEngine;
using Mirror;

public class WorldState : NetworkBehaviour
{
    [SyncVar] public bool _alarmActive;
    [SyncVar] public int _currentHeat;
}