using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerConsume : MonoBehaviour
{
    [Serializable]
    public class FoodPassiveLink
    {
        public StatsModifierSO foodItem;  // the inventory item (food)
        public OnHitPassiveSO passive;   // on-hit buff it grants when eaten
    }

    [Header("References")]
    public Inventory inventory;
    public StatsManager stats;
    public PassiveManager passiveManager;

    [Header("Food → Passive Links")]
    public List<FoodPassiveLink> foodPassives = new List<FoodPassiveLink>();

    private void Awake()
    {
        if (inventory == null) inventory = GetComponent<Inventory>();
        if (stats == null) stats = GetComponent<StatsManager>();
        if (passiveManager == null) passiveManager = GetComponent<PassiveManager>();
    }

    /// <summary>
    /// Call this when the player eats/uses a food item by inventory index (e.g. pressing a hotkey).
    /// This is where the item's stat roll happens and the values are added to the player.
    /// </summary>
    public void EatFoodAtIndex(int inventoryIndex)
    {
        if (inventory == null) return;
        if (inventoryIndex < 0 || inventoryIndex >= inventory.InventorySlots.Count) return;

        InventoryItem item = inventory.InventorySlots[inventoryIndex];
        if (item == null || item.ModifierSO == null) return;

        // 1. Roll and apply the stat modifier — this is the moment stats change
        inventory.ConsumeItem(inventoryIndex, stats);

        // 2. If this food also grants an on-hit passive, register that too
        OnHitPassiveSO passive = FindPassive(item.ModifierSO);
        if (passive != null && passiveManager != null)
            passiveManager.AddFoodPassive(passive);
    }

    /// <summary>
    /// Alternative: eat by passing the SO directly (useful for UI buttons that know the item).
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

    private OnHitPassiveSO FindPassive(StatsModifierSO foodItemSO)
    {
        for (int i = 0; i < foodPassives.Count; i++)
        {
            if (foodPassives[i] != null && foodPassives[i].foodItem == foodItemSO)
                return foodPassives[i].passive;
        }
        return null;
    }
    
    public void OnEatSlot1(InputAction.CallbackContext context)
    {
        // "performed" means the key was just pressed down
        if (context.performed)
        {
            EatFoodAtIndex(0);
        }
    }

    public void OnEatSlot2(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            EatFoodAtIndex(1);
        }
    }

    public void OnEatSlot3(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            EatFoodAtIndex(2);
        }
    }

    public void OnEatSlot4(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            EatFoodAtIndex(2);
        }
    }
    public void OnEatSlot5(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            EatFoodAtIndex(2);
        }
    }
    public void OnEatSlot6(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            EatFoodAtIndex(2);
        }
    }
    public void OnEatSlot7(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            EatFoodAtIndex(2);
        }
    }
    public void OnEatSlot8(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            EatFoodAtIndex(2);
        }
    }

}