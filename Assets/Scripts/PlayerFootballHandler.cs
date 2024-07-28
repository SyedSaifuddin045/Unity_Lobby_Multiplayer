using Unity.Netcode;
using UnityEngine;

public class PlayerFootballHandler : NetworkBehaviour
{
    private GameObject football;
    public bool HasFootballAttached => football != null;
    public Transform footBallAttachTransform;
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Trigger Enter");
        if (IsServer && other.TryGetComponent<FootballScript>(out var footballScript))
        {
            football = other.transform.parent.gameObject;
            AttachFootballServerRpc(football.GetComponent<NetworkObject>().NetworkObjectId);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void AttachFootballServerRpc(ulong footballNetworkObjectId)
    {
        var footballNetworkObject = NetworkManager.Singleton.SpawnManager.SpawnedObjects[footballNetworkObjectId];
        if (footballNetworkObject != null)
        {
            // // Reparent the football on the server
            // footballNetworkObject.transform.SetParent(transform);
            // footballNetworkObject.transform.localPosition = Vector3.zero; // Adjust this based on where you want the football to be attached

            footballNetworkObject.transform.SetParent(transform);
            footballNetworkObject.transform.localPosition = footBallAttachTransform.localPosition;

            var footballScript = footballNetworkObject.GetComponentInChildren<FootballScript>();
            if (footballScript != null)
            {
                footballScript.Attach();
            }

            // Inform clients to update their local position
            UpdateFootballPositionClientRpc(footballNetworkObjectId, transform.position, transform.rotation);
        }
    }

    [ClientRpc]
    private void UpdateFootballPositionClientRpc(ulong footballNetworkObjectId, Vector3 parentPosition, Quaternion parentRotation)
    {
        var footballNetworkObject = NetworkManager.Singleton.SpawnManager.SpawnedObjects[footballNetworkObjectId];
        if (footballNetworkObject != null && !IsServer)
        {
            footballNetworkObject.transform.SetParent(transform);
            footballNetworkObject.transform.localPosition = footBallAttachTransform.localPosition; // Adjust this based on where you want the football to be attached

            var footballScript = footballNetworkObject.GetComponentInChildren<FootballScript>();
            if (footballScript != null)
            {
                footballScript.Attach();
            }
        }
    }

    public GameObject DetachFootball()
    {
        if (football == null) return null;

        var footballNetworkObject = football.GetComponent<NetworkObject>();
        if (footballNetworkObject != null)
        {
            footballNetworkObject.transform.SetParent(null);

            var footballScript = football.GetComponentInChildren<FootballScript>();
            if (footballScript != null)
            {
                footballScript.Detach();
            }
        }

        var detachedFootball = football;
        football = null;
        return detachedFootball;
    }
}
