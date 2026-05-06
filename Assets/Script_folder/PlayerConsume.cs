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
    [Tooltip("Link each food SO to the passive it grants. " +
             "Leave passive empty if the food only gives a stat boost with no on-hit effect.")]
    public List<FoodPassiveLink> foodPassives = new List<FoodPassiveLink>();

    private void Awake()
    {
        if (inventory == null) inventory = GetComponent<Inventory>();
        if (stats == null) stats = GetComponent<StatsManager>();
        if (passiveManager == null) passiveManager = GetComponent<PassiveManager>();

        // Log if any reference is missing so you can catch it early
        if (inventory == null) Debug.LogError($"PlayerConsume on '{name}': Inventory is missing.");
        if (stats == null) Debug.LogError($"PlayerConsume on '{name}': StatsManager is missing.");
        if (passiveManager == null) Debug.LogError($"PlayerConsume on '{name}': PassiveManager is missing.");
    }

    // ------------------------------------------------------------------ Eating

    /// <summary>
    /// Eats the food at a given inventory slot index.
    /// Hook to hotkeys: press 1 = EatFoodAtIndex(0), press 2 = EatFoodAtIndex(1), etc.
    ///
    /// What happens:
    ///   1. Item is rolled and removed from inventory (stats not applied yet)
    ///   2. The rolled stats are handed to PassiveManager along with the passive
    ///   3. PassiveManager applies the stats AND tracks them so they can be removed on upgrade
    /// </summary>
    public void EatFoodAtIndex(int inventoryIndex)
    {
        if (inventory == null)
        {
            Debug.LogError("PlayerConsume: Cannot eat — Inventory is null.");
            return;
        }

        if (inventoryIndex < 0 || inventoryIndex >= inventory.InventorySlots.Count)
        {
            Debug.LogWarning($"PlayerConsume: Index {inventoryIndex} is out of range " +
                             $"(inventory has {inventory.InventorySlots.Count} slots).");
            return;
        }

        InventoryItem item = inventory.InventorySlots[inventoryIndex];
        if (item == null || item.ModifierSO == null)
        {
            Debug.LogWarning($"PlayerConsume: Slot {inventoryIndex} is empty or has no ModifierSO.");
            return;
        }

        StatsModifierSO foodSO = item.ModifierSO;
        Debug.Log($"PlayerConsume: Eating '{foodSO.displayName}' from slot {inventoryIndex}.");

        // Roll the stat values and remove the item from inventory
        // Stats are NOT applied yet — PassiveManager does that so it can track and undo them
        RolledModifierInstance rolledStats = inventory.ConsumeItem(inventoryIndex);

        // Look up which OnHitPassiveSO this food grants (set in Inspector via foodPassives list)
        OnHitPassiveSO passive = FindPassive(foodSO);

        if (passive == null)
        {
            Debug.Log($"PlayerConsume: '{foodSO.displayName}' has no passive linked — applying stats directly.");
        }

        if (passive != null && passiveManager != null)
        {
            // Hand both the passive and the stat roll to PassiveManager.
            // It applies the stats, tracks them, and removes them if a higher rarity replaces this passive.
            passiveManager.AddFoodPassive(passive, rolledStats);
        }
        else if (rolledStats != null && stats != null)
        {
            // No passive linked — food is stat-only (e.g. a healing herb).
            // Apply the stats directly since there's nothing to track for replacement.
            stats.AddRolledModifier(rolledStats);
        }
        else
        {
            Debug.LogWarning($"PlayerConsume: Could not apply stats for '{foodSO.displayName}'. " +
                             "Check that StatsManager and PassiveManager are assigned.");
        }
    }

    /// <summary>
    /// Eats a food item by its SO directly. Useful for UI buttons.
    /// </summary>
    public void EatFood(StatsModifierSO foodItemSO)
    {
        if (foodItemSO == null)
        {
            return;
        }

        if (inventory == null)
        {
            Debug.LogError("PlayerConsume: Cannot eat — Inventory is null.");
            return;
        }

        int index = inventory.IndexOf(foodItemSO);
        if (index == -1)
        {
            Debug.LogWarning($"PlayerConsume: '{foodItemSO.displayName}' is not in inventory.");
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
                return link.passive; // can be null — that means stat-only food, which is fine
        }

        // No entry found for this food at all
        return null;
    }
}