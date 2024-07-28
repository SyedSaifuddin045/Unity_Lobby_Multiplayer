using UnityEngine;
using Unity.Netcode;
using System;
using System.Collections;

public class NetworkManagerSingleton : NetworkManager
{
    private static NetworkManagerSingleton instance;

    public static NetworkManagerSingleton Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<NetworkManagerSingleton>();
                if (instance == null)
                {
                    GameObject singletonObject = new GameObject();
                    instance = singletonObject.AddComponent<NetworkManagerSingleton>();
                    singletonObject.name = typeof(NetworkManagerSingleton).ToString() + " (Singleton)";
                }
            }
            return instance;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            instance.Shutdown();
            StartCoroutine(DestroyAfterShutdown(this.gameObject));
        }
        else
        {
            instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
    }

    private IEnumerator DestroyAfterShutdown(GameObject gameObject)
    {
        yield return new WaitUntil(() => !instance.ShutdownInProgress);
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}
