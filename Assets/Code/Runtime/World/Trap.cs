using UnityEngine;
using Mirror;

public class Trap : NetworkBehaviour
{
    [SerializeField] private Rigidbody _ballRigidbody;
    [SerializeField] private ParticleSystem _hitEffect;
    [SerializeField] private float _launchForce = 5f;

    public override void OnStartServer()
    {
        // стартовое катание
        LaunchBall();
    }

    [Server]
    void LaunchBall()
    {
        _ballRigidbody.AddForce(Vector3.forward * _launchForce, ForceMode.VelocityChange);
    }

  [ServerCallback]
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.TryGetComponent<PlayerBehavior>(out var player))
        {
            // Вызываем у всех клиентов
            RpcPlayHitEffect(collision.contacts[0].point);
        }
    }

    [ClientRpc]
    void RpcPlayHitEffect(Vector3 position)
    {
        if (_hitEffect != null)
        {
            _hitEffect.transform.position = position;
            _hitEffect.Play();
        }
    }

    [ClientRpc]
    void RpcHitPlayer(NetworkIdentity playerNetId)
    {
        if (playerNetId.TryGetComponent<PlayerBehavior>(out var player))
        {
            Debug.Log("Player hit by rolling ball: " + player.name);
            // тут можно сделать эффект, урон, звук и т.д.
        }
    }
}
