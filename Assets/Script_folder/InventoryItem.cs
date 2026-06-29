using UnityEngine;

[System.Serializable]
public class InventoryItem
{
    public StatsModifierSO ModifierSO;

    // The specific rolled values applied when this item was picked up.
    // Stored so we can cleanly remove them via stats.RemoveRolledInstance when unequipping.
    [System.NonSerialized]
    public RolledModifierInstance RolledInstance;

    public string Name;
    public string Description;
    public int ItemID;

    // Changed from Sprite to Texture2D for use with RawImage
    public Texture2D Image;
}
