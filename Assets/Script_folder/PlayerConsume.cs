using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerConsume : MonoBehaviour
{
    [Serializable]
    public class FoodPassiveLink
    {
        public StatsModifierSO foodItem; // the food item SO
        public OnHitPassiveSO passive;  // the passive it grants when eaten (can be null for stat-only food)
    }

    [Header("References")]
    public Inventory inventory;
    public StatsManager stats;
    public PassiveManager passiveManager;

    [Header("Food to Passive Links")]
    [Tooltip("Link each food SO to the passive it grants. Leave passive empty if the food only gives a stat boost with no on-hit effect.")]
    public List<FoodPassiveLink> foodPassives = new List<FoodPassiveLink>();

    private void Awake()
    {
        if (inventory == null) inventory = GetComponent<Inventory>();
        if (stats == null) stats = GetComponent<StatsManager>();
        if (passiveManager == null) passiveManager = GetComponent<PassiveManager>();
    }

    // ------------------------------------------------------------------ Eating

    /// <summary>
    /// Eats the food at a given inventory slot index.
    /// Hook to hotkeys: press 1 = EatFoodAtIndex(0), press 2 = EatFoodAtIndex(1), etc.
    ///
    /// What happens here:
    ///   1. The item is rolled and removed from inventory (no stats applied yet)
    ///   2. The rolled instance is handed to PassiveManager
    ///   3. PassiveManager applies the stats AND tracks them so they can be removed on upgrade
    /// </summary>
    public void EatFoodAtIndex(int inventoryIndex)
    {
        if (inventory == null) return;
        if (inventoryIndex < 0 || inventoryIndex >= inventory.InventorySlots.Count) return;

        InventoryItem item = inventory.InventorySlots[inventoryIndex];
        if (item == null || item.ModifierSO == null) return;

        StatsModifierSO foodSO = item.ModifierSO;

        // Roll the stat values and remove the item from inventory.
        // Stats are NOT applied yet — PassiveManager does that so it can track and undo them.
        RolledModifierInstance rolledStats = inventory.ConsumeItem(inventoryIndex);

        // Find if this food has an on-hit passive linked to it
        OnHitPassiveSO passive = FindPassive(foodSO);

        if (passive != null && passiveManager != null)
        {
            // Hand both the passive and the stat roll to PassiveManager.
            // It applies the stats, tracks them, and will remove them if a higher rarity replaces this passive.
            passiveManager.AddFoodPassive(passive, rolledStats);
        }
        else if (rolledStats != null && stats != null)
        {
            // No passive linked — food is stat-only (e.g. a healing herb).
            // Apply the stats directly since there's nothing to track for replacement.
            stats.AddRolledModifier(rolledStats);
        }
    }

    /// <summary>
    /// Eats a food item by its SO. Useful for UI buttons.
    /// </summary>
    public void EatFood(StatsModifierSO foodItemSO)
    {
        if (foodItemSO == null) return;

        int index = inventory.IndexOf(foodItemSO);
        if (index == -1)
        {
            Debug.LogWarning("Tried to eat an item not in inventory: " + foodItemSO.displayName);
            return;
        }

        EatFoodAtIndex(index);
    }

    // ------------------------------------------------------------------ Helpers

    private OnHitPassiveSO FindPassive(StatsModifierSO foodItemSO)
    {
        foreach (FoodPassiveLink link in foodPassives)
        {
            if (link != null && link.foodItem == foodItemSO)
                return link.passive; // can be null — that's fine
        }
        return null;
    }
}