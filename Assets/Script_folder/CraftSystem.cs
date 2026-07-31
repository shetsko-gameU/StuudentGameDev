using UnityEngine;
using UnityEngine.UI;

<<<<<<< HEAD
=======

>>>>>>> ScriptBreanchfixs
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
<<<<<<< HEAD
=======
    public RarityRecipeSO[] rarityRecipes;
>>>>>>> ScriptBreanchfixs

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

<<<<<<< HEAD
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
=======
        // Try exact recipe first
        CraftRecipeSO recipe = FindMatch();
        if (recipe != null)
        {
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
>>>>>>> ScriptBreanchfixs
    }

    private CraftRecipeSO FindMatch()
    {
        if (recipes == null) return null;
<<<<<<< HEAD

        foreach (var r in recipes)
        {
            if (r != null && r.Matches(primarySlot, secondarySlot))
                return r;
        }
=======
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
>>>>>>> ScriptBreanchfixs
        return null;
    }

    // ------------------------------------------------------------------ UI refresh

    public void RefreshUI()
    {
        if (primaryUI?.slotImage != null) primaryUI.slotImage.enabled = (primarySlot != null);
        if (secondaryUI?.slotImage != null) secondaryUI.slotImage.enabled = (secondarySlot != null);

        if (resultSlotImage == null) return;

<<<<<<< HEAD
        CraftRecipeSO match = FindMatch();
        bool showResult = (primarySlot != null) && (secondarySlot != null)
                                   && (match != null) && (match.result != null);

        resultSlotImage.enabled = showResult;

        if (showResult)
        {
            // Changed from .sprite to .texture
            resultSlotImage.texture = match.result.Image;
        }
    }

=======
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
>>>>>>> ScriptBreanchfixs
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




