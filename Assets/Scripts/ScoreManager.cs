using TMPro;
using UnityEngine;
using Unity.Netcode;
using System.Collections;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class ScoreManager : NetworkBehaviour
{
    public TextMeshProUGUI leftScoreText;
    public TextMeshProUGUI rightScoreText;

    [SerializeField]
    private NetworkVariable<int> leftScore = new NetworkVariable<int>();
    [SerializeField]
    private NetworkVariable<int> rightScore = new NetworkVariable<int>();

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            leftScore.Value = 0;
            rightScore.Value = 0;
        }

        leftScore.OnValueChanged += (int oldValue, int newValue) =>
        {
            UpdateScoreUI();
        };
        rightScore.OnValueChanged += (int oldValue, int newValue) => UpdateScoreUI();

        UpdateScoreUI();
    }

    private void OnEnable()
    {
        GoalScript.Scored += OnScore;
    }

    private void OnDisable()
    {
        GoalScript.Scored -= OnScore;
    }

    private void OnScore(Side side)
    {
        if (IsServer)
        {
            Debug.Log("Incrementing on Server");
            switch (side)
            {
                case Side.Left:
                    leftScore.Value++;
                    break;
                case Side.Right:
                    rightScore.Value++;
                    break;
            }
        }
    }

    private void UpdateScoreUI()
    {
        leftScoreText.text = leftScore.Value.ToString();
        rightScoreText.text = rightScore.Value.ToString();
    }

    public void LobbyButtonClicked()
    {
        if (NetworkManagerSingleton.Instance.IsServer)
        {
            List<ulong> clientsToDisconnect = new List<ulong>();
            foreach (var client in NetworkManagerSingleton.Instance.ConnectedClients)
            {
                if (client.Key != NetworkManagerSingleton.Instance.LocalClientId)
                {
                    clientsToDisconnect.Add(client.Key);
                }
            }

            ChangeSceneClientRpc();

            StartCoroutine(DisconnectClientsAfterDelay(clientsToDisconnect));
        }
        else
        {
            NetworkManagerSingleton.Instance.Shutdown();
            SceneManager.LoadScene("Lobby");
        }
    }

    private IEnumerator DisconnectClientsAfterDelay(List<ulong> clientsToDisconnect)
    {
        yield return new WaitForSeconds(1f);

        foreach (var clientId in clientsToDisconnect)
        {
            NetworkManagerSingleton.Instance.DisconnectClient(clientId);
        }
        yield return new WaitForSeconds(1f);

        StartCoroutine(ShutdownAndChangeSceneCoroutine());
    }

    private IEnumerator ShutdownAndChangeSceneCoroutine()
    {
        NetworkManagerSingleton.Instance.Shutdown();

        yield return new WaitUntil(() => !NetworkManagerSingleton.Instance.ShutdownInProgress);
        SceneManager.LoadScene("Lobby");
    }

    [ClientRpc]
    private void ChangeSceneClientRpc()
    {
        SceneManager.LoadScene("Lobby");
    }
}