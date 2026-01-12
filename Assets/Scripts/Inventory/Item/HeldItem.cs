using UnityEngine;

public class HeldItem : MonoBehaviour
{
    private InputSystem_Actions inputActions;
    private PlayerMovement playerController;
    private InventoryManger inventoryManager;
    private Inventroy inventory;
    private InventoryItem currentHeldItem;

    private GameObject currentItemInstance;

    private Transform HeldItemSlot;
    void Awake()
    {
        inputActions = new InputSystem_Actions();
        playerController = GetComponentInParent<PlayerMovement>();
        HeldItemSlot = this.transform;
        inventoryManager = FindFirstObjectByType<InventoryManger>();
        inventory = FindFirstObjectByType<Inventroy>();
    }


    private void OnEnable()
    {
        inputActions.Enable();
        

        if (inventoryManager != null)
        {
            Inventroy.OnInventoryChanged += OnInventoryChanged;
        }
        else
        {
            Debug.LogWarning("InventoryManager not found in the scene.");
        }
    }

    private void OnDisable()
    {
        
        inputActions.Disable();

        if (inventoryManager != null)
        {
            Inventroy.OnInventoryChanged -= OnInventoryChanged;
        }
    }

    private void Update()
    {
        if (playerController == null || playerController.isDead)
            return;


        UpdateHeldItem();

        


    }

    private void OnInventoryChanged(System.Collections.Generic.List<InventoryItem> currentInventory)
    {
        UpdateHeldItem();
    }

    private void UpdateHeldItem()
    {
        if (inventoryManager == null || inventory == null)
            return;

        int selectedSlot = inventoryManager.GetSelectedSlot();

        if (selectedSlot >= 0 && selectedSlot < inventory.inventory.Count)
        {
            InventoryItem selectedItem = inventory.inventory[selectedSlot];

            if (selectedItem != null && selectedItem.item != null)
            {
                // Only destroy and recreate if the item has changed
                if (currentHeldItem != selectedItem)
                {
                    // Destroy previous item instance if it exists
                    if (currentItemInstance != null)
                    {
                        Destroy(currentItemInstance);
                    }

                    // Instantiate the item prefab as a child of the hand
                    if (selectedItem.item.itemPrefab != null)
                    {
                        currentItemInstance = Instantiate(selectedItem.item.itemPrefab, HeldItemSlot);
                        currentItemInstance.transform.localPosition = Vector3.zero;
                        playerController.isHoldingItem = true;
                    }
                    else
                    {
                        // Fallback to sprite if no prefab is assigned
                        Debug.LogWarning("Item prefab is not assigned for item: " + selectedItem.item.itemName);
                        playerController.isHoldingItem = true;
                    }

                    currentHeldItem = selectedItem;
                }
            }
            else
            {
                // No item in this slot
                ClearHeldItem();
            }
        }
        else
        {
            // Selected slot is empty
            ClearHeldItem();
        }
    }

    private void ClearHeldItem()
    {
        if (currentItemInstance != null)
        {
            Destroy(currentItemInstance);
            currentItemInstance = null;
        }

        currentHeldItem = null;
        playerController.isHoldingItem = false;
    }

}
