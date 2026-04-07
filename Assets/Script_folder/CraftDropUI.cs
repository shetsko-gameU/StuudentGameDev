using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CraftDropSlotUI : MonoBehaviour, IDropHandler
{
    public enum SlotType { Primary, Secondary }
    public SlotType slotType;

    [Header("Wiring")]
    public CraftSystem craftSystem;
    public Image slotImage;  // image to display what's in the craft slot

    public void OnDrop(PointerEventData eventData)
    {
        var dragged = eventData.pointerDrag;
        if (dragged == null) return;

        var dragSlot = dragged.GetComponent<DraggableInventorySlotUI>();
        if (dragSlot == null) return;

        StatsModifierSO so = dragSlot.GetSO();
        if (so == null) return;

        // Set craft system slot
        if (slotType == SlotType.Primary) craftSystem.primarySlot = so;
        if (slotType == SlotType.Secondary) craftSystem.secondarySlot = so;

        // Update craft slot icon (from inventory item image)
        if (slotImage != null)
        {
            slotImage.sprite = dragSlot.GetIcon();
            slotImage.enabled = true;
        }

        // craftSystem.RefreshUIAfterDrop(); // we'll add this helper below
    }
}
