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
    /// Picks up an item and stores it in the inventory.
    /// Stats are NOT applied here — they are applied when the player consumes the item.
    /// </summary>
    public bool TryAddModifierPickup(StatsModifierSO modifierTemplate, string itemName, int itemId, Sprite image)
    {
        if (modifierTemplate == null)
        {
            Debug.LogWarning("Modifier template was null.");
            return false;
        }

        // Just store the item — no rolling, no stat changes happen yet
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
    /// Consumes an item by inventory index.
    /// This is where stats actually get rolled and applied to the player.
    /// The item is then removed from inventory.
    /// </summary>
    public RolledModifierInstance ConsumeItem(int index, StatsManager statsManager)
    {
        if (index < 0 || index >= InventorySlots.Count) return null;

        InventoryItem item = InventorySlots[index];
        if (item == null || item.ModifierSO == null) return null;

        // Roll the stats NOW — at the moment the player eats/uses the item
        RolledModifierInstance rolled = ModifierRoller.Roll(item.ModifierSO);

        if (statsManager != null)
        {
            statsManager.AddRolledModifier(rolled);
        }
        else
        {
            Debug.LogWarning("StatsManager was null — stats not applied on consume.");
        }

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
    /// Removes the first item matching this SO without consuming it (e.g. for crafting).
    /// Pass a StatsManager if you also need to un-apply the stats.
    /// </summary>
    public bool RemoveSO(StatsModifierSO so, StatsManager statsManager = null)
    {
        if (so == null) return false;

        for (int i = 0; i < InventorySlots.Count; i++)
        {
            if (InventorySlots[i] != null && InventorySlots[i].ModifierSO == so)
            {
                if (statsManager != null && InventorySlots[i].RolledInstance != null)
                    statsManager.RemoveRolledInstance(InventorySlots[i].RolledInstance);

                InventorySlots.RemoveAt(i);
                RefreshUI();
                return true;
            }
        }
        return false;
    }

    // ------------------------------------------------------------------ Adding by SO (crafting results etc.)

    /// <summary>
    /// Adds an item directly without rolling or applying stats.
    /// Used for crafted results that sit in inventory until consumed.
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
