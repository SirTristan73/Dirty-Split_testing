using UnityEngine;
using Mirror;
using UnityEngine.UI;
using TMPro;

public class NPC_behaviour : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnHPChanged))]
    private float _currentHP;

    [SerializeField] private float _maxHP = 100f;
    [SerializeField] private Slider _hpBar;
    [SerializeField] private TMP_Text _nameText;

    private Camera _camera;
    private Transform _uiRoot;

    private void Start()
    {
        if (isServer)
            _currentHP = _maxHP;

        _uiRoot = _hpBar.transform.parent;

        if (isClient)
            _camera = Camera.main;

        UpdateHPBar();
    }

    private void LateUpdate()
    {
        if (_camera == null)
            _camera = Camera.main;

        if (_camera != null && _uiRoot != null)
        {
            _uiRoot.LookAt(_camera.transform);
        }
    }

    [Server]
    public void TakeDamage(float amount)
    {
        _currentHP -= amount;
        if (_currentHP <= 0f)
        {
            _currentHP = 0f;
            NetworkServer.Destroy(gameObject); // удаляем NPC на всех клиентах
        }
    }

    void OnHPChanged(float oldHP, float newHP)
    {
        UpdateHPBar();
    }

    void UpdateHPBar()
    {
        if (_hpBar != null)
        {
            _hpBar.value = _currentHP / _maxHP;
            _nameText.text = $"NPC {_currentHP}/{_maxHP}";
        }
    }
}
