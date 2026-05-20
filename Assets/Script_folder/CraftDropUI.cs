using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CraftDropSlotUI : MonoBehaviour, IDropHandler
{
    public enum SlotType { Primary, Secondary }
    public SlotType slotType;

    [Header("Wiring")]
    public CraftSystem craftSystem;

    // Changed from Image to RawImage
    public RawImage slotImage;

    public void OnDrop(PointerEventData eventData)
    {
        var dragged = eventData.pointerDrag;
        if (dragged == null) return;

        var dragSlot = dragged.GetComponent<DraggableInventorySlotUI>();
        if (dragSlot == null) return;

        StatsModifierSO so = dragSlot.GetSO();
        if (so == null) return;

        if (slotType == SlotType.Primary) craftSystem.primarySlot = so;
        if (slotType == SlotType.Secondary) craftSystem.secondarySlot = so;

        if (slotImage != null)
        {
            // Changed from .sprite to .texture
            slotImage.texture = dragSlot.GetIcon();
            slotImage.enabled = true;
        }

        dragSlot.craftInSlot = true;
        dragSlot.dragIconImage.color = dragSlot.inventory.removedColor;
        dragSlot.dragIconImage.raycastTarget = false;
        dragSlot.removed = true;

        craftSystem.RefreshUIAfterDrop();
    }
}
