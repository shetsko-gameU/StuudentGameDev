using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerConsume : MonoBehaviour
{
    // Links a food SO to the on-hit passive it grants when eaten
    [Serializable]
    public class FoodPassiveLink
    {
        public StatsModifierSO foodItem;
        public OnHitPassiveSO passive;
    }

    // Links a food SO to the stat boost passive it grants when eaten
    [Serializable]
    public class FoodStatBoostLink
    {
        public StatsModifierSO foodItem;
        public FoodStatPassiveSO statPassive;
    }

    // Links a food SO to the kill passive it grants when eaten
    [Serializable]
    public class FoodKillBoostLink
    {
        public StatsModifierSO foodItem;
        public KillPassiveSO killPassive;
    }

    [Header("References")]
    public Inventory inventory;
    public StatsManager stats;
    public PassiveManager passiveManager;

    [Header("Food → On-Hit Passive Links")]
    [Tooltip("Link food SOs to the on-hit passive they grant. " +
             "These fire a temporary buff every time the player takes damage.")]
    public List<FoodPassiveLink> foodPassives = new List<FoodPassiveLink>();

    [Header("Food → Stat Boost Links")]
    [Tooltip("Link food SOs to a stat boost passive. " +
             "These give a flat stat boost (e.g. +10 MaxHealth) that lasts until " +
             "the player eats a higher rarity version of the same food.")]
    public List<FoodStatBoostLink> foodStatBoosts = new List<FoodStatBoostLink>();

    [Header("Food → Kill Passive Links")]
    [Tooltip("Link food SOs to a kill passive. " +
             "These fire every time the player kills an enemy — e.g. 'gain a stack of " +
             "+Attack per kill' — until the player eats a higher rarity version of the same food.")]
    public List<FoodKillBoostLink> foodKillBoosts = new List<FoodKillBoostLink>();

    private void Awake()
    {
        if (inventory == null) inventory = GetComponent<Inventory>();
        if (stats == null) stats = GetComponent<StatsManager>();
        if (passiveManager == null) passiveManager = GetComponent<PassiveManager>();

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
    ///   1. Item is rolled and removed from inventory
    ///   2. If the food has an on-hit passive linked  → PassiveManager.AddFoodPassive
    ///   3. If the food has a stat boost linked        → PassiveManager.AddStatBoostPassive
    ///   4. If neither is linked                       → stats applied directly (no tracking)
    ///
    /// A food can have BOTH links — the on-hit passive gets the stat roll,
    /// and the stat boost gets a separate roll of the same template.
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

        // Roll and remove from inventory — stats not applied yet
        RolledModifierInstance rolledStats = inventory.ConsumeItem(inventoryIndex);

        OnHitPassiveSO onHitPassive = FindOnHitPassive(foodSO);
        FoodStatPassiveSO statPassive = FindStatBoostPassive(foodSO);
        KillPassiveSO killPassive = FindKillBoostPassive(foodSO);

        bool handled = false;

        // Handle on-hit passive
        if (onHitPassive != null && passiveManager != null)
        {
            passiveManager.AddFoodPassive(onHitPassive, rolledStats);
            handled = true;
        }

        // Handle stat boost passive — rolls stats independently if food has both types
        if (statPassive != null && passiveManager != null)
        {
            // If an on-hit passive already used the rolled stats, roll fresh ones for the stat boost
            RolledModifierInstance statRoll = handled
                ? ModifierRoller.Roll(foodSO)
                : rolledStats;

            passiveManager.AddStatBoostPassive(statPassive, statRoll);
            handled = true;
        }

        // Handle kill passive — rolls stats independently if food has other types too
        if (killPassive != null && passiveManager != null)
        {
            RolledModifierInstance killRoll = handled
                ? ModifierRoller.Roll(foodSO)
                : rolledStats;

            passiveManager.AddKillPassive(killPassive, killRoll);
            handled = true;
        }

        // No passive linked at all — apply stats directly
        if (!handled)
        {
            if (rolledStats != null && stats != null)
            {
                Debug.Log($"PlayerConsume: '{foodSO.displayName}' has no passive linked — applying stats directly.");
                stats.AddRolledModifier(rolledStats);
            }
            else
            {
                Debug.LogWarning($"PlayerConsume: Could not apply stats for '{foodSO.displayName}'. " +
                                 "Check that StatsManager and PassiveManager are assigned.");
            }
        }
    }

    /// <summary>
    /// Eats a food item by its SO directly. Useful for UI buttons.
    /// </summary>
    public void EatFood(StatsModifierSO foodItemSO)
    {
        if (foodItemSO == null) return;

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

    private OnHitPassiveSO FindOnHitPassive(StatsModifierSO foodItemSO)
    {
        foreach (FoodPassiveLink link in foodPassives)
        {
            if (link != null && link.foodItem == foodItemSO)
                return link.passive;
        }
        return null;
    }

    private FoodStatPassiveSO FindStatBoostPassive(StatsModifierSO foodItemSO)
    {
        foreach (FoodStatBoostLink link in foodStatBoosts)
        {
            if (link != null && link.foodItem == foodItemSO)
                return link.statPassive;
        }
        return null;
    }

    private KillPassiveSO FindKillBoostPassive(StatsModifierSO foodItemSO)
    {
        foreach (FoodKillBoostLink link in foodKillBoosts)
        {
            if (link != null && link.foodItem == foodItemSO)
                return link.killPassive;
        }
        return null;
    }
}