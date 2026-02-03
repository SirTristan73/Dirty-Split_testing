using UnityEngine;

public class WeaponRuntime : MonoBehaviour
{
    [SerializeField] private ParticleSystem _shootEffect;

    public void Shoot()
    {
        if (_shootEffect != null)
        {
            _shootEffect.Play();
        }
    }
}
