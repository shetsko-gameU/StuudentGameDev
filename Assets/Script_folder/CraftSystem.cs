using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class CraftSystem : MonoBehaviour
{
    public GameObject CraftingMenu;
    public RectTransform DragImage;
    public bool NearCraftPot;
    public bool ItemSelected;

    [Header("Inventory")]
    public Inventory playerInventory;

    [Header("Crafting Slots")]
    public StatsModifierSO primarySlot;
    public StatsModifierSO secondarySlot;

    [Header("Slot UI")]
    public Image primarySlotImage;
    public Image secondarySlotImage;
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
        
        if (ItemSelected)
        {
          DragImage.anchoredPosition = new Vector2(Mouse.current.position.ReadValue().x, Mouse.current.position.ReadValue().y);
        }

        
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Craft_Pot")
        {
            NearCraftPot = true;

        }


    }
    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.tag == "Craft_Pot")
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
        if (index < 0 || index >= playerInventory.Inventory_Slots.Count) return;

        var item = playerInventory.Inventory_Slots[index];
        if (item == null) return;

        primarySlot = item.ModifierSO;
        RefreshUI();
    }
    public void SetSecondaryFromInventorySlot(int index)
    {
        if (playerInventory == null) return;
        if (index < 0 || index >= playerInventory.Inventory_Slots.Count) return;

        var item = playerInventory.Inventory_Slots[index];
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

        // verify ingredients are actually in inventory
        if (!playerInventory.HasSO(recipe.primary)) { Debug.Log("Missing primary."); return; }
        if (recipe.secondary != null && !playerInventory.HasSO(recipe.secondary)) { Debug.Log("Missing secondary."); return; }

        // consume
        playerInventory.RemoveSO(recipe.primary);
        if (recipe.secondary != null) playerInventory.RemoveSO(recipe.secondary);

        // add result item to inventory
        playerInventory.AddSO(recipe.result);

        // clear crafting slots (optional)
        primarySlot = null;
        secondarySlot = null;
        RefreshUI();
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

    private void RefreshUI()
    {
        // Since StatsModifierSO has no icon, this UI section is optional.
        // If you add an icon to StatsModifierSO later, set sprites here.
        if (primarySlotImage != null) primarySlotImage.enabled = (primarySlot != null);
        if (secondarySlotImage != null) secondarySlotImage.enabled = (secondarySlot != null);

        var match = FindMatch();
        if (resultSlotImage != null) resultSlotImage.enabled = (match != null && match.result != null);
    }

}







