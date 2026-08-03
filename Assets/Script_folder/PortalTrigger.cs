using UnityEngine;

/// <summary>
/// Place on the portal/door GameObject itself, alongside a trigger Collider.
/// When the player walks into it, calls RoomExit.EnterPortal() to load the next scene.
/// Same self-contained trigger-detection pattern as CurrencyPickup.
/// </summary>
[RequireComponent(typeof(Collider))]
public class PortalTrigger : MonoBehaviour
{
    [Tooltip("The RoomExit that owns this portal. Auto-found on the parent if left empty.")]
    public RoomExit roomExit;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;

        if (roomExit == null)
            roomExit = GetComponentInParent<RoomExit>();

        if (roomExit == null)
            Debug.LogError($"PortalTrigger on '{name}': No RoomExit found (checked self and parents).");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (roomExit == null) return;

        roomExit.EnterPortal();
    }
}
