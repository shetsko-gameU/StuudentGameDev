using UnityEngine;
using System.Collections.Generic;
using System.Threading;

/*public class ModifierPickup : MonoBehaviour
{
    [SerializeField] private StatsModifierSO modifierTemplate;
    public string Name;
    public int ItemId;
    public Sprite Image;

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("Player"))
        {


            if (other.gameObject.GetComponent<StatsManager>() == null)
            {
                Debug.Log("StatManger == null");
            }
            //return;/

            var rolled = ModifierRoller.Roll(modifierTemplate);
            other.gameObject.GetComponent<StatsManager>().AddRolledModifier(rolled);
            var stat = other.gameObject.GetComponent<StatsManager>();
            Debug.Log("Stat = " + stat.gameObject);

            Inventory inventory = other.gameObject.GetComponent<Inventory>();
            Inventory_Item newItem = new Inventory_Item();
            newItem.Name = Name;
            newItem.ItemID = ItemId;
            newItem.Image = Image;
            
            inventory.Inventory_Slots.Add(newItem);
            /* bool HasItem = false;
            if (inventory.Inventory_Slots.Count > 0)
            {
                for (int i = 0; i < inventory.Inventory_Slots.Count; i++)
                {
                    if (inventory.Inventory_Slots[i].ItemID == newItem.ItemID)
                    {
                        inventory.Inventory_Slots[i].Count++; 
                        HasItem = true;
                    }
                    
                }
                if (HasItem == false)
                {

                    inventory.Inventory_Slots.Add(newItem);
                }

            }
            else
            {
                inventory.Inventory_Slots.Add(newItem);

            }


            Destroy(gameObject);
        }
    }
}*/
public class ModifierPickup : MonoBehaviour
{
    [SerializeField] private StatsModifierSO modifierTemplate;
    public string Name;
    public int ItemId;
    public Sprite Image;



    private void OnCollisionEnter(Collision other)
    {
        if (!other.gameObject.CompareTag("Player"))
            return;

        Inventory inventory = other.gameObject.GetComponent<Inventory>();
        StatsManager stats = other.gameObject.GetComponent<StatsManager>();
        if (inventory == null)
        {
            Debug.LogWarning("Player has no Inventory component.");
            return;
        }

        

        bool added = inventory.TryAddModifierPickup(modifierTemplate, Name, ItemId, Image, stats);
        if (added)
        {
            Destroy(gameObject);
        }
    }
}
