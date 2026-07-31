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

<<<<<<< HEAD
=======
    // Links a food SO to the kill passive it grants when eaten
    [Serializable]
    public class FoodKillBoostLink
    {
        public StatsModifierSO foodItem;
        public KillPassiveSO killPassive;
    }

    // Links a food SO to the debuff-on-hit passive it grants when eaten
    [Serializable]
    public class FoodDebuffBoostLink
    {
        public StatsModifierSO foodItem;
        public DebuffOnHitPassiveSO debuffPassive;
    }

    // Links a food SO to the ult ability it equips when eaten
    [Serializable]
    public class FoodUltBoostLink
    {
        public StatsModifierSO foodItem;
        public UltFoodSO ultFood;
    }

>>>>>>> ScriptBreanchfixs
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

<<<<<<< HEAD
=======
    [Header("Food → Kill Passive Links")]
    [Tooltip("Link food SOs to a kill passive. " +
             "These fire every time the player kills an enemy — e.g. 'gain a stack of " +
             "+Attack per kill' — until the player eats a higher rarity version of the same food.")]
    public List<FoodKillBoostLink> foodKillBoosts = new List<FoodKillBoostLink>();

    [Header("Food → Debuff Boost Links")]
    [Tooltip("Link food SOs to a debuff-on-hit passive. " +
             "These apply a temporary debuff to the enemy every time the player LANDS a hit " +
             "(not on a miss/dodge) — until the player eats a higher rarity version of the same food.")]
    public List<FoodDebuffBoostLink> foodDebuffBoosts = new List<FoodDebuffBoostLink>();

    [Header("Food → Ult Ability Links")]
    [Tooltip("Link food SOs to an ult ability. Eating one equips it into the player's " +
             "secondary ability slot — only one ult can ever be equipped at a time.")]
    public List<FoodUltBoostLink> foodUltBoosts = new List<FoodUltBoostLink>();

>>>>>>> ScriptBreanchfixs
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
<<<<<<< HEAD
    ///   2. If the food has an on-hit passive linked  → PassiveManager.AddFoodPassive
    ///   3. If the food has a stat boost linked        → PassiveManager.AddStatBoostPassive
    ///   4. If neither is linked                       → stats applied directly (no tracking)
    ///
    /// A food can have BOTH links — the on-hit passive gets the stat roll,
    /// and the stat boost gets a separate roll of the same template.
=======
    ///   2. If the food has an on-hit passive linked   → PassiveManager.AddFoodPassive
    ///   3. If the food has a stat boost linked         → PassiveManager.AddStatBoostPassive
    ///   4. If the food has a kill passive linked       → PassiveManager.AddKillPassive
    ///   5. If the food has a debuff passive linked     → PassiveManager.AddDebuffPassive
    ///   6. If the food has an ult ability linked        → PassiveManager.AddUltAbility
    ///   7. If none of the above are linked             → stats applied directly (no tracking)
    ///
    /// A food can have MULTIPLE links — the first one uses the original rolled stats,
    /// every additional linked type gets its own fresh roll of the same template.
>>>>>>> ScriptBreanchfixs
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
<<<<<<< HEAD
=======
        KillPassiveSO killPassive = FindKillBoostPassive(foodSO);
        DebuffOnHitPassiveSO debuffPassive = FindDebuffBoostPassive(foodSO);
        UltFoodSO ultFood = FindUltBoostPassive(foodSO);
>>>>>>> ScriptBreanchfixs

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

<<<<<<< HEAD
=======
        // Handle kill passive — rolls stats independently if food has other types too
        if (killPassive != null && passiveManager != null)
        {
            RolledModifierInstance killRoll = handled
                ? ModifierRoller.Roll(foodSO)
                : rolledStats;

            passiveManager.AddKillPassive(killPassive, killRoll);
            handled = true;
        }

        // Handle debuff-on-hit passive — rolls stats independently if food has other types too
        if (debuffPassive != null && passiveManager != null)
        {
            RolledModifierInstance debuffRoll = handled
                ? ModifierRoller.Roll(foodSO)
                : rolledStats;

            passiveManager.AddDebuffPassive(debuffPassive, debuffRoll);
            handled = true;
        }

        // Handle ult ability — no stat roll needed, it just equips into the ability slot
        if (ultFood != null && passiveManager != null)
        {
            passiveManager.AddUltAbility(ultFood);
            handled = true;
        }

>>>>>>> ScriptBreanchfixs
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
<<<<<<< HEAD
=======

    private KillPassiveSO FindKillBoostPassive(StatsModifierSO foodItemSO)
    {
        foreach (FoodKillBoostLink link in foodKillBoosts)
        {
            if (link != null && link.foodItem == foodItemSO)
                return link.killPassive;
        }
        return null;
    }

    private DebuffOnHitPassiveSO FindDebuffBoostPassive(StatsModifierSO foodItemSO)
    {
        foreach (FoodDebuffBoostLink link in foodDebuffBoosts)
        {
            if (link != null && link.foodItem == foodItemSO)
                return link.debuffPassive;
        }
        return null;
    }

    private UltFoodSO FindUltBoostPassive(StatsModifierSO foodItemSO)
    {
        foreach (FoodUltBoostLink link in foodUltBoosts)
        {
            if (link != null && link.foodItem == foodItemSO)
                return link.ultFood;
        }
        return null;
    }
>>>>>>> ScriptBreanchfixs
}