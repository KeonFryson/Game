using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InventoryManger : MonoBehaviour
{
 

    public GameObject slotPrefab;

    public List<InventorySlot> inventorySlots = new List<InventorySlot>(4);

    private int currentSelectedSlot = 0;
    private InputSystem_Actions inputActions;
    private Inventroy inventory;
    public PlayerMovement playerController;
    private void Awake()
    { 
        inputActions = new InputSystem_Actions();
        inventory = GetComponent<Inventroy>();
        playerController = GetComponent<PlayerMovement>();
        if (inventory == null)
        {
            inventory = FindFirstObjectByType<Inventroy>();
        }
        if(playerController == null)
        {
            playerController = FindFirstObjectByType<PlayerMovement>();
        }
    }

    private void Start()
    {
        InitializeHotbar();
        UpdateSlotSelection();
    }

    public void OnEnable()
    {
        Inventroy.OnInventoryChanged += DrawHotbar;

        inputActions.Player.Enable();
        inputActions.Player.ScrollInventory.performed += OnScrollInventory;
        inputActions.Player.SelectSlot.performed += OnSelectSlot;
        inputActions.Player.DropItem.performed += OnDropItem;
    }

    public void OnDisable()
    {
        Inventroy.OnInventoryChanged -= DrawHotbar;

        inputActions.Player.ScrollInventory.performed -= OnScrollInventory;
        inputActions.Player.SelectSlot.performed -= OnSelectSlot;
        inputActions.Player.DropItem.performed -= OnDropItem;
        inputActions.Player.Disable();
    }

    private void OnScrollInventory(InputAction.CallbackContext context)
    {
        float scrollValue = context.ReadValue<float>();

        if (scrollValue > 0f)
        {
            currentSelectedSlot--;
            if (currentSelectedSlot < 0)
                currentSelectedSlot = inventorySlots.Count - 1;
            UpdateSlotSelection();
        }
        else if (scrollValue < 0f)
        {
            currentSelectedSlot++;
            if (currentSelectedSlot >= inventorySlots.Count)
                currentSelectedSlot = 0;
            UpdateSlotSelection();
        }
    }

    private void OnSelectSlot(InputAction.CallbackContext context)
    {
        // Get the control that triggered this (e.g., "1", "2", "3", "4")
        string keyName = context.control.name;

        if (int.TryParse(keyName, out int slotNumber))
        {
            int slotIndex = slotNumber - 1; // Convert 1-based to 0-based index
            if (slotIndex >= 0 && slotIndex < inventorySlots.Count)
            {
                currentSelectedSlot = slotIndex;
                UpdateSlotSelection();
            }
        }
    }

    private void OnDropItem(InputAction.CallbackContext context)
    {
        if (inventory == null || inventory.inventory == null || inventory.inventory.Count == 0)
            return;

        // Check if the current selected slot has an item
        if (currentSelectedSlot < inventory.inventory.Count)
        {
            InventoryItem itemToDrop = inventory.inventory[currentSelectedSlot];

            if (itemToDrop != null && itemToDrop.item != null && itemToDrop.item.itemPrefab != null)
            {
                // Get mouse position and calculate direction
                if (Mouse.current != null && UnityEngine.Camera.main != null)
                {
                    Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
                    Vector3 mouseWorldPos = UnityEngine.Camera.main.ScreenToWorldPoint(mouseScreenPos);
                    mouseWorldPos.z = 0f;

                    // Calculate direction from player to mouse
                    Vector2 dropDirection = (mouseWorldPos - playerController.transform.position).normalized;

                    // Drop the item 2 units in the mouse direction
                    Vector3 dropPosition = playerController.transform.position + (Vector3)(dropDirection * 2f);

                    GameObject droppedItem = Instantiate(itemToDrop.item.itemPrefab, dropPosition, itemToDrop.item.itemPrefab.transform.rotation);

                    Debug.Log("Dropped: " + itemToDrop.item.itemName);

                    // Remove the item from inventory
                    inventory.Remove(itemToDrop.item);
                }
            }
        }
    }

    void UpdateSlotSelection()
    {

        if (playerController.isDead)
            return;

        for (int i = 0; i < inventorySlots.Count; i++)
        {
            inventorySlots[i].SetSelected(i == currentSelectedSlot);
        }
    }

    void InitializeHotbar()
    {
        // Create hotbar slots from prefab
        for (int i = 0; i < inventorySlots.Capacity; i++)
        {
            GameObject slotInstance = Instantiate(slotPrefab, this.transform);
            InventorySlot slot = slotInstance.GetComponent<InventorySlot>();
            slot.ClearSlot();
            inventorySlots.Add(slot);
        }
    }

    void DrawHotbar(List<InventoryItem> currentInventory)
    {
        if (currentInventory == null) return;

        for (int i = 0; i < inventorySlots.Count; i++)
        {
            if (i < currentInventory.Count)
            {
                inventorySlots[i].DrawSlot(currentInventory[i]);
            }
            else
            {
                inventorySlots[i].ClearSlot();
            }
        }
    }

    void ResetInventory()
    {
        foreach (InventorySlot slot in inventorySlots)
        {
            slot.ClearSlot();
        }
    }

    public int GetSelectedSlot()
    {
        return currentSelectedSlot;
    }
}