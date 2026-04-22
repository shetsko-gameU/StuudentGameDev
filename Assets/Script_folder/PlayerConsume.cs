using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerConsume : MonoBehaviour
{
    [Serializable]
    public class FoodPassiveLink
    {
        public StatsModifierSO foodItem;   // the inventory item (food)
        public OnHitPassiveSO passive;     // what it grants permanently
    }

    public Inventory inventory;
    public PassiveManager passiveManager;

    public List<FoodPassiveLink> foodPassives = new List<FoodPassiveLink>();

    private void Awake()
    {
        if (inventory == null)
        {
            inventory = GetComponent<Inventory>();
        }

        if (passiveManager == null)
        {
            passiveManager = GetComponent<PassiveManager>();
        }
    }

    public void EatFood(StatsModifierSO foodItemSO)
    {
        if (foodItemSO == null)
        {
            return;
        }

        OnHitPassiveSO passive = FindPassive(foodItemSO);
        if (passive == null)
        {
            Debug.LogWarning("No passive linked to this food item.");
            return;
        }

        passiveManager.AddFoodPassive(passive);

        // TODO: remove the food from inventory (however you do it)
    }

    private OnHitPassiveSO FindPassive(StatsModifierSO foodItemSO)
    {
        for (int i = 0; i < foodPassives.Count; i++)
        {
            if (foodPassives[i] != null && foodPassives[i].foodItem == foodItemSO)
            {
                return foodPassives[i].passive;
            }
        }
        return null;
    }
}