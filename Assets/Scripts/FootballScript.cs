using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class FootballScript : NetworkBehaviour
{
    public Rigidbody rb;
    public bool IsAttached;

    public override void OnNetworkDespawn()
    {
        Destroy(this.gameObject);
    }

    private void Awake()
    {
        rb = transform.parent.GetComponent<Rigidbody>();
    }

    public void Attach()
    {
        IsAttached = true;
        if (rb != null)
        {
            rb.isKinematic = true; // Disable physics
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

        }
    }

    public void Detach()
    {
        IsAttached = false;
        if (rb != null)
        {
            rb.isKinematic = false; // Enable physics
        }
    }
}
