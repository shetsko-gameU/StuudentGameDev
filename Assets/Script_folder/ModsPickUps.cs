using UnityEngine;

public class ModifierPickup : MonoBehaviour
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



            Destroy(gameObject);
        }
    }
}