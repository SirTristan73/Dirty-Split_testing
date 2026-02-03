using UnityEngine;
using UnityEngine.UI;
using Steamworks;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;

public class SteamFriendsPanel : MonoBehaviour
{
    [Header("UI")]
    public Transform ContentRoot;
    public GameObject FriendPrefab;

    private readonly List<GameObject> spawned = new();

    void Start()
    {
        if (!SteamManager.Initialized)
        {
            Debug.LogError("Steam не инициализирован. Вселенная отменяется.");
            return;
        }

        RefreshFriends();
    }

    void OnDestroy()
    {
        ClearUI();
    }

    // =========================
    // MAIN
    // =========================

    void RefreshFriends()
    {
        ClearUI();

        int count = SteamFriends.GetFriendCount(EFriendFlags.k_EFriendFlagImmediate);

        for (int i = 0; i < count; i++)
        {
            var id = SteamFriends.GetFriendByIndex(i, EFriendFlags.k_EFriendFlagImmediate);
            CreateFriendItem(id);
        }
    }

    async void CreateFriendItem(CSteamID id)
    {
        var obj = Instantiate(FriendPrefab, ContentRoot);
        spawned.Add(obj);

        var name = SteamFriends.GetFriendPersonaName(id);
        var state = SteamFriends.GetFriendPersonaState(id);

        // TEXT
        var text = obj.GetComponentInChildren<TMP_Text>();
        if (text != null)
        {
            text.text = $"{name}\n{StatusToString(state)}";
            text.color = state == EPersonaState.k_EPersonaStateOnline
                ? Color.white
                : Color.gray;
        }

        // AVATAR
        var image = obj.GetComponentInChildren<RawImage>();
        if (image != null)
        {
            await LoadAvatar(id, image, state == EPersonaState.k_EPersonaStateOnline);
        }
    }

    // =========================
    // AVATAR
    // =========================

    public static async Task LoadAvatar(CSteamID id, RawImage image, bool online)
    {
        int handle = SteamFriends.GetLargeFriendAvatar(id);

        while (handle == -1)
            await Task.Delay(50);

        if (handle <= 0) return;

        SteamUtils.GetImageSize(handle, out uint w, out uint h);
        byte[] data = new byte[w * h * 4];

        if (!SteamUtils.GetImageRGBA(handle, data, (int)(w * h * 4)))
            return;

        var tex = new Texture2D((int)w, (int)h, TextureFormat.RGBA32, false);
        tex.LoadRawTextureData(data);
        tex.Apply();

        image.texture = tex;
        image.color = online ? Color.white : new Color(1, 1, 1, 0.35f);
    }

    // =========================
    // UTILS
    // =========================

    string StatusToString(EPersonaState state) => state switch
    {
        EPersonaState.k_EPersonaStateOnline => "🟢 Online",
        EPersonaState.k_EPersonaStateAway => "🟡 Away",
        EPersonaState.k_EPersonaStateBusy => "🔴 Busy",
        _ => "⚫ Offline"
    };

    // =========================
    // CLEANUP
    // =========================

    void ClearUI()
    {
        foreach (var go in spawned)
            if (go) Destroy(go);

        spawned.Clear();
    }
}
