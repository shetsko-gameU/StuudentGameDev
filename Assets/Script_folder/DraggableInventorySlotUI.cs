using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DraggableInventorySlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [Header("Wiring")]
    public Inventory inventory;
    public int slotIndex;
    public RectTransform dragIcon;

    // Changed from Image to RawImage
    public RawImage dragIconImage;

    private Canvas rootCanvas;
    private Transform dragIconOriginalParent;
    private Vector2 originalPosition;

    public bool removed;
    public bool craftInSlot;

    private void Awake()
    {
        rootCanvas = GetComponentInParent<Canvas>();
        originalPosition = dragIcon.anchoredPosition;
        inventory = FindAnyObjectByType<Inventory>();
    }

    // ------------------------------------------------------------------ Item helpers

    private InventoryItem GetItem()
    {
        if (inventory == null) return null;
        if (slotIndex < 0 || slotIndex >= inventory.InventorySlots.Count) return null;
        return inventory.InventorySlots[slotIndex];
    }

    public StatsModifierSO GetSO()
    {
        var item = GetItem();
        return item != null ? item.ModifierSO : null;
    }

    // Changed return type from Sprite to Texture2D
    public Texture2D GetIcon()
    {
        var item = GetItem();
        return item != null ? item.Image : null;
    }

    // ------------------------------------------------------------------ Drag events

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (removed || GetItem() == null) return;

        dragIconOriginalParent = dragIcon.parent;

        // Changed from .sprite to .texture
        dragIconImage.texture = GetIcon();
        dragIcon.position = eventData.position;

        // The dragged icon stays at its original sibling index while following the cursor —
        // it isn't reparented to float above everything. Sibling order decides raycast
        // priority for overlapping UI, so a later-indexed slot's flying icon would otherwise
        // win the raycast over an earlier-indexed slot it passes over, stealing its own drop.
        dragIconImage.raycastTarget = false;

        removed = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragIcon == null) return;
        dragIcon.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (dragIcon == null) return;

        if (dragIconOriginalParent != null)
            dragIcon.SetParent(dragIconOriginalParent, true);

        dragIcon.anchoredPosition = originalPosition;

        if (craftInSlot == false)
        {
            removed = false;
            dragIconImage.enabled = true;
            dragIconImage.raycastTarget = true;
        }
    }

    // ------------------------------------------------------------------ Drop (item dragged from another inventory slot onto this one)

    /// <summary>
    /// Handles another inventory item being dropped onto this slot — swaps the two slots'
    /// contents (or just moves, if this slot was empty). The dragged-from slot's own visuals
    /// reset themselves via its OnEndDrag, which always fires right after this.
    /// </summary>
    public void OnDrop(PointerEventData eventData)
    {
        if (removed) return; // this slot is mid-drag itself, or staged in a craft slot

        var dragged = eventData.pointerDrag;
        if (dragged == null) return;

        var sourceSlot = dragged.GetComponent<DraggableInventorySlotUI>();
        if (sourceSlot == null || sourceSlot == this) return;

        if (inventory == null) return;

        inventory.SwapSlots(sourceSlot.slotIndex, slotIndex);
    }

    // ------------------------------------------------------------------ Reset (called by CraftSystem after crafting)

    public void RefreshDisplay(int newSlotIndex)
    {
        slotIndex = newSlotIndex;
        removed = false;
        craftInSlot = false;

        dragIcon.anchoredPosition = originalPosition;

        dragIconImage.raycastTarget = true;

        InventoryItem item = GetItem();

        if (item != null)
        {
            // Changed from .sprite to .texture
            dragIconImage.texture = item.Image;
            dragIconImage.color = Color.white;
        }
        else
        {
            dragIconImage.texture = null;

            // Made invisible via alpha rather than disabled — a disabled Graphic drops out of
            // Unity's raycast registry, which would make this slot unable to receive OnDrop.
            dragIconImage.color = Color.clear;
        }

        // Stays enabled either way, for the same reason.
        dragIconImage.enabled = true;
    }
}