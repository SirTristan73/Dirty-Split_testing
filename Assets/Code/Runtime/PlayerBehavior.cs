using UnityEngine;
using EventBus;
using Mirror;

public class PlayerBehavior : NetworkBehaviour, IDamageable
{
    [Header("Movement")]
    [SerializeField] private float _speed = 5f;
    [SerializeField] private Rigidbody _playerRB;
    private Vector3 _movementInput;

    [Header("Look / Aim")]
    [SerializeField] private Transform _playerTransform;
    [SerializeField] private Vector2 _lookInput;
    [SerializeField] private float _rotationSpeed = 720f;

    [Header("Camera")]
    [SerializeField] private Camera _playerCamera;

    [Header("Shooting")]
    [SerializeField] private Transform _weaponPosition;
    [SerializeField] private BaseWeapon _firstWeapon; //// TODO
    [SerializeField] private BaseWeapon _secondWeapon;////
    private WeaponRuntime _currentWeapon;
    private WeaponRuntime _secondWeaponRuntime;
    private bool _isShooting = false;


    [Header("Animation")]
    [SerializeField] private Animator _animator;

#region \ Unity Methods
    public override void OnStartClient()
    {
        base.OnStartClient();

        if (_playerCamera == null) return;

        bool isLocal = isLocalPlayer;

        _playerCamera.gameObject.SetActive(isLocal);

        var listener = _playerCamera.GetComponent<AudioListener>();
        if (listener)
            listener.enabled = isLocal;
        SpawnWeapon();
    }

    public override void OnStartLocalPlayer()
    {
        EventBus.EventBus.SubscribeToEvent<MoveEvent>(OnMoveEvent);
        EventBus.EventBus.SubscribeToEvent<LookEvent>(OnLookEvent);
        EventBus.EventBus.SubscribeToEvent<ShootEvent>(OnShootEvent);
        EventBus.EventBus.SubscribeToEvent<SwitchWeaponEvent>(OnSwitchWeaponEvent);
        EventBus.EventBus.SubscribeToEvent<InteractEvent>(OnInteractEvent);
    }

    public override void OnStopLocalPlayer()
    {
        EventBus.EventBus.UnsubscribeFromEvent<MoveEvent>(OnMoveEvent);
        EventBus.EventBus.UnsubscribeFromEvent<LookEvent>(OnLookEvent);
        EventBus.EventBus.UnsubscribeFromEvent<ShootEvent>(OnShootEvent);
        EventBus.EventBus.UnsubscribeFromEvent<SwitchWeaponEvent>(OnSwitchWeaponEvent);
        EventBus.EventBus.UnsubscribeFromEvent<InteractEvent>(OnInteractEvent);
    }

    private void Update()
    {
        if (!isLocalPlayer) return;
        RotateCharacterOnly();
        // UpdateAnimation(_movementInput.sqrMagnitude > 0.01f);

        if (_isShooting)
        {
            HandleShooting();
        }
    }

    private void FixedUpdate()
    {
        if (!isLocalPlayer) return;
        Vector3 nextPos = transform.position + (_movementInput * _speed * Time.fixedDeltaTime);
        _playerRB.MovePosition(nextPos);
    }
#endregion
#region  \ Movement and Look
    private void RotateCharacterOnly()
    {
        if (_lookInput.sqrMagnitude < 0.01f) return;
        Vector3 direction = new Vector3(_lookInput.x, 0, _lookInput.y).normalized;
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            _playerTransform.rotation = Quaternion.RotateTowards(_playerTransform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
        }
    }

#endregion
#region \ Event Handlers
    private void OnMoveEvent(MoveEvent e) => _movementInput = new Vector3(e.MovementInput.x, 0, e.MovementInput.y);
    private void OnLookEvent(LookEvent e) => _lookInput = e.LookInput;
    private void OnShootEvent(ShootEvent e)
    {
        if (!isLocalPlayer) return;

        _isShooting = e.IsShooting;
    }
    private void OnSwitchWeaponEvent(SwitchWeaponEvent e)
    {
        if (!isLocalPlayer) return;

        if (_currentWeapon == null || _secondWeaponRuntime == null) return;

        if (_currentWeapon.gameObject.activeSelf)
        {
            _currentWeapon.gameObject.SetActive(false);
            _secondWeaponRuntime.gameObject.SetActive(true);
            _currentWeapon = _secondWeaponRuntime;
        }
        else
        {
            _secondWeaponRuntime.gameObject.SetActive(false);
            _currentWeapon.gameObject.SetActive(true);
        }
    }

    private void OnInteractEvent(InteractEvent e)
    {
        if (!isLocalPlayer) return;

        // Interaction logic here
    }
#endregion
#region  \Shooting

    private void SpawnWeapon()
    {
        _currentWeapon = Instantiate(_firstWeapon.WeaponPrefab, 
                                _weaponPosition.position, 
                                _weaponPosition.rotation, 
                                _weaponPosition)
                                .GetComponent<WeaponRuntime>();

        _secondWeaponRuntime = Instantiate(_secondWeapon.WeaponPrefab,
                                        _weaponPosition.position,
                                        _weaponPosition.rotation,
                                        _weaponPosition)
                                        .GetComponent<WeaponRuntime>();
        _secondWeaponRuntime.gameObject.SetActive(false);
    }

    private void HandleShooting()
    {
                // _animator.SetTrigger("Attack");

        // локальный эффект
        if (_currentWeapon == null) return;
        _currentWeapon.Shoot();

        CmdFire(_weaponPosition.position, _weaponPosition.forward);
    }

    [Command]
    private void CmdFire(Vector3 position, Vector3 forward)
    {
        float range = _firstWeapon.Range;
        float damage = _firstWeapon.Damage;

        if (Physics.Raycast(position, forward, out RaycastHit hit, range))
        {
            if (hit.collider.transform.root == transform) return;

            if (hit.collider.TryGetComponent<IDamageable>(out var damageable))
            {
                damageable.TakeDamage(damage);
            }
        }

        RpcFire();
    }

    [ClientRpc]
    private void RpcFire()
    {
        if (isLocalPlayer) return; 
        _currentWeapon.Shoot();
    }

    public void TakeDamage(float amount)
    {
        // Реализация получения урона игроком
        Debug.Log($"Player {netId} took {amount} damage.");
    }
#endregion
    private void UpdateAnimation(bool isMoving) => _animator.SetBool("IsMoving", isMoving);
}