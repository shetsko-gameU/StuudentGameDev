using NUnit.Framework;
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditorInternal.Profiling.Memory.Experimental;
using UnityEngine;
using UnityEngine.UI;

/*public class Inventory : MonoBehaviour
{
      
    
    public List<Inventory_Item> Inventory_Slots = new List<Inventory_Item>();
    public List<Image> UI_Slots = new List<Image>();


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Inventory_Slots.Count > 0)
        {
            for (int i = 0; i < Inventory_Slots.Count; i++)
            {
                UI_Slots[i].enabled = true;
                UI_Slots[i].sprite = Inventory_Slots[i].Image;

            }
            

        }


    }
}*/
public class Inventory : MonoBehaviour
{
    public List<Inventory_Item> Inventory_Slots = new List<Inventory_Item>();
    public List<Image> UI_Slots = new List<Image>();

    void Update()
    {
        // Basic UI update (you may want to optimize later)
        for (int i = 0; i < UI_Slots.Count; i++)
        {
            if (i < Inventory_Slots.Count && Inventory_Slots[i] != null)
            {
                UI_Slots[i].enabled = true;
                UI_Slots[i].sprite = Inventory_Slots[i].Image;
            }
            else
            {
                UI_Slots[i].enabled = false;
            }
        }
    }

    // Inventory is the one that "adds the Scriptable Object to itself"
    public bool TryAddModifierPickup(StatsModifierSO modifierTemplate, string itemName, int itemId, Sprite image, StatsManager statsManager)
    {
        if (modifierTemplate == null)
        {
            Debug.LogWarning("Modifier template was null.");
            return false;
        }

        // Roll + apply stats here (moved from ModifierPickup)
        if (statsManager != null)
        {
            var rolled = ModifierRoller.Roll(modifierTemplate);
            statsManager.AddRolledModifier(rolled);
        }
        else
        {
            Debug.LogWarning("StatsManager was null; item will still be added to inventory.");
        }

        // Create inventory item and store it
        Inventory_Item newItem = new Inventory_Item();
        newItem.Name = itemName;
        newItem.ItemID = itemId;
        newItem.Image = image;

        Inventory_Slots.Add(newItem);

        return true;
    }
}
