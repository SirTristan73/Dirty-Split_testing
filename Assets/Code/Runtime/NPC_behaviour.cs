using UnityEngine;
using Mirror;
using TMPro;

public class NPC_behaviour : NetworkBehaviour, IDamageable
{
    [SyncVar(hook = nameof(OnHPChanged))]
    private float _currentHP;

    [SerializeField] private float _maxHP = 100f;
    [SerializeField] private float _moveSpeed;
    [SerializeField] private float _moveRange;
    private Vector3 _startPosition;
    [SerializeField] private TMP_Text _nameText;
    private Transform _targetTransform;
    
    [SerializeField] private Transform _uiRoot;

    private void Start()
    {
        if (isServer)
        {
            _currentHP = _maxHP;
            _startPosition = transform.position;
        }

        if (isClient)
            FindTarget();

        UpdateHPDisplay();
    }

    private void FindTarget()
    {
        if (NetworkClient.localPlayer != null)
        {
            _targetTransform = NetworkClient.localPlayer.transform;
        }
    }

    private void Update()
    {
        float xOffset = Mathf.PingPong(Time.time * _moveSpeed, _moveRange);
        transform.position = _startPosition + new Vector3(xOffset, 0, 0);
    }

    private void LateUpdate()
    {
        if (_targetTransform == null)
        {
            FindTarget();
            return;
        }

        if (_uiRoot != null)
        {
            Vector3 directionToPlayer = _targetTransform.position - _uiRoot.position;
            directionToPlayer.y = 0;
            if (directionToPlayer.sqrMagnitude > 0.001f)
            {
                _uiRoot.rotation = Quaternion.LookRotation(-directionToPlayer);
            }
        }
    }

    [Server]
    public void TakeDamage(float amount)
    {
        _currentHP -= amount;
        if (_currentHP <= 0f)
        {
            _currentHP = 0f;
            NetworkServer.Destroy(gameObject); 
        }
    }

    void OnHPChanged(float oldHP, float newHP)
    {
        UpdateHPDisplay();
    }

    void UpdateHPDisplay()
    {
        if (_nameText != null)
        {
            _nameText.text = $"NPC {_currentHP}/{_maxHP}";
        }
    }
}