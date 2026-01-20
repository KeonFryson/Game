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
    public Inventroy inventory;
    public PlayerMovement playerController;
    public SpellCastingManager spellCastingManager; // Add reference to spell manager

    private void Awake()
    {
        inputActions = new InputSystem_Actions();
        inventory = GetComponent<Inventroy>();
        playerController = GetComponent<PlayerMovement>();
        spellCastingManager = GetComponent<SpellCastingManager>(); // Get reference

        if (inventory == null)
        {
            inventory = FindFirstObjectByType<Inventroy>();
        }
        if (playerController == null)
        {
            playerController = FindFirstObjectByType<PlayerMovement>();
        }
        if (spellCastingManager == null)
        {
            spellCastingManager = FindFirstObjectByType<SpellCastingManager>();
        }
    }

    private void Start()
    {
        InitializeHotbar();
        UpdateSlotSelection();
    }

    private void Update()
    {
        // Update cooldowns every frame
        UpdateCooldownVisuals();
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
                // Drop the item 2 units in front of the player
                Vector3 dropPosition = playerController.transform.position + playerController.transform.forward;

                GameObject droppedItem = Instantiate(itemToDrop.item.itemPrefab, dropPosition, itemToDrop.item.itemPrefab.transform.rotation);

                Debug.Log("Dropped: " + itemToDrop.item.itemName);

                // Remove the item from inventory
                inventory.Remove(itemToDrop.item);
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

    void UpdateCooldownVisuals()
    {
        if (spellCastingManager == null || inventory == null || inventory.inventory == null)
            return;

        for (int i = 0; i < inventorySlots.Count; i++)
        {
            if (i < inventory.inventory.Count)
            {
                InventoryItem item = inventory.inventory[i];

                if (item != null && item.item != null && item.item.isSpell)
                {
                    float remainingCooldown = spellCastingManager.GetSpellCooldownRemaining(item.item.itemID);
                    float totalCooldown = item.item.spellCooldown;

                    // Calculate percentage (1 = full cooldown, 0 = ready)
                    float cooldownPercent = totalCooldown > 0 ? remainingCooldown / totalCooldown : 0f;

                    inventorySlots[i].UpdateCooldown(cooldownPercent);
                }
                else
                {
                    // Not a spell, no cooldown
                    inventorySlots[i].UpdateCooldown(0f);
                }
            }
            else
            {
                inventorySlots[i].UpdateCooldown(0f);
            }
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