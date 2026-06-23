using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DraggableInventorySlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
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
    public RenderTexture GetIcon()
    {
        var item = GetItem();
        return item != null ? item.renderTexture : null;
    }

    // ------------------------------------------------------------------ Drag events

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (removed || GetItem() == null) return;

        dragIconOriginalParent = dragIcon.parent;

        // Changed from .sprite to .texture
        dragIconImage.texture = GetIcon();
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

    // ------------------------------------------------------------------ Reset (called by CraftSystem after crafting)

    public void RefreshDisplay(int newSlotIndex)
    {
        slotIndex = newSlotIndex;
        removed = false;
        craftInSlot = false;

        dragIcon.anchoredPosition = originalPosition;

        dragIconImage.raycastTarget = true;
        dragIconImage.color = Color.white;

        InventoryItem item = GetItem();

        if (item != null)
        {
            // Changed from .sprite to .texture
            dragIconImage.texture = item.Image;
            dragIconImage.enabled = true;
        }
        else
        {
            dragIconImage.texture = null;
            dragIconImage.enabled = false;
        }
    }
}