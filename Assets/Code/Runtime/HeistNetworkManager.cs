using UnityEngine;
using Mirror;

public class HeistNetworkManager : NetworkManager
{
    [SerializeField] private GameObject _mapPrefab;

    [Server]
    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        Transform start = WorldControler.Instance.GetNextPlayerSpawn();

        GameObject player = Instantiate(
            playerPrefab,
            start.position,
            start.rotation
        );

        NetworkServer.AddPlayerForConnection(conn, player);
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        SpawnMap();
    }

    [Server]
    void SpawnMap()
    {
        GameObject map = Instantiate(_mapPrefab);
        NetworkServer.Spawn(map);
    }
}
