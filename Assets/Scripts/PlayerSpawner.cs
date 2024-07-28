using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

public class PlayerSpawner : NetworkBehaviour
{
    public GameObject playerPrefab;

    private void Awake()
    {
        DontDestroyOnLoad(this);
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
            {
                NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += SceneManager_OnLoadEventCompleted;
                Debug.Log("Subscribed to OnLoadEventCompleted");
            }
            else
            {
                Debug.LogError("NetworkManager.Singleton or SceneManager is null in OnNetworkSpawn");
            }
        }
    }

    private void SceneManager_OnLoadEventCompleted(string sceneName, UnityEngine.SceneManagement.LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        Debug.Log($"Scene {sceneName} loaded. Spawning players...");

        if (playerPrefab == null)
        {
            Debug.LogError("Player prefab is not set in PlayerSpawner");
            return;
        }

        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("NetworkManager.Singleton is null in SceneManager_OnLoadEventCompleted");
            return;
        }

        int i = 1;
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
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
            // NetworkManager.SpawnManager.InstantiateAndSpawn(playerPrefab,clientId,true,true);
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
            {
                NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= SceneManager_OnLoadEventCompleted;
                Debug.Log("Unsubscribed from OnLoadEventCompleted");
            }
        }
    }
}