using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class PlayerSpawner : NetworkBehaviour
{
    public GameObject playerPrefab;
    public GameObject footballPrefab;

    public static PlayerSpawner instance;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this);
        }
        else
        {
            instance = this;
            DontDestroyOnLoad(this);
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            if (NetworkManagerSingleton.Instance != null && NetworkManagerSingleton.Instance.SceneManager != null)
            {
                NetworkManagerSingleton.Instance.SceneManager.OnLoadEventCompleted += SceneManager_OnLoadEventCompleted;
                GoalScript.Scored += onGoalScore;
                Debug.Log("Subscribed to OnLoadEventCompleted");
            }
            else
            {
                Debug.LogError("NetworkManagerSingleton.Instance or SceneManager is null in OnNetworkSpawn");
            }
        }
    }

    private void onGoalScore(Side side)
    {
        float time = 2.0f;
        StartCoroutine(SpawnFootballAfter(time));
    }

    private IEnumerator SpawnFootballAfter(float time)
    {
        yield return new WaitForSeconds(time);
        SpawnFootball();
    }

    private void SceneManager_OnLoadEventCompleted(string sceneName, UnityEngine.SceneManagement.LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        Debug.Log($"Scene {sceneName} loaded. Spawning players...");

        if (sceneName.Equals("Game"))
        {
            if (playerPrefab == null)
            {
                Debug.LogError("Player prefab is not set in PlayerSpawner");
                return;
            }

            if (NetworkManagerSingleton.Instance == null)
            {
                Debug.LogError("NetworkManagerSingleton.Instance is null in SceneManager_OnLoadEventCompleted");
                return;
            }

            SpawnFootball();
            SpawnPlayers();
        }

    }

    private void SpawnFootball()
    {
        GameObject footballInstance = Instantiate(footballPrefab, new Vector3(0, 4, 0), Quaternion.identity);
        if (footballInstance == null)
        {
            Debug.LogError("Failed to instantiate football prefab");
            return;
        }

        NetworkObject networkObject = footballInstance.GetComponent<NetworkObject>();
        if (networkObject == null)
        {
            Debug.LogError("NetworkObject component missing on football prefab");
            Destroy(footballInstance);
            return;
        }

        networkObject.Spawn();
        Debug.Log("Spawned football");
    }

    private void SpawnPlayers()
    {
        int i = 1;
        foreach (ulong clientId in NetworkManagerSingleton.Instance.ConnectedClientsIds)
        {
            GameObject playerInstance = Instantiate(playerPrefab, new Vector3(i * 3f, 6, i * 3f), Quaternion.identity);
            i++;
            if (playerInstance == null)
            {
                Debug.LogError($"Failed to instantiate player prefab for client {clientId}");
                continue;
            }

            NetworkObject networkObject = playerInstance.GetComponent<NetworkObject>();
            if (networkObject == null)
            {
                Debug.LogError($"NetworkObject component missing on player prefab for client {clientId}");
                Destroy(playerInstance);
                continue;
            }

            networkObject.SpawnAsPlayerObject(clientId, true);
            Debug.Log($"Spawned player for client {clientId}");
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            if (NetworkManagerSingleton.Instance != null && NetworkManagerSingleton.Instance.SceneManager != null)
            {
                NetworkManagerSingleton.Instance.SceneManager.OnLoadEventCompleted -= SceneManager_OnLoadEventCompleted;
                GoalScript.Scored -= onGoalScore;
                Debug.Log("Unsubscribed from OnLoadEventCompleted");
            }
        }
    }
}