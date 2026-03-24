using NUnit.Framework;
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditorInternal.Profiling.Memory.Experimental;
using UnityEngine;
using UnityEngine.UI;

public class Inventory : MonoBehaviour
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
}
