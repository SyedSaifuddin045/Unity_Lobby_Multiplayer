using System;
using Unity.Netcode;
using UnityEngine;

public enum Side
{
    None,
    Left,
    Right,
}
public class GoalScript : MonoBehaviour
{
    public Side scoreIncrementSide;
    public static Action<Side> Scored;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject)
        {
            var footballScript = other.transform.GetChild(0).GetComponent<FootballScript>();

            if (footballScript != null)
            {
                if (other.gameObject.GetComponent<NetworkObject>() != null)
                {
                    Scored?.Invoke(scoreIncrementSide);
                    other.gameObject.GetComponent<NetworkObject>().Despawn();
                }
            }
        }
    }
}