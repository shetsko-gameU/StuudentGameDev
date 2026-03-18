using UnityEngine;
using System.Collections.Generic;
using System.Threading;

public class ModifierPickup : MonoBehaviour
{
    [SerializeField] private StatsModifierSO modifierTemplate;
    public string Name;
    public int ItemId;

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
            newItem.Count = 1;
            if (!inventory.Inventory_Slots.Contains(newItem))
            {
                inventory.Inventory_Slots.Add(newItem);

            }
            else
            {
                inventory.Inventory_Slots[inventory.Inventory_Slots.IndexOf(newItem)].Count += newItem.Count;
            }

            Destroy(gameObject);
        }
    }
}
