using UnityEngine;

public class FootballScript : MonoBehaviour
{
    public Rigidbody rb;
    public bool IsAttached;

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
            rb.velocity = Vector3.zero; // Stop any existing motion
            rb.angularVelocity = Vector3.zero;

            // Destroy(transform.parent.GetComponent<NetworkRigidbody>());
            // Destroy(rb);
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
