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
    public List<InventoryItem> InventorySlots = new List<InventoryItem>();
    public List<Image> UISlots = new List<Image>();
    public Color removedColor;

    void Update()
    {
        
        
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
        InventoryItem newItem = new InventoryItem();
        newItem.ModifierSO = modifierTemplate;

        newItem.Name = itemName;
        newItem.ItemID = itemId;
        newItem.Image = image;

        InventorySlots.Add(newItem);

        for (int i = 0; i < UISlots.Count; i++)
        {
            if (i < InventorySlots.Count && InventorySlots[i] != null)
            {
                UISlots[i].enabled = true;
                UISlots[i].sprite = InventorySlots[i].Image;
            }
            else
            {
                UISlots[i].enabled = false;
                UISlots[i].sprite = null;
            }
        }
         return true;

    }
    public bool HasSO(StatsModifierSO so)
    {
        if (so == null) return false;

        for (int i = 0; i < InventorySlots.Count; i++)
            if (InventorySlots[i] != null && InventorySlots[i].ModifierSO == so)
                return true;

        return false;
    }

    public bool RemoveSO(StatsModifierSO so)
    {
        if (so == null) return false;

        for (int i = 0; i < InventorySlots.Count; i++)
        {
            if (InventorySlots[i] != null && InventorySlots[i].ModifierSO == so)
            {
                InventorySlots.RemoveAt(i);
                return true;
            }
        }
        return false;
    }

    public void AddSO(StatsModifierSO so, Sprite iconOverride = null)
    {
        if (so == null) return;

        InventoryItem newItem = new InventoryItem();
        newItem.ModifierSO = so;
        newItem.Name = so.displayName;
        newItem.Image = so.Image; 

        InventorySlots.Add(newItem);
    }








}
