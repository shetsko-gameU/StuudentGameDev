using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Inventory : MonoBehaviour
{
    public List<InventoryItem> InventorySlots = new List<InventoryItem>();
    public List<Image> UISlots = new List<Image>();
    public Color removedColor;

    // ------------------------------------------------------------------ Picking up items

    /// <summary>
    /// Stores an item in inventory when the player walks over it.
    /// Stats are NOT applied here — they happen when the player eats the item.
    /// </summary>
    public bool TryAddModifierPickup(StatsModifierSO modifierTemplate, string itemName, int itemId, Sprite image)
    {
        if (modifierTemplate == null)
        {
            Debug.LogWarning("Modifier template was null.");
            return false;
        }

        InventoryItem newItem = new InventoryItem
        {
            ModifierSO = modifierTemplate,
            Name = itemName,
            ItemID = itemId,
            Image = image
        };

        InventorySlots.Add(newItem);
        RefreshUI();
        return true;
    }

    // ------------------------------------------------------------------ Consuming items

    /// <summary>
    /// Rolls the item's stats and removes it from inventory.
    /// Does NOT apply the stats — returns the rolled instance so the caller can decide what to do with it.
    ///
    /// For food: PlayerConsume takes this rolled instance and hands it to PassiveManager,
    /// which applies it and tracks it so it can be removed if the passive is upgraded later.
    /// </summary>
    public RolledModifierInstance ConsumeItem(int index)
    {
        if (index < 0 || index >= InventorySlots.Count) return null;

        InventoryItem item = InventorySlots[index];
        if (item == null || item.ModifierSO == null) return null;

        // Roll the stat values at the moment of eating
        RolledModifierInstance rolled = ModifierRoller.Roll(item.ModifierSO);

        InventorySlots.RemoveAt(index);
        RefreshUI();

        return rolled;
    }

    // ------------------------------------------------------------------ Queries

    public bool HasSO(StatsModifierSO so)
    {
        if (so == null) return false;

        for (int i = 0; i < InventorySlots.Count; i++)
        {
            if (InventorySlots[i] != null && InventorySlots[i].ModifierSO == so)
                return true;
        }
        return false;
    }

    public int IndexOf(StatsModifierSO so)
    {
        if (so == null) return -1;

        for (int i = 0; i < InventorySlots.Count; i++)
        {
            if (InventorySlots[i] != null && InventorySlots[i].ModifierSO == so)
                return i;
        }
        return -1;
    }

    // ------------------------------------------------------------------ Removing items

    /// <summary>
    /// Removes an item without consuming it (used by the crafting system).
    /// </summary>
    public bool RemoveSO(StatsModifierSO so)
    {
        if (so == null) return false;

        for (int i = 0; i < InventorySlots.Count; i++)
        {
            if (InventorySlots[i] != null && InventorySlots[i].ModifierSO == so)
            {
                InventorySlots.RemoveAt(i);
                RefreshUI();
                return true;
            }
        }
        return false;
    }

    // ------------------------------------------------------------------ Adding by SO (crafting results etc.)

    /// <summary>
    /// Adds an item directly without rolling stats — used for crafted results.
    /// </summary>
    public void AddSO(StatsModifierSO so, Sprite iconOverride = null)
    {
        if (so == null) return;

        InventoryItem newItem = new InventoryItem
        {
            ModifierSO = so,
            Name = so.displayName,
            Image = iconOverride != null ? iconOverride : so.Image
        };

        InventorySlots.Add(newItem);
        RefreshUI();
    }

    // ------------------------------------------------------------------ UI

    public void RefreshUI()
    {
        for (int i = 0; i < UISlots.Count; i++)
        {
            if (i < InventorySlots.Count && InventorySlots[i] != null)
            {
                UISlots[i].sprite = InventorySlots[i].Image;
                UISlots[i].enabled = true;
            }
            else
            {
                UISlots[i].sprite = null;
                UISlots[i].enabled = false;
            }
        }
    }
}
