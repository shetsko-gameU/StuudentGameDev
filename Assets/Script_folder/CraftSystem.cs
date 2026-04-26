using UnityEngine;
using UnityEngine.UI;

public class CraftSystem : MonoBehaviour
{
    [Header("UI")]
    public GameObject CraftingMenu;
    public CraftDropSlotUI primaryUI;
    public CraftDropSlotUI secondaryUI;
    public Image resultSlotImage;

    [Header("Inventory")]
    public Inventory playerInventory;

    [Header("Crafting Slots")]
    public StatsModifierSO primarySlot;
    public StatsModifierSO secondarySlot;

    [Header("Recipes")]
    public CraftRecipeSO[] recipes;

    [Header("State")]
    public bool NearCraftPot;

    // ------------------------------------------------------------------ Trigger zone

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("CraftPot"))
            NearCraftPot = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("CraftPot"))
            NearCraftPot = false;
    }

    // ------------------------------------------------------------------ Menu

    public void OpenCraftMenu()
    {
        if (NearCraftPot && CraftingMenu != null)
            CraftingMenu.SetActive(true);
    }

    public void CloseCraftMenu()
    {
        if (CraftingMenu != null)
            CraftingMenu.SetActive(false);
    }

    // ------------------------------------------------------------------ Slot assignment

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

    // ------------------------------------------------------------------ Crafting

    public void Craft()
    {
        if (playerInventory == null) return;

        CraftRecipeSO recipe = FindMatch();
        if (recipe == null)
        {
            Debug.Log("No matching recipe.");
            return;
        }

        // Remove ingredients from inventory data
        playerInventory.RemoveSO(recipe.primary);

        if (recipe.secondary != null)
            playerInventory.RemoveSO(recipe.secondary);

        // Add crafted result to inventory data
        playerInventory.AddSO(recipe.result);

        primarySlot = null;
        secondarySlot = null;

        // Rebuild the UI so every slot shows the correct item at the correct index
        RefreshAfterCraft();
    }

    private CraftRecipeSO FindMatch()
    {
        if (recipes == null) return null;

        foreach (var r in recipes)
        {
            if (r != null && r.Matches(primarySlot, secondarySlot))
                return r;
        }
        return null;
    }

    // ------------------------------------------------------------------ UI refresh

    public void RefreshUI()
    {
        if (primaryUI.slotImage != null) primaryUI.slotImage.enabled = (primarySlot != null);
        if (secondaryUI.slotImage != null) secondaryUI.slotImage.enabled = (secondarySlot != null);

        if (resultSlotImage == null) return;

        CraftRecipeSO match = FindMatch();
        bool showResult = (primarySlot != null) && (secondarySlot != null)
                                   && (match != null) && (match.result != null);

        resultSlotImage.enabled = showResult;

        if (showResult)
            resultSlotImage.sprite = match.result.Image;
    }

   
    public void RefreshAfterCraft()
    {
        // Hide the craft slot icons and result preview
        if (primaryUI?.slotImage != null) primaryUI.slotImage.enabled = false;
        if (secondaryUI?.slotImage != null) secondaryUI.slotImage.enabled = false;
        if (resultSlotImage != null) resultSlotImage.enabled = false;

        // Rebuild every inventory UI slot from scratch.
        // RefreshDisplay(i) updates slotIndex, sprite on dragIconImage, color, and raycastTarget.
        for (int i = 0; i < playerInventory.UISlots.Count; i++)
        {
            var draggable = playerInventory.UISlots[i].GetComponent<DraggableInventorySlotUI>();

            if (draggable != null)
                draggable.RefreshDisplay(i);
        }
    }

    // Called by CraftDropSlotUI after a drag-drop
    public void RefreshUIAfterDrop() => RefreshUI();
}






