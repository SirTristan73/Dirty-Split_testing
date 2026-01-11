using System.Collections.Generic;
using System.Threading.Tasks;
using Steamworks;
using Steamworks.Data;
using UnityEngine;
using UnityEngine.UI;

public class SteamFriendsPanel : MonoBehaviour
{
    [Header("UI")]
    public Transform ContentRoot;
    public GameObject FriendPrefab;

    private readonly List<GameObject> _spawned = new();

    void Start()
    {
        if (!SteamClient.IsValid)
        {
            Debug.LogError("SteamClient не инициализирован. Steam выключен или Init не вызван.");
            return;
        }

        DisplayFriends();
    }

    void OnDestroy()
    {
        ClearUI();
    }

    // =========================
    // MAIN
    // =========================

    void DisplayFriends()
    {
        ClearUI();

        foreach (var friend in SteamFriends.GetFriends())
        {
            CreateFriendItem(friend);
        }
    }

    async void CreateFriendItem(Friend friend)
    {
        var obj = Instantiate(FriendPrefab, ContentRoot);
        _spawned.Add(obj);

        // TEXT
        var text = obj.GetComponentInChildren<TMPro.TMP_Text>();
        if (text != null)
        {
            string status =
                friend.IsOnline ? "🟢 Online" :
                friend.IsAway ? "🟡 Away" :
                friend.IsBusy ? "🔴 Busy" :
                "⚫ Offline";

            text.text = $"{friend.Name}\n{status}";
            text.color = friend.IsOnline ? UnityEngine.Color.white : UnityEngine.Color.gray;
        }

        // AVATAR
        var image = obj.GetComponentInChildren<RawImage>();
        if (image != null)
        {
            await LoadAvatar(friend.Id, image, friend.IsOnline);
        }

        var nick = obj.GetComponentInChildren<TMPro.TMP_Text>();
        if (nick != null)
        {
            nick.text = friend.Name;
        }
    }

    // =========================
    // AVATAR
    // =========================

    async Task LoadAvatar(SteamId id, RawImage image, bool isOnline)
    {
        var avatar = await SteamFriends.GetLargeAvatarAsync(id);
        if (avatar == null) return;

        var img = avatar.Value;

        Texture2D tex = new Texture2D(
            (int)img.Width,
            (int)img.Height,
            TextureFormat.RGBA32,
            false
        );

        tex.LoadRawTextureData(img.Data);
        FlipTextureY(tex);

        image.texture = tex;
        image.color = isOnline ? UnityEngine.Color.white : new UnityEngine.Color(1f, 1f, 1f, 0.35f);
    }

    static void FlipTextureY(Texture2D tex)
    {
        var pixels = tex.GetPixels();
        int w = tex.width;
        int h = tex.height;

        for (int y = 0; y < h / 2; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int top = y * w + x;
                int bottom = (h - y - 1) * w + x;

                (pixels[top], pixels[bottom]) = (pixels[bottom], pixels[top]);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
    }

    // =========================
    // CLEANUP
    // =========================

    void ClearUI()
    {
        foreach (var go in _spawned)
            if (go) Destroy(go);

        _spawned.Clear();
    }
}
