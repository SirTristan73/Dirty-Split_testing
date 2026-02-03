using UnityEngine;


public class BaseWeapon : ScriptableObject
{
    [SerializeField] private string _weaponName = "Base Weapon";
    [SerializeField] private float _damage = 10f;
    [SerializeField] private float _range = 100f;
    [SerializeField] private float _fireRate = 1f;
    [SerializeField] private GameObject _weaponPrefab;

    public string WeaponName => _weaponName;
    public float Damage => _damage;
    public float Range => _range;
    public float FireRate => _fireRate;
    public GameObject WeaponPrefab => _weaponPrefab;

}
