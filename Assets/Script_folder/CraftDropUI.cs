using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// One craft slot (primary or secondary) in the crafting UI. Receiving a drop from a
/// DraggableInventorySlotUI stages that item's StatsModifierSO into CraftSystem's
/// matching slot field and visually marks the source inventory slot as staged
/// (dragIconImage tinted to Inventory.removedColor, raycastTarget off) without actually
/// removing it from Inventory — CraftSystem.Craft() consumes it for real later.
///
/// Setup:
///   1. Add to each craft slot's RawImage GameObject in the crafting UI.
///   2. Set slotType to Primary or Secondary.
///   3. Assign slotImage — craftSystem auto-finds via FindAnyObjectByType.
/// </summary>
public class CraftDropSlotUI : MonoBehaviour, IDropHandler
{
    public enum SlotType { Primary, Secondary }
    public SlotType slotType;

    [Header("Wiring")]
    public CraftSystem craftSystem;

    // Changed from Image to RawImage
    public RawImage slotImage;

    public void Awake()
    {
      craftSystem = FindAnyObjectByType<CraftSystem>();
    }


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
