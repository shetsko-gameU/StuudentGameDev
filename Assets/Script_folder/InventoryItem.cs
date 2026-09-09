using UnityEngine;

/// <summary>
/// One inventory slot's contents — a reference to the food/ingredient's StatsModifierSO
/// template plus display info. Stats are NOT rolled at pickup time; RolledInstance is only
/// populated once the item is actually equipped/consumed and needs to be removed later by
/// exact instance. Plain data, never created as an asset — populated by Inventory itself.
/// </summary>
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
    public Texture Image;
}
