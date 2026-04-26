using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DraggableInventorySlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Wiring")]
    public Inventory inventory;
    public int slotIndex;       // which index in InventorySlots this UI slot represents
    public RectTransform dragIcon;        // the child RectTransform that moves during drag
    public Image dragIconImage;   // the Image on dragIcon — this is the visible item icon

    private Canvas rootCanvas;
    private Transform dragIconOriginalParent;
    private Vector2 originalPosition;

    public bool removed;
    public bool craftInSlot;

    private void Awake()
    {
        rootCanvas = GetComponentInParent<Canvas>();
        originalPosition = dragIcon.anchoredPosition;
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

    public Sprite GetIcon()
    {
        var item = GetItem();
        return item != null ? item.Image : null;
    }

    // ------------------------------------------------------------------ Drag events

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (removed || GetItem() == null) return;

        dragIconOriginalParent = dragIcon.parent;

        dragIcon.gameObject.SetActive(true);
        dragIconImage.sprite = GetIcon();
        dragIcon.position = eventData.position;

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
        }
    }

    // ------------------------------------------------------------------ Called by CraftSystem after crafting

    /// <summary>
    /// Updates this slot to show a specific inventory index.
    /// Call this on every slot after a craft so slotIndex and both images
    /// are correct for the new inventory state.
    /// </summary>
    public void RefreshDisplay(int newSlotIndex)
    {
        // Update which inventory index this slot now represents
        slotIndex = newSlotIndex;
        removed = false;
        craftInSlot = false;

        // Snap the icon back to its home position in the slot
        dragIcon.anchoredPosition = originalPosition;

        // Restore raycast target so the slot can be clicked/dragged again
        dragIconImage.raycastTarget = true;
        dragIconImage.color = Color.white;

        InventoryItem item = GetItem();

        if (item != null)
        {
            // Slot has an item — show the icon
            dragIconImage.sprite = item.Image;
            dragIconImage.enabled = true;
        }
        else
        {
            // Slot is empty — hide the icon
            dragIconImage.sprite = null;
            dragIconImage.enabled = false;
        }
    }
}