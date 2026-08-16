using UnityEngine;
using UnityEngine.UI;


public class CraftSystem : MonoBehaviour
{
    [Header("UI")]
    public GameObject CraftingMenu;
    public CraftDropSlotUI primaryUI;
    public CraftDropSlotUI secondaryUI;

    // Changed from Image to RawImage
    public RawImage resultSlotImage;

    [Header("Inventory")]
    public Inventory playerInventory;

    [Header("Crafting Slots")]
    public StatsModifierSO primarySlot;
    public StatsModifierSO secondarySlot;

    [Header("Recipes")]
    public CraftRecipeSO[] recipes;
    public RarityRecipeSO[] rarityRecipes;

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

        // Try exact recipe first
        CraftRecipeSO recipe = FindMatch();
        if (recipe != null)
        {
            if (!IngredientsStillAvailable()) return;

            playerInventory.RemoveSO(recipe.primary);
            if (recipe.secondary != null)
                playerInventory.RemoveSO(recipe.secondary);
            playerInventory.AddSO(recipe.result);
            primarySlot = null;
            secondarySlot = null;
            RefreshAfterCraft();
            return;
        }

        // Try rarity recipe — roll output rarity, then pick matching SO
        RarityRecipeSO rarityRecipe = FindRarityMatch();
        if (rarityRecipe != null)
        {
            if (!IngredientsStillAvailable()) return;

            Rarity primaryRarity  = primarySlot != null  ? primarySlot.rarity  : Rarity.Common;
            Rarity secondaryRarity = secondarySlot != null ? secondarySlot.rarity : primaryRarity;

            Rarity outputRarity = RarityRecipeSO.RollOutputRarity(primaryRarity, secondaryRarity);
            StatsModifierSO result = rarityRecipe.GetResult(outputRarity);

            if (result == null)
            {
                Debug.LogWarning($"CraftSystem: RarityRecipe '{rarityRecipe.name}' produced no result for {outputRarity}.");
                return;
            }

            Debug.Log($"CraftSystem: Rarity roll — {primaryRarity} + {secondaryRarity} → {outputRarity} ({result.displayName})");

            playerInventory.RemoveSO(primarySlot);
            if (secondarySlot != null)
                playerInventory.RemoveSO(secondarySlot);
            playerInventory.AddSO(result);
            primarySlot = null;
            secondarySlot = null;
            RefreshAfterCraft();
            return;
        }

        Debug.Log("No matching recipe.");
    }

    /// <summary>
    /// Confirms the staged ingredient(s) are still physically in inventory right before
    /// crafting consumes them. Without this, an item dragged into a craft slot can be eaten
    /// (or otherwise removed) in the meantime — Craft() would then either silently skip
    /// removing it (free result) or remove an unrelated item that happens to share the same
    /// SO. Also requires a second physical copy when primary and secondary reference the
    /// same SO, so one item can't be double-counted as both ingredients.
    /// Clears whichever slot went stale and refreshes the UI; leaves a still-valid slot staged.
    /// </summary>
    private bool IngredientsStillAvailable()
    {
        if (primarySlot == null) return false;

        int neededOfPrimary = (secondarySlot == primarySlot) ? 2 : 1;
        if (CountSO(primarySlot) < neededOfPrimary)
        {
            Debug.LogWarning($"CraftSystem: '{primarySlot.displayName}' is no longer available in inventory — aborting craft.");
            primarySlot = null;
            RefreshUI();
            return false;
        }

        if (secondarySlot != null && secondarySlot != primarySlot && !playerInventory.HasSO(secondarySlot))
        {
            Debug.LogWarning($"CraftSystem: '{secondarySlot.displayName}' is no longer available in inventory — aborting craft.");
            secondarySlot = null;
            RefreshUI();
            return false;
        }

        return true;
    }

    private int CountSO(StatsModifierSO so)
    {
        int count = 0;
        foreach (var item in playerInventory.InventorySlots)
            if (item != null && item.ModifierSO == so) count++;
        return count;
    }

    private CraftRecipeSO FindMatch()
    {
        if (recipes == null) return null;
        foreach (var r in recipes)
            if (r != null && r.Matches(primarySlot, secondarySlot))
                return r;
        return null;
    }

    private RarityRecipeSO FindRarityMatch()
    {
        if (rarityRecipes == null) return null;
        foreach (var r in rarityRecipes)
            if (r != null && r.Matches(primarySlot, secondarySlot))
                return r;
        return null;
    }

    // ------------------------------------------------------------------ UI refresh

    public void RefreshUI()
    {
        if (primaryUI?.slotImage != null) primaryUI.slotImage.enabled = (primarySlot != null);
        if (secondaryUI?.slotImage != null) secondaryUI.slotImage.enabled = (secondarySlot != null);

        if (resultSlotImage == null) return;

        // Exact recipe match
        CraftRecipeSO match = FindMatch();
        if (match != null && match.result != null)
        {
            resultSlotImage.enabled = true;
            resultSlotImage.texture = match.result.Image;
            return;
        }

        // Rarity recipe match — preview the best possible (higher rarity) result
        RarityRecipeSO rarityMatch = FindRarityMatch();
        if (rarityMatch != null && primarySlot != null)
        {
            Rarity secondaryRarity = secondarySlot != null ? secondarySlot.rarity : primarySlot.rarity;
            Rarity higher = (Rarity)Mathf.Max((int)primarySlot.rarity, (int)secondaryRarity);
            StatsModifierSO preview = rarityMatch.GetResult(higher);
            resultSlotImage.enabled = preview != null;
            if (preview != null) resultSlotImage.texture = preview.Image;
            return;
        }

        resultSlotImage.enabled = false;
        resultSlotImage.texture = null;
    }

    public void ResetSlots()
    {
        primarySlot = null;
        secondarySlot = null;

        if (primaryUI?.slotImage != null)
        {
            primaryUI.slotImage.texture = null;
            primaryUI.slotImage.enabled = false;
        }

        if (secondaryUI?.slotImage != null)
        {
            secondaryUI.slotImage.texture = null;
            secondaryUI.slotImage.enabled = false;
        }

        if (resultSlotImage != null)
        {
            resultSlotImage.texture = null;
            resultSlotImage.enabled = false;
        }

        if (playerInventory != null)
        {
            foreach (RawImage rawImage in playerInventory.UISlots)
            {
                if (rawImage == null) continue;
                DraggableInventorySlotUI slot = rawImage.GetComponent<DraggableInventorySlotUI>();
                if (slot == null) continue;

                slot.craftInSlot = false;
                slot.dragIconImage.color = Color.white;
                slot.dragIconImage.raycastTarget = true;
                slot.removed = false;
            }
        }

        RefreshUI();
    }
    public void RefreshAfterCraft()
    {
        if (primaryUI?.slotImage != null) primaryUI.slotImage.enabled = false;
        if (secondaryUI?.slotImage != null) secondaryUI.slotImage.enabled = false;
        if (resultSlotImage != null) resultSlotImage.enabled = false;

        playerInventory.RefreshUI();

        for (int i = 0; i < playerInventory.UISlots.Count; i++)
        {
            var draggable = playerInventory.UISlots[i].GetComponent<DraggableInventorySlotUI>();

            if (draggable != null)
                draggable.RefreshDisplay(i);
        }
    }

    public void RefreshUIAfterDrop() => RefreshUI();
}




