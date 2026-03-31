using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Craft_System : MonoBehaviour
{
    public GameObject CraftingMenu;
    public RectTransform DragImage;
    public bool NearCraftPot;
    public bool ItemSelected;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
        if (ItemSelected)
        {
          DragImage.anchoredPosition = new Vector2(Mouse.current.position.ReadValue().x, Mouse.current.position.ReadValue().y);
        }

        
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Craft_Pot")
        {
            NearCraftPot = true;

        }


    }
    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.tag == "Craft_Pot")
        {
            NearCraftPot = false;

        }


    }
    public void OpenCraftMenu()
    {
      if (NearCraftPot)
        {
            CraftingMenu.SetActive(true);

        }


    }
    public void CloseCraftMenu()
    {
        if (NearCraftPot)
        {
            CraftingMenu.SetActive(false);
        }
    }
    public void SelectItem(int slot)
    {
        ItemSelected = true;
    }

}
