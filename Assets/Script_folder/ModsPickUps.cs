using UnityEngine;

public class ModifierPickup : MonoBehaviour
{
    [SerializeField] private StatsModifierSO modifierTemplate;
    public string Name;
    public int ItemId;

    // Changed from Sprite to Texture2D for use with RawImage
    public Texture2D Image;
    public RenderTexture RenderTexture;

    private void OnCollisionEnter(Collision other)
    {
        if (!other.gameObject.CompareTag("Player")) return;

        Inventory inventory = other.gameObject.GetComponent<Inventory>();
        if (inventory == null)
        {
            Debug.LogWarning("Player has no Inventory component.");
            return;
        }

        // Stats are NOT applied here — the item waits in inventory until consumed.
        bool added = inventory.TryAddModifierPickup(modifierTemplate, Name, ItemId, Image, RenderTexture);
        if (added)
            Destroy(gameObject);
    }
}
