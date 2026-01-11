using UnityEngine;
using Mirror;
using Steamworks;
using Steamworks.Data;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Threading.Tasks;
using TMPro;

public class SteamLobbyManager : MonoBehaviour
{
    [Header("UI Elements")]
    public Transform ContentRoot;
    public GameObject FriendPrefab;
    public Button StartGameButton;
    public Button CreateLobbyButton;

    private Dictionary<SteamId, GameObject> _playersInLobby = new Dictionary<SteamId, GameObject>();
    public Lobby CurrentLobby { get; private set; }

    private const string HostAddressKey = "HostAddress";

    private void Awake()
    {
        SteamMatchmaking.OnLobbyCreated += OnLobbyCreatedCallback;
        SteamMatchmaking.OnLobbyEntered += OnLobbyEntered;
        SteamMatchmaking.OnLobbyMemberJoined += OnLobbyMemberJoined;
        SteamMatchmaking.OnLobbyMemberDisconnected += OnLobbyMemberLeft;
        SteamMatchmaking.OnLobbyMemberLeave += OnLobbyMemberLeft;
        SteamFriends.OnGameLobbyJoinRequested += OnGameLobbyJoinRequest;
        
        // Главная деталь: следим за обновлением данных лобби
        SteamMatchmaking.OnLobbyDataChanged += OnLobbyDataChanged;
    }

    private void OnDestroy()
    {
        SteamMatchmaking.OnLobbyCreated -= OnLobbyCreatedCallback;
        SteamMatchmaking.OnLobbyEntered -= OnLobbyEntered;
        SteamMatchmaking.OnLobbyMemberJoined -= OnLobbyMemberJoined;
        SteamMatchmaking.OnLobbyMemberDisconnected -= OnLobbyMemberLeft;
        SteamMatchmaking.OnLobbyMemberLeave -= OnLobbyMemberLeft;
        SteamFriends.OnGameLobbyJoinRequested -= OnGameLobbyJoinRequest;
        SteamMatchmaking.OnLobbyDataChanged -= OnLobbyDataChanged;
    }

    private void OnEnable()
    {
        StartGameButton.interactable = false;
        CreateLobbyButton.onClick.AddListener(CreateLobby);
        StartGameButton.onClick.AddListener(OnStartGamePressed);
    }

    private void OnDisable()
    {
        CreateLobbyButton.onClick.RemoveListener(CreateLobby);
        StartGameButton.onClick.RemoveListener(OnStartGamePressed);
    }

    public async void CreateLobby()
    {
        var lobby = await SteamMatchmaking.CreateLobbyAsync(4);
        if (!lobby.HasValue) return;

        CurrentLobby = lobby.Value;
        CurrentLobby.SetPublic();
        CurrentLobby.SetJoinable(true);
    }

    private void OnLobbyCreatedCallback(Result result, Lobby lobby)
    {
        if (result != Result.OK) return;
        CurrentLobby = lobby;
        // Кнопка старта активна только для хозяина
        StartGameButton.interactable = true;
    }

    private void OnLobbyEntered(Lobby lobby)
    {
        CurrentLobby = lobby;
        ClearLobbyUI();

        foreach (var member in lobby.Members)
            AddPlayerToUI(member);

        // Если мы зашли в лобби, где уже идет игра (HostAddress уже задан)
        CheckForHostAddress(lobby);
    }

    private void OnLobbyDataChanged(Lobby lobby)
    {
        CheckForHostAddress(lobby);
    }

    private void CheckForHostAddress(Lobby lobby)
    {
        // Если адрес хоста появился и мы еще не подключены — заходим
        string hostAddress = lobby.GetData(HostAddressKey);
        if (!string.IsNullOrEmpty(hostAddress) && !NetworkClient.active && !NetworkServer.active)
        {
            Debug.Log($"Подключаемся к хосту: {hostAddress}");
            NetworkManager.singleton.networkAddress = hostAddress;
            NetworkManager.singleton.StartClient();
        }
    }

    public void OnStartGamePressed()
    {
        // Проверка на вшивость: только владелец лобби может жать старт
        if (CurrentLobby.Owner.Id != SteamClient.SteamId) return;

        // Рассылаем всем наш SteamID как адрес сервера
        CurrentLobby.SetData(HostAddressKey, SteamClient.SteamId.ToString());

        var manager = NetworkManager.singleton;
        if (manager != null && !NetworkServer.active)
        {
            manager.StartHost();
        }
    }

    private async void OnGameLobbyJoinRequest(Lobby lobby, SteamId friendId)
    {
        await lobby.Join();
    }

    // --- UI ЛОГИКА ---

    private void AddPlayerToUI(Friend friend)
    {
        if (_playersInLobby.ContainsKey(friend.Id)) return;
        var obj = Instantiate(FriendPrefab, ContentRoot);
        _playersInLobby[friend.Id] = obj;
        UpdatePlayerDisplay(friend.Id, obj);
    }

    private void OnLobbyMemberJoined(Lobby lobby, Friend friend) => AddPlayerToUI(friend);

    private void OnLobbyMemberLeft(Lobby lobby, Friend friend)
    {
        if (_playersInLobby.ContainsKey(friend.Id))
        {
            Destroy(_playersInLobby[friend.Id]);
            _playersInLobby.Remove(friend.Id);
        }
    }

    private async void UpdatePlayerDisplay(SteamId id, GameObject uiElement)
    {
        int attempts = 0;
        string name = new Friend(id).Name;
        while ((string.IsNullOrEmpty(name) || name == "[unknown]") && attempts < 5)
        {
            await Task.Delay(500);
            name = new Friend(id).Name;
            attempts++;
        }

        if (uiElement == null) return;
        var text = uiElement.GetComponentInChildren<TMP_Text>();
        if (text != null) text.text = name;

        var rawImage = uiElement.GetComponentInChildren<RawImage>();
        if (rawImage != null) await LoadAvatar(id, rawImage);
    }

    private void ClearLobbyUI()
    {
        foreach (var obj in _playersInLobby.Values) Destroy(obj);
        _playersInLobby.Clear();
    }

    private async Task LoadAvatar(SteamId id, RawImage targetImage)
    {
        var img = await SteamFriends.GetLargeAvatarAsync(id);
        if (!img.HasValue) return;

        Texture2D tex = new Texture2D((int)img.Value.Width, (int)img.Value.Height, TextureFormat.RGBA32, false);
        tex.LoadRawTextureData(img.Value.Data);
        FlipTextureY(tex);
        if (targetImage != null) targetImage.texture = tex;
    }

    private void FlipTextureY(Texture2D tex)
    {
        var pixels = tex.GetPixels();
        int w = tex.width, h = tex.height;
        for (int y = 0; y < h / 2; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int top = y * w + x, bottom = (h - y - 1) * w + x;
                var temp = pixels[top];
                pixels[top] = pixels[bottom];
                pixels[bottom] = temp;
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
    }
}