using UnityEngine;

/// <summary>
/// Add to the player alongside Inventory.
///
/// Listens to Inventory.OnIngredientPickedUp — fired only when a world ingredient
/// (ModifierPickup) is successfully added to the inventory, not for crafted results
/// added via Inventory.AddSO — and plays a non-positional pickup sound.
///
/// Setup:
///   1. Add this component to the Player, alongside Inventory.
///   2. Leave inventory empty — it auto-finds on Awake.
///   3. Drag a SoundSO into pickupSound.
/// </summary>
public class IngredientPickupSFXTrigger : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Auto-found if on the same GameObject.")]
    public Inventory inventory;

    [Header("Sound")]
    public SoundSO pickupSound;

    private void Awake()
    {
        if (inventory == null) inventory = GetComponent<Inventory>();
        if (inventory == null) Debug.LogError($"IngredientPickupSFXTrigger on '{name}': Inventory missing.");
    }

    private void OnEnable()
    {
        if (inventory != null)
            inventory.OnIngredientPickedUp += HandleIngredientPickedUp;
    }

    private void OnDisable()
    {
        if (inventory != null)
            inventory.OnIngredientPickedUp -= HandleIngredientPickedUp;
    }

    private void HandleIngredientPickedUp(InventoryItem item)
    {
        if (pickupSound != null)
            SoundManager.Instance.PlaySFX(pickupSound);
    }
}
