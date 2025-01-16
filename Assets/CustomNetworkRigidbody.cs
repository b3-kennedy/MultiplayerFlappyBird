using Unity.Netcode;
using UnityEngine;

public class CustomNetworkRigidbody : NetworkBehaviour
{
    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            // Allow non-owners to use physics
            rb.bodyType = RigidbodyType2D.Dynamic;
        }
    }
}
