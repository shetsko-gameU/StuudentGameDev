using UnityEngine;

public class ModifierPickup : MonoBehaviour
{
    [SerializeField] private StatsModifierSO modifierTemplate;
    public string Name;
    public int ItemId;
    public Sprite Image;

    private void OnCollisionEnter(Collision other)
    {
        if (!other.gameObject.CompareTag("Player")) return;

        Inventory inventory = other.gameObject.GetComponent<Inventory>();
        if (inventory == null)
        {
            Debug.LogWarning("Player has no Inventory component.");
            return;
        }

        // Stats are NOT applied here anymore.
        // The item just goes into the inventory and waits to be consumed.
        bool added = inventory.TryAddModifierPickup(modifierTemplate, Name, ItemId, Image);
        if (added)
            Destroy(gameObject);
    }
}
