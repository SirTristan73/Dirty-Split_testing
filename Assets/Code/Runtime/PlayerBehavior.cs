using UnityEngine;
using EventBus;
using Mirror;

public class PlayerBehavior : NetworkBehaviour
{
[Header("Movement")]
    [SerializeField] private float _speed = 5f;
    private Vector3 _movementInput;

    [Header("Look")]
    [SerializeField] private Camera _playerCamera;
    private Vector2 _lookInput;
    private float _lookRotationX = 0f;
    [SerializeField] private float _lookSensitivity = 100f;

    [Header("Shooting")]
    [SerializeField] private float _shootRange = 50f;
    [SerializeField] private float _shootDamage = 25f;

    [Header("Animation")]
    [SerializeField] private Animator _animator;


    private void OnEnable()
    {
        EventBus.EventBus.SubscribeToEvent<MoveEvent>(OnMoveEvent);
        EventBus.EventBus.SubscribeToEvent<LookEvent>(OnLookEvent);
        EventBus.EventBus.SubscribeToEvent<ShootEvent>(OnShootEvent);
    }

    private void OnDisable()
    {
        EventBus.EventBus.UnsubscribeFromEvent<MoveEvent>(OnMoveEvent);
        EventBus.EventBus.UnsubscribeFromEvent<LookEvent>(OnLookEvent);
        EventBus.EventBus.UnsubscribeFromEvent<ShootEvent>(OnShootEvent);
    }

    private void Update()
    {
        if (!isLocalPlayer) return;

        // Вращение игрока по Y
        transform.Rotate(Vector3.up, _lookInput.x * _lookSensitivity * Time.deltaTime);

        // Вращение камеры по X
        _lookRotationX += -_lookInput.y * _lookSensitivity * Time.deltaTime;
        _lookRotationX = Mathf.Clamp(_lookRotationX, -90f, 90f);
        if (_playerCamera != null)
        {
            _playerCamera.transform.localRotation = Quaternion.Euler(_lookRotationX, 0, 0);
        }


            UpdateAnimation(true);

    }

    private void FixedUpdate()
    {
        if (!isLocalPlayer) return;
        transform.Translate(_movementInput * _speed * Time.fixedDeltaTime, Space.Self);
    }

    private void OnMoveEvent(MoveEvent e)
    {
        _movementInput = new Vector3(e.MovementInput.x, 0, e.MovementInput.y);
    }

    private void OnLookEvent(LookEvent e)
    {
        _lookInput = e.LookInput;
    }

    private void OnShootEvent(ShootEvent e)
    {
        if (!isLocalPlayer) return;

        UpdateAnimation(false);
        // Двери
        if (Physics.Raycast(_playerCamera.transform.position, _playerCamera.transform.forward, out RaycastHit doorHit, 2f))
        {
            if (doorHit.collider.TryGetComponent<DoorBehavior>(out var door))
            {

                    door.CmdToggleDoor();
            }
        }

        // NPC
if (Physics.Raycast(_playerCamera.transform.position, _playerCamera.transform.forward, out RaycastHit npcHit, _shootRange))
{
    // Получаем NPC_behaviour с родителя (или самого объекта)
    NPC_behaviour npc = npcHit.collider.GetComponentInParent<NPC_behaviour>();
    if (npc != null)
    {
        CmdHitNPC(npc.netIdentity.netId, _shootDamage);
        Debug.Log($"Client hit NPC netId: {npc.netIdentity.netId}");
    }
}
    }

    [Command(requiresAuthority = false)]
    private void CmdHitNPC(uint npcNetId, float damage)
    {
        if (!NetworkServer.spawned.TryGetValue(npcNetId, out var identity))
        {
            Debug.LogWarning("CmdHitNPC: NPC не найден на сервере!");
            return;
        }

        if (identity.TryGetComponent<NPC_behaviour>(out var npc))
        {
            npc.TakeDamage(damage);
            Debug.Log($"NPC hit on server: {npc.name}, damage: {damage}");
        }
    }

    private void UpdateAnimation(bool isMoving)
    {
        if (isMoving)
        {
            float speed = new Vector3(_movementInput.x, 0, _movementInput.z).magnitude;

            _animator.SetInteger("State", (int)speed);
        }
        else
        {
            _animator.SetTrigger("Attack");
        }
    }
}