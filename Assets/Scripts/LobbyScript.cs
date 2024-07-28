using System;
using System.Collections.Generic;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using Unity.Netcode;
using TMPro;



#if UNITY_EDITOR
using ParrelSync;
#endif

public class LobbyScript : MonoBehaviour
{
    private Lobby hostLobby;
    public float timerMax = 15f;
    private float timer;
    private UnityTransport _transport;
    public string JoinCodeKey = "joinCode";
    private bool isLobbyHost = false;
    public TextMeshProUGUI lobbyIdText;
    public TextMeshProUGUI playersText;
    public GameObject startScreen;
    public GameObject lobbyScreen;
    public GameObject startButton;
    private LobbyEventCallbacks callback;

    private void Awake()
    {
        _transport = FindAnyObjectByType<UnityTransport>();
    }
    private async void Start()
    {
        timer = timerMax;
        var options = new InitializationOptions();
#if UNITY_EDITOR
        options.SetProfile(ClonesManager.IsClone() ? ClonesManager.GetArgument() : "Primary");
#endif
        await UnityServices.InitializeAsync(options);
        AuthenticationService.Instance.SignedIn += UserSignedIn;
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    private void UserSignedIn()
    {
        Debug.Log("Signed in player with ID : " + AuthenticationService.Instance.PlayerId);
    }

    private void Update()
    {
        HandleLobbyHeartBeat();
    }

    private async void HandleLobbyHeartBeat()
    {
        try
        {
            if (hostLobby != null)
            {
                timer -= Time.deltaTime;
                if (timer <= 0)
                {
                    timer = timerMax;
                    await LobbyService.Instance.SendHeartbeatPingAsync(hostLobby.Id);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Exception Occured : " + ex);
        }
    }

    public async void CreateLobby()
    {
        try
        {
            int maxPlayers = 4;

            var a = await RelayService.Instance.CreateAllocationAsync(maxPlayers);
            var joinCode = await RelayService.Instance.GetJoinCodeAsync(a.AllocationId);

            var options = new CreateLobbyOptions
            {
                Data = new Dictionary<string, DataObject> { { JoinCodeKey, new DataObject(DataObject.VisibilityOptions.Public, joinCode) } }
            };

            string lobbyName = "My_Lobby";
            hostLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);

            Debug.Log("Lobby Created : " + lobbyName);
            startScreen.SetActive(false);

            _transport.SetHostRelayData(a.RelayServer.IpV4, (ushort)a.RelayServer.Port, a.AllocationIdBytes, a.Key, a.ConnectionData);
            isLobbyHost = true;
            NetworkManager.Singleton.StartHost();

            ShowLobbyDetails(hostLobby);
        }
        catch (LobbyServiceException ex)
        {
            Debug.LogError("Exception Occured : " + ex);
        }
    }

    private async void ShowLobbyDetails(Lobby lobby)
    {
        lobbyScreen.SetActive(true);
        lobbyIdText.text = lobby.Id;
        playersText.text = lobby.Players.Count.ToString() + "/" + lobby.MaxPlayers.ToString();

        callback = new LobbyEventCallbacks();
        await LobbyService.Instance.SubscribeToLobbyEventsAsync(lobby.Id, callback);
        callback.LobbyChanged += UpdateLobby;
    }

    private void UpdateLobby(ILobbyChanges changes)
    {
        if (!changes.LobbyDeleted)
            changes.ApplyToLobby(hostLobby);
        if (changes.PlayerJoined.Changed || changes.PlayerLeft.Changed)
        {
            int newPlayerCount = hostLobby.Players.Count;
            if (isLobbyHost)
                startButton.SetActive(newPlayerCount > 1);
            playersText.text = newPlayerCount.ToString() + "/" + hostLobby.MaxPlayers.ToString();
        }
    }

    public void StartPress()
    {
        if (isLobbyHost)
            NetworkManager.Singleton.SceneManager.LoadScene("Game", UnityEngine.SceneManagement.LoadSceneMode.Single);
    }
    private async void ListLobbies()
    {
        try
        {
            QueryResponse response = await Lobbies.Instance.QueryLobbiesAsync();
            Debug.Log("Found " + response.Results.Count + " lobbies");
        }
        catch (Exception ex)
        {
            Debug.LogError("Exception Occured" + ex);
        }
    }
    private async void JoinLobby(string id)
    {
        Lobby joinedLobby = await Lobbies.Instance.JoinLobbyByIdAsync(id);
        Debug.Log("Joined lobby " + joinedLobby.Id);
    }

    public async void QuickJoinLobby()
    {
        try
        {
            Lobby lobby = await LobbyService.Instance.QuickJoinLobbyAsync();
            ShowLobbyDetails(lobby);
            var a = await RelayService.Instance.JoinAllocationAsync(lobby.Data[JoinCodeKey].Value);

            SetTransformAsClient(a);
            NetworkManager.Singleton.StartClient();
            Debug.Log("Lobby Name : " + lobby.Name + ",Player Count : " + lobby.Players.Count);
        }
        catch (Exception e)
        {
            Debug.LogError("Exception Occured " + e);
            throw;
        }
    }
    private void SetTransformAsClient(JoinAllocation a)
    {
        _transport.SetClientRelayData(a.RelayServer.IpV4, (ushort)a.RelayServer.Port, a.AllocationIdBytes, a.Key, a.ConnectionData, a.HostConnectionData);
    }
}
