using Mirror;
using UnityEngine;

public class SteamNetworkManager : NetworkManager
{
    public override void Start()
    {
        base.Start();
        Debug.Log("Steam Network Manager готов к страданиям");
    }
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
