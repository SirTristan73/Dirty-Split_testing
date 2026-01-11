using UnityEngine;
using Steamworks;

public class SteamManager : MonoBehaviour
{
    public static SteamManager Instance { get; private set; }
    public bool Initialized { get; private set; }

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        try
        {
            SteamClient.Init(480); // AppID 480 для теста
            Initialized = true;
            Debug.Log("Steam инициализирован: " + Steamworks.SteamClient.Name);
        }
        catch (System.Exception e)
        {
            Initialized = false;
            Debug.LogError("Steam инициализация не удалась: " + e.Message);
        }
    }

    void Update()
    {
        if (Initialized)
            SteamClient.RunCallbacks();
    }

    void OnApplicationQuit()
    {
        if (Initialized)
            SteamClient.Shutdown();
    }
}
