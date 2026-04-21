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
    private Vector2 OriginalPosition;
    public bool removed;
    public bool craftInSlot;
    

    private void Awake()
    {
        rootCanvas = GetComponentInParent<Canvas>();
        OriginalPosition = dragIcon.position;
    }

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

    public void OnBeginDrag(PointerEventData eventData)
    {

       // if (GetSO() == null) return;

        //if (dragIcon == null || dragIconImage == null) return;

        //dragIconImage.raycastTarget = false;

        dragIconOriginalParent = dragIcon.parent;
        // dragIcon.SetParent(rootCanvas.transform, true); // ensure it renders on top
        dragIcon.gameObject.SetActive(true);

        removed = true;

        dragIconImage.sprite = GetIcon();
        // dragIconImage.enabled = (dragIconImage.sprite != null);
        //dragIconImage.enabled = false;
        
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

        
        if (dragIconOriginalParent != null)
            dragIcon.SetParent(dragIconOriginalParent, true);
            dragIcon.position = OriginalPosition;
       if (craftInSlot == false)
        {
            removed = false;
            dragIconImage.enabled = true;
        }

    }
}
