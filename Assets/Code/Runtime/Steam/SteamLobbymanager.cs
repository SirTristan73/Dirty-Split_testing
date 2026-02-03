using UnityEngine;
using Mirror;
using Steamworks;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

public class SteamLobbyManager : MonoBehaviour
{
    [Header("UI")]
    public Transform ContentRoot;
    public GameObject PlayerPrefab;
    public Button CreateLobbyButton;
    public Button StartGameButton;

    private CSteamID currentLobby;
    private Dictionary<CSteamID, GameObject> players = new();

    private const string HOST_STARTED_KEY = "HostStarted";
    private const string HOST_STEAMID_KEY = "HostSteamId";

    #region CALLBACKS

    private Callback<LobbyCreated_t> lobbyCreated;
    private Callback<LobbyEnter_t> lobbyEntered;
    private Callback<LobbyDataUpdate_t> lobbyDataUpdated;
    private Callback<GameLobbyJoinRequested_t> joinRequested;

    private void Awake()
    {
        lobbyCreated     = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        lobbyEntered     = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
        lobbyDataUpdated = Callback<LobbyDataUpdate_t>.Create(OnLobbyDataUpdated);
        joinRequested    = Callback<GameLobbyJoinRequested_t>.Create(OnJoinRequest);
    }

    private void OnEnable()
    {
        StartGameButton.interactable = false;
        CreateLobbyButton.onClick.AddListener(CreateLobby);
        StartGameButton.onClick.AddListener(StartGame);
    }

    private void OnDisable()
    {
        CreateLobbyButton.onClick.RemoveListener(CreateLobby);
        StartGameButton.onClick.RemoveListener(StartGame);
    }

    #endregion

    #region LOBBY LOGIC

    private void CreateLobby()
    {
        SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, 4);
    }

    private void OnLobbyCreated(LobbyCreated_t cb)
    {
        if (cb.m_eResult != EResult.k_EResultOK)
        {
            Debug.LogError("Lobby creation failed. Steam сегодня не в духе.");
            return;
        }

        currentLobby = new CSteamID(cb.m_ulSteamIDLobby);
        SteamMatchmaking.SetLobbyJoinable(currentLobby, true);

        StartGameButton.interactable = true;
        RebuildUI();
    }

    private void OnLobbyEntered(LobbyEnter_t cb)
    {
        currentLobby = new CSteamID(cb.m_ulSteamIDLobby);
        RebuildUI();
        TryJoinIfHostStarted();
    }

    private void OnLobbyDataUpdated(LobbyDataUpdate_t cb)
    {
        if ((ulong)currentLobby == cb.m_ulSteamIDLobby)
            TryJoinIfHostStarted();
    }

    private void OnJoinRequest(GameLobbyJoinRequested_t cb)
    {
        SteamMatchmaking.JoinLobby(cb.m_steamIDLobby);
    }

    #endregion

    #region NETWORK

    private void StartGame()
    {
        // Только владелец лобби может быть хостом. Демократии не будет.
        if (SteamMatchmaking.GetLobbyOwner(currentLobby) != SteamUser.GetSteamID())
            return;

        // КЛЮЧЕВО: объявляем SteamID хоста
        SteamMatchmaking.SetLobbyData(
            currentLobby,
            HOST_STEAMID_KEY,
            SteamUser.GetSteamID().ToString()
        );

        SteamMatchmaking.SetLobbyData(currentLobby, HOST_STARTED_KEY, "1");

        NetworkManager.singleton.networkAddress =
            SteamUser.GetSteamID().ToString();

        NetworkManager.singleton.StartHost();
    }

    private void TryJoinIfHostStarted()
    {
        if (NetworkClient.active || NetworkServer.active)
            return;

        var started = SteamMatchmaking.GetLobbyData(currentLobby, HOST_STARTED_KEY);
        if (started != "1")
            return;

        var hostId = SteamMatchmaking.GetLobbyData(currentLobby, HOST_STEAMID_KEY);
        if (string.IsNullOrEmpty(hostId))
        {
            Debug.LogError("HostSteamId отсутствует. Лобби без хоста — философская ошибка.");
            return;
        }

        NetworkManager.singleton.networkAddress = hostId;
        NetworkManager.singleton.StartClient();
    }

    #endregion

    #region UI

    private void RebuildUI()
    {
        foreach (var obj in players.Values)
            Destroy(obj);

        players.Clear();

        int count = SteamMatchmaking.GetNumLobbyMembers(currentLobby);
        for (int i = 0; i < count; i++)
        {
            var id = SteamMatchmaking.GetLobbyMemberByIndex(currentLobby, i);
            AddPlayer(id);
            
        }
    }

    async void AddPlayer(CSteamID id)
    {
        if (players.ContainsKey(id)) return;

        var ui = Instantiate(PlayerPrefab, ContentRoot);
        players[id] = ui;

        var text = ui.GetComponentInChildren<TMP_Text>();
        if (text != null)
            text.text = SteamFriends.GetFriendPersonaName(id);
        var image = ui.GetComponentInChildren<RawImage>();
        if (image != null)
        {
            SteamFriendsPanel.LoadAvatar(id, image, SteamFriends.GetFriendPersonaState(id) == EPersonaState.k_EPersonaStateOnline);
        }
    }

    #endregion
}
