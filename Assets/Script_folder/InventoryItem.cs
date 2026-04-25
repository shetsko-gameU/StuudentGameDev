using UnityEngine;

[System.Serializable]
public class InventoryItem
{
    public StatsModifierSO ModifierSO;       // identity of the item

    // The specific rolled values that were applied to the player's stats when this item was picked up.
    // Keep this so we can cleanly call stats.RemoveRolledInstance(item.RolledInstance) when unequipping.
    [System.NonSerialized]
    public RolledModifierInstance RolledInstance;

    public string Name;
    public string Description;
    public int ItemID;
    public Sprite Image;
}
