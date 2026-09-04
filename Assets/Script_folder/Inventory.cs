using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Inventory : MonoBehaviour
{
    public List<InventoryItem> InventorySlots = new List<InventoryItem>();

    // Changed from List<Image> to List<RawImage>
    public List<RawImage> UISlots = new List<RawImage>();

    public Color removedColor;

    public GameObject inventory;

    /// <summary>Fires after a world ingredient pickup is successfully added to the inventory.
    /// Used by IngredientPickupSFXTrigger — does NOT fire for AddSO (crafted results).</summary>
    public event System.Action<InventoryItem> OnIngredientPickedUp;

    // ------------------------------------------------------------------ Picking up items

    /// <summary>
    /// Stores an item in inventory when the player walks over it.
    /// Stats are NOT applied here � they happen when the player eats the item.
    /// </summary>
    public bool TryAddModifierPickup(StatsModifierSO modifierTemplate, string itemName, int itemId, Texture image)
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
        OnIngredientPickedUp?.Invoke(newItem);
        return true;
    }

    public void Awake()
    {
        foreach (RawImage UIslot in inventory.GetComponentsInChildren<RawImage>())
        {
            UISlots.Add(UIslot);
        }
    }
    // ------------------------------------------------------------------ Consuming items

    /// <summary>
    /// Rolls the item's stats and removes it from inventory.
    /// Does NOT apply the stats � returns the rolled instance so the caller decides what to do with it.
    /// </summary>
    public RolledModifierInstance ConsumeItem(int index)
    {
        if (index < 0 || index >= InventorySlots.Count) return null;

        InventoryItem item = InventorySlots[index];
        if (item == null || item.ModifierSO == null) return null;

        RolledModifierInstance rolled = ModifierRoller.Roll(item.ModifierSO);

        // Auto-compacts — every later item shifts down one slot to close the gap.
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
                // Auto-compacts — see ConsumeItem.
                InventorySlots.RemoveAt(i);
                RefreshUI();
                return true;
            }
        }
        return false;
    }

    // ------------------------------------------------------------------ Moving items

    /// <summary>
    /// Swaps the items at two inventory slot indices — used when the player drags one
    /// inventory item onto another slot. If the destination is empty this just moves the
    /// item there and leaves the source empty (a swap against a hole degrades to a move).
    /// </summary>
    public void SwapSlots(int indexA, int indexB)
    {
        if (indexA < 0 || indexA >= InventorySlots.Count) return;
        if (indexB < 0 || indexB >= InventorySlots.Count) return;
        if (indexA == indexB) return;

        (InventorySlots[indexA], InventorySlots[indexB]) = (InventorySlots[indexB], InventorySlots[indexA]);
        RefreshUI();
    }

    // ------------------------------------------------------------------ Adding by SO (crafting results etc.)

    /// <summary>
    /// Adds an item directly without rolling stats � used for crafted results.
    /// </summary>
    public void AddSO(StatsModifierSO so, Texture iconOverride = null)
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
                // Changed from .sprite to .texture
                UISlots[i].texture = InventorySlots[i].Image;
                UISlots[i].color = Color.white;
                UISlots[i].enabled = true;
            }
            else
            {
                UISlots[i].texture = null;

                // Stays enabled (just made invisible via alpha) instead of disabled — a
                // disabled Graphic is dropped from Unity's raycast registry entirely, which
                // would make empty slots unable to receive OnDrop and silently block dragging
                // an item into an empty slot.
                UISlots[i].color = Color.clear;
                UISlots[i].enabled = true;

                // The item that used to live here is gone (eaten directly, or removed by
                // crafting) — clear any drag/craft lock left on this slot so a future item
                // that lands in this hole isn't stuck greyed-out and undraggable. Only runs
                // when the slot is actually empty, so a slot legitimately staged in a craft
                // slot (item still present, still mid-consideration) is left untouched.
                DraggableInventorySlotUI slot = UISlots[i].GetComponent<DraggableInventorySlotUI>();
                if (slot != null)
                {
                    slot.removed = false;
                    slot.craftInSlot = false;
                    if (slot.dragIconImage != null)
                    {
                        slot.dragIconImage.color = Color.clear;
                        slot.dragIconImage.raycastTarget = true;
                    }
                }
            }
        }
    }
}
