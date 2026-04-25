using UnityEngine;
using UnityEngine.UI;

public class CraftSystem : MonoBehaviour
{
    [Header("UI")]
    public GameObject craftingMenu;
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
        if (other.CompareTag("CraftPot"))
            NearCraftPot = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("CraftPot"))
            NearCraftPot = false;
    }

    // ------------------------------------------------------------------ Menu

    public void OpenCraftMenu()
    {
        if (NearCraftPot && craftingMenu != null)
            craftingMenu.SetActive(true);
    }

    public void CloseCraftMenu()
    {
        if (craftingMenu != null)
            craftingMenu.SetActive(false);
    }

    // ------------------------------------------------------------------ Slot helpers

    public void ClearPrimary() { primarySlot = null; RefreshUI(); }
    public void ClearSecondary() { secondarySlot = null; RefreshUI(); }

    public void SetPrimaryFromInventorySlot(int index)
    {
        var item = GetInventoryItem(index);
        if (item == null) return;
        primarySlot = item.ModifierSO;
        RefreshUI();
    }

    public void SetSecondaryFromInventorySlot(int index)
    {
        var item = GetInventoryItem(index);
        if (item == null) return;
        secondarySlot = item.ModifierSO;
        RefreshUI();
    }

    private InventoryItem GetInventoryItem(int index)
    {
        if (playerInventory == null) return null;
        if (index < 0 || index >= playerInventory.InventorySlots.Count) return null;
        return playerInventory.InventorySlots[index];
    }

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

        playerInventory.RemoveSO(recipe.primary);
        if (recipe.secondary != null)
            playerInventory.RemoveSO(recipe.secondary);

        playerInventory.AddSO(recipe.result);

        primarySlot = null;
        secondarySlot = null;

        RefreshAfterCraft();
    }

    private CraftRecipeSO FindMatch()
    {
        if (recipes == null) return null;
        foreach (var r in recipes)
            if (r != null && r.Matches(primarySlot, secondarySlot))
                return r;
        return null;
    }

    // ------------------------------------------------------------------ UI refresh

    public void RefreshUI()
    {
        bool primaryFilled = primarySlot != null;
        bool secondaryFilled = secondarySlot != null;

        if (primaryUI?.slotImage != null) primaryUI.slotImage.enabled = primaryFilled;
        if (secondaryUI?.slotImage != null) secondaryUI.slotImage.enabled = secondaryFilled;

        if (resultSlotImage == null) return;

        // Only show result when both slots are filled and a recipe matches
        CraftRecipeSO match = FindMatch();
        bool showResult = primaryFilled && secondaryFilled && match != null && match.result != null;

        resultSlotImage.enabled = showResult;
        if (showResult)
            resultSlotImage.sprite = match.result.Image;
    }

    public void RefreshAfterCraft()
    {
        if (resultSlotImage != null)
            resultSlotImage.enabled = false;

        playerInventory.RefreshUI();

        // Reset draggable slot visuals
        for (int i = 0; i < playerInventory.UISlots.Count; i++)
        {
            var draggable = playerInventory.UISlots[i].GetComponent<DraggableInventorySlotUI>();
            if (draggable == null) continue;
            draggable.removed = false;
            draggable.craftInSlot = false;
            draggable.dragIconImage.color = Color.white;
        }

        if (primaryUI?.slotImage != null) primaryUI.slotImage.enabled = false;
        if (secondaryUI?.slotImage != null) secondaryUI.slotImage.enabled = false;
    }

    // Called by CraftDropSlotUI after a drag-drop
    public void RefreshUIAfterDrop() => RefreshUI();
}







