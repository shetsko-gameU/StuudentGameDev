using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DraggableInventorySlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Wiring")]
    public Inventory inventory;           // reference to player inventory
    public int slotIndex;                 // which index this UI slot represents
    public RectTransform dragIcon;        // your CraftSystem.DragImage (an Image on top of UI)
    public Image dragIconImage;           // Image component on dragIcon

    private Canvas rootCanvas;
    private Transform dragIconOriginalParent;

    private void Awake()
    {
        rootCanvas = GetComponentInParent<Canvas>();
    }

    public StatsModifierSO GetSO()
    {
        if (inventory == null) return null;
        if (slotIndex < 0 || slotIndex >= inventory.InventorySlots.Count) return null;
        var item = inventory.InventorySlots[slotIndex];
        return item != null ? item.ModifierSO : null;
    }

    public Sprite GetIcon()
    {
        if (inventory == null) return null;
        if (slotIndex < 0 || slotIndex >= inventory.InventorySlots.Count) return null;
        var item = inventory.InventorySlots[slotIndex];
        return item != null ? item.Image : null; // using Inventory_Item.Image (since SO has no icon)
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (GetSO() == null) return;

        if (dragIcon == null || dragIconImage == null) return;

        dragIconOriginalParent = dragIcon.parent;
        dragIcon.SetParent(rootCanvas.transform, true); // ensure it renders on top
        dragIcon.gameObject.SetActive(true);

        dragIconImage.sprite = GetIcon();
        dragIconImage.enabled = (dragIconImage.sprite != null);

        dragIcon.position = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragIcon == null) return;
        dragIcon.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (dragIcon == null) return;

        dragIcon.gameObject.SetActive(false);
        if (dragIconOriginalParent != null)
            dragIcon.SetParent(dragIconOriginalParent, true);
    }
}
