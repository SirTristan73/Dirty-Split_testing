using UnityEngine;
using Mirror;
using System.Linq;

public class WorldControler : NetworkBehaviour
{
    public static WorldControler Instance;
    public SpawnPoint[] _spawnPoints;
    private int _nextSpawnIndex = 0;
    [SerializeField] private GameObject npcPrefab; // префаб с NetworkIdentity
    [SerializeField] private Transform[] npcSpawnPoints;

    [SerializeField] private GameObject _doorPrefab; // префаб с NetworkIdentity
    [SerializeField] private Transform[] _doorSpawnPoints;

    [SerializeField] private GameObject _trapPrefab; // префаб с NetworkIdentity
    [SerializeField] private Transform[] _trapSpawnPoints;


    void Awake()
    {
        Instance = this;
    }

    

    [Server]
    public void StartHeist()
    {
        RpcHeistStarted();
    }

    [ClientRpc]
    void RpcHeistStarted()
    {
        Debug.Log("Ограбление началось, молитесь");
    }

        public override void OnStartServer()
    {
        base.OnStartServer();

        SpawnNPCs();

        SpawnDoors();
        SpawnTraps();

        // чтобы не всегда один и тот же ад
        _spawnPoints = _spawnPoints
            .OrderBy(x => Random.value)
            .ToArray();
    }

    [Server]
    public Transform GetNextPlayerSpawn()
    {
        if (_spawnPoints.Length == 0)
        {
            Debug.LogError("Нет SpawnPoint'ов. Игроки появятся в пустоте.");
            return null;
        }

        var spawn = _spawnPoints[_nextSpawnIndex];
        _nextSpawnIndex = (_nextSpawnIndex + 1) % _spawnPoints.Length;
        return spawn.transform;
    }

     
    [Server]
    void SpawnNPCs()
    {
        foreach (var spawnPoint in npcSpawnPoints)
        {
            GameObject npc = Instantiate(npcPrefab, spawnPoint.position, spawnPoint.rotation);
            NetworkServer.Spawn(npc); // критично!
        }
    }

    [Server]
    void SpawnDoors()
    {
        foreach (var spawnPoint in _doorSpawnPoints)
        {
            GameObject door = Instantiate(_doorPrefab, spawnPoint.position, spawnPoint.rotation);
            NetworkServer.Spawn(door); // критично!
        }
    }

    [Server]
    void SpawnTraps()
    {
        foreach (var spawnPoint in _trapSpawnPoints)
        {
            GameObject trap = Instantiate(_trapPrefab, spawnPoint.position, spawnPoint.rotation);
            NetworkServer.Spawn(trap); // критично!
        }
    }
}