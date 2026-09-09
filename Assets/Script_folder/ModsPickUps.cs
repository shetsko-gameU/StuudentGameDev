using UnityEngine;

/// <summary>
/// World pickup for an ingredient — on collision with the player, adds itself to Inventory
/// (stats are NOT rolled here; they're rolled when the item is eaten via PlayerConsume) and
/// destroys itself once accepted. Fires Inventory.OnIngredientPickedUp on success, which is
/// what IngredientPickupSFXTrigger listens for.
///
/// Setup:
///   1. Add to an ingredient prefab alongside a (non-trigger) Collider — this uses
///      OnCollisionEnter, not a trigger.
///   2. Assign modifierTemplate (the StatsModifierSO this ingredient grants when eaten),
///      Name, ItemId, and Image (icon shown in the inventory UI).
/// </summary>
public class ModifierPickup : MonoBehaviour
{
    [SerializeField] private StatsModifierSO modifierTemplate;
    public string Name;
    public int ItemId;

    // Changed from Sprite to Texture2D for use with RawImage
    public Texture Image;

    private void OnCollisionEnter(Collision other)
    {
        if (!other.gameObject.CompareTag("Player")) return;

        Inventory inventory = other.gameObject.GetComponent<Inventory>();
        if (inventory == null)
        {
            Debug.LogWarning("Player has no Inventory component.");
            return;
        }

        // Stats are NOT applied here � the item waits in inventory until consumed.
       bool added = inventory.TryAddModifierPickup(modifierTemplate, Name, ItemId, Image);
        if (added)
            Destroy(gameObject);
    }
}
