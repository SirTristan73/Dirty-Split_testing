using UnityEngine;
using Steamworks;

public class SteamManager : MonoBehaviour
{
    public static SteamManager Instance { get; private set; }
    public static bool Initialized { get; private set; }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (Initialized)
            return;

        try
        {
            if (!SteamAPI.Init())
            {
                Debug.LogError("SteamAPI.Init() вернул false. Steam мёртв.");
                SteamAPI.Init();

                return;
            }

            Initialized = true;
            Debug.Log("Steam инициализирован (Steamworks.NET).");
        }
        catch (System.Exception e)
        {
            Debug.LogError("Steam инициализация не удалась: " + e);
        }
    }

    private void Update()
    {
        if (Initialized)
            SteamAPI.RunCallbacks();
    }

    private void OnApplicationQuit()
    {
        if (Initialized)
            SteamAPI.Shutdown();
    }
}
