using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class CraftSystem : MonoBehaviour
{
    public GameObject CraftingMenu;
    public CraftDropSlotUI primaryUI, secondaryUI;
    
    public bool NearCraftPot;
    public bool ItemSelected;

    [Header("Inventory")]
    public Inventory playerInventory;

    [Header("Crafting Slots")]
    public StatsModifierSO primarySlot;
    public StatsModifierSO secondarySlot;

    [Header("Slot UI")]
    
    public Image resultSlotImage;

    [Header("Recipes")]
    public CraftRecipeSO[] recipes;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
        

        
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "CraftPot")
        {
            NearCraftPot = true;

        }


    }
    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.tag == "CraftPot")
        {
            NearCraftPot = false;

        }


    }
    public void OpenCraftMenu()
    {
      if (NearCraftPot)
        {
            CraftingMenu.SetActive(true);

        }


    }
    public void CloseCraftMenu()
    {
        if (NearCraftPot)
        {
            CraftingMenu.SetActive(false);
        }
    }
    public void SelectItem(int slot)
    {
        ItemSelected = true;
    }
    public void SetPrimaryFromInventorySlot(int index)
    {
        if (playerInventory == null) return;
        if (index < 0 || index >= playerInventory.InventorySlots.Count) return;

        var item = playerInventory.InventorySlots[index];
        if (item == null) return;

        primarySlot = item.ModifierSO;
        RefreshUI();
    }
    public void SetSecondaryFromInventorySlot(int index)
    {
        if (playerInventory == null) return;
        if (index < 0 || index >= playerInventory.InventorySlots.Count) return;

        var item = playerInventory.InventorySlots[index];
        if (item == null) return;

        secondarySlot = item.ModifierSO;
        RefreshUI();
    }
    public void ClearPrimary() { primarySlot = null; RefreshUI(); }
    public void ClearSecondary() { secondarySlot = null; RefreshUI(); }

    public void Craft()
    {
        if (playerInventory == null) return;

        CraftRecipeSO recipe = FindMatch();
        if (recipe == null)
        {
            Debug.Log("No matching recipe.");
            return;
        }

        

        // consume
        playerInventory.RemoveSO(recipe.primary);
        if (recipe.secondary != null) playerInventory.RemoveSO(recipe.secondary);

        // add result item to inventory
        playerInventory.AddSO(recipe.result);

        // clear crafting slots (optional)
        primarySlot = null;
        secondarySlot = null;
        RefreshInventory();
    }

    private CraftRecipeSO FindMatch()
    {
        if (recipes == null) return null;

        for (int i = 0; i < recipes.Length; i++)
        {
            var r = recipes[i];
            if (r != null && r.Matches(primarySlot, secondarySlot))
                return r;
        }
        return null;
    }

    public void RefreshUI()
    {
        if (primaryUI.slotImage != null) primaryUI.slotImage.enabled = (primarySlot != null);
        if (secondaryUI.slotImage != null) secondaryUI.slotImage.enabled = (secondarySlot != null);

        Debug.Log("Is fire");
        var match = FindMatch();
        if (resultSlotImage != null&&primaryUI.slotImage.enabled == true&&secondaryUI.slotImage.enabled == true)
        {

            resultSlotImage.enabled = (match != null && match.result != null);
           
            resultSlotImage.sprite = match.result.Image;
        }
    }
    public void RefreshInventory()
    {
        if (primaryUI.slotImage != null) primaryUI.slotImage.enabled = (primarySlot != null);
        if (secondaryUI.slotImage != null) secondaryUI.slotImage.enabled = (secondarySlot != null);
        resultSlotImage.enabled = false;
        for (int i = 0; i < playerInventory.UISlots.Count; i++)
        {
            if (i < playerInventory.InventorySlots.Count && playerInventory.InventorySlots[i] != null)
            {
                playerInventory.UISlots[i].enabled = true;
                playerInventory.UISlots[i].sprite = playerInventory.InventorySlots[i].Image;
            }
            else
            {
                playerInventory.UISlots[i].enabled = false;
                playerInventory.UISlots[i].sprite = null;
            }
            playerInventory.UISlots[i].GetComponent<DraggableInventorySlotUI>().removed = false;
            playerInventory.UISlots[i].GetComponent<DraggableInventorySlotUI>().dragIconImage.color = Color.white;
            playerInventory.UISlots[i].GetComponent<DraggableInventorySlotUI>().craftInSlot = false;
        }
        

    }
    public void RefreshUIAfterDrop()
    {
        // Recompute result / enable craft button 
        
        RefreshUI();
    }



}







