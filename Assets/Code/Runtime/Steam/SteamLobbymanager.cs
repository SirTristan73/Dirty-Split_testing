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

    private void Awake()
    {
        // Подписываемся на события Steam
        SteamMatchmaking.OnLobbyCreated += OnLobbyCreatedCallback;
        SteamMatchmaking.OnLobbyEntered += OnLobbyEntered;
        SteamMatchmaking.OnLobbyMemberJoined += OnLobbyMemberJoined;
        SteamMatchmaking.OnLobbyMemberDisconnected += OnLobbyMemberLeft;
        SteamMatchmaking.OnLobbyMemberLeave += OnLobbyMemberLeft;
        SteamFriends.OnGameLobbyJoinRequested += OnGameLobbyJoinRequest;
    }

    private void OnDestroy()
    {
        // Отписываемся, чтобы не ловить ошибки при смене сцены
        SteamMatchmaking.OnLobbyCreated -= OnLobbyCreatedCallback;
        SteamMatchmaking.OnLobbyEntered -= OnLobbyEntered;
        SteamMatchmaking.OnLobbyMemberJoined -= OnLobbyMemberJoined;
        SteamMatchmaking.OnLobbyMemberDisconnected -= OnLobbyMemberLeft;
        SteamMatchmaking.OnLobbyMemberLeave -= OnLobbyMemberLeft;
        SteamFriends.OnGameLobbyJoinRequested -= OnGameLobbyJoinRequest;
    }

    private void OnEnable()
    {
        StartGameButton.interactable = false;
        CreateLobbyButton.interactable = true;
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
        // Создаем лобби на 4 человек (можно поменять число)
        var lobby = await SteamMatchmaking.CreateLobbyAsync(4);
        if (!lobby.HasValue)
        {
            Debug.LogError("Не удалось создать лобби. Либо нет интернета, либо Steam не запущен.");
            return;
        }

        CurrentLobby = lobby.Value;
        CurrentLobby.SetPublic();
        CurrentLobby.SetJoinable(true);
        Debug.Log($"Лобби создано: {CurrentLobby.Id}");
    }

    private void OnLobbyCreatedCallback(Result result, Lobby lobby)
    {
        if (result != Result.OK) return;
        
        CurrentLobby = lobby;
        StartGameButton.interactable = true;
    }

    private void OnLobbyEntered(Lobby lobby)
    {
        CurrentLobby = lobby;
        ClearLobbyUI();

        // Добавляем всех, кто уже в лобби (включая нас самих)
        foreach (var member in lobby.Members)
        {
            AddPlayerToUI(member);
        }

        Debug.Log($"Вошли в лобби: {lobby.Id}");
    }

    private void OnLobbyMemberJoined(Lobby lobby, Friend friend)
    {
        if (_playersInLobby.ContainsKey(friend.Id)) return;
        
        AddPlayerToUI(friend);
        Debug.Log($"{friend.Name} зашел в лобби.");
    }

    private void OnLobbyMemberLeft(Lobby lobby, Friend friend)
    {
        if (!_playersInLobby.ContainsKey(friend.Id)) return;
        
        Destroy(_playersInLobby[friend.Id]);
        _playersInLobby.Remove(friend.Id);
        Debug.Log($"{friend.Name} покинул лобби.");
    }

    private async void OnGameLobbyJoinRequest(Lobby lobby, SteamId friendId)
    {
        var result = await lobby.Join();
        if (result != RoomEnter.Success)
            Debug.LogError($"Не удалось зайти в лобби: {result}");
    }

    // --- ЛОГИКА UI ---

    private void AddPlayerToUI(Friend friend)
    {
        var obj = Instantiate(FriendPrefab, ContentRoot);
        _playersInLobby[friend.Id] = obj;

        // Сразу запускаем асинхронную подгрузку данных (ник и аватар)
        UpdatePlayerDisplay(friend.Id, obj);
    }

    private async void UpdatePlayerDisplay(SteamId id, GameObject uiElement)
    {
        // Иногда Steam возвращает "[unknown]" если данные еще не закэшированы
        // Мы подождем, пока имя станет вменяемым
        int attempts = 0;
        string name = new Friend(id).Name;
        
        while ((string.IsNullOrEmpty(name) || name == "[unknown]") && attempts < 10)
        {
            await Task.Delay(500);
            name = new Friend(id).Name;
            attempts++;
        }

        if (uiElement == null) return; // Игрок мог выйти, пока мы ждали

        // Ставим никнейм
        var text = uiElement.GetComponentInChildren<TMP_Text>();
        if (text != null) text.text = name;

        // Грузим аватар
        var rawImage = uiElement.GetComponentInChildren<RawImage>();
        if (rawImage != null)
        {
            await LoadAvatar(id, rawImage);
        }
    }

    private void ClearLobbyUI()
    {
        foreach (var obj in _playersInLobby.Values)
            Destroy(obj);
        _playersInLobby.Clear();
    }

    // --- КНОПКА СТАРТА ---

    public void OnStartGamePressed()
    {
        if (CurrentLobby.Id == 0) return;

        // Запускаем сервер Mirror (только для хоста)
        var manager = NetworkManager.singleton;
        if (manager != null && !NetworkServer.active)
        {
            manager.StartHost();
            Debug.Log("Хост запущен. Игра начинается.");
        }
    }

    // --- РАБОТА С КАРТИНКАМИ ---

    private async Task LoadAvatar(SteamId id, RawImage targetImage)
    {
        var avatarTask = await SteamFriends.GetLargeAvatarAsync(id);
        if (!avatarTask.HasValue) return;

        var img = avatarTask.Value;

        Texture2D tex = new Texture2D((int)img.Width, (int)img.Height, TextureFormat.RGBA32, false);
        tex.LoadRawTextureData(img.Data);
        
        // Steam присылает текстуры перевернутыми по вертикали, исправляем
        FlipTextureY(tex);
        
        if (targetImage != null)
        {
            targetImage.texture = tex;
        }
    }

    private void FlipTextureY(Texture2D tex)
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
                var temp = pixels[top];
                pixels[top] = pixels[bottom];
                pixels[bottom] = temp;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
    }
}