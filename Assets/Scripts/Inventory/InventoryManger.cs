using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InventoryManger : MonoBehaviour
{
    public GameObject slotPrefab;

    public List<InventorySlot> inventorySlots;

    private int currentSelectedSlot = 0;
    private InputSystem_Actions inputActions;
    public Inventroy inventory;
    public PlayerMovement playerController;
    public SpellCastingManager spellCastingManager; // Add reference to spell manager

    public int inventorySize = 4;

    private void Awake()
    {

        inventorySlots = new List<InventorySlot>(inventorySize);
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
            // Search specifically for player's SpellCastingManager
            SpellCastingManager[] allManagers = FindObjectsByType<SpellCastingManager>(FindObjectsSortMode.None);
            foreach (SpellCastingManager manager in allManagers)
            {
                // Only use the one that has PlayerMovement (i.e., is on the player)
                if (manager.GetComponent<PlayerMovement>() != null)
                {
                    spellCastingManager = manager;
                    Debug.Log($"[InventoryManger] Found player's SpellCastingManager on {manager.gameObject.name}");
                    break;
                }
            }

            if (spellCastingManager == null)
            {
                Debug.LogError("[InventoryManger] Could not find player's SpellCastingManager!");
            }
        }
        else
        {
            Debug.Log($"[InventoryManger] Using SpellCastingManager from {spellCastingManager.gameObject.name}");
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
        if (playerController != null && playerController.isDead)
            return;

        for (int i = 0; i < inventorySlots.Count; i++)
        {
            inventorySlots[i].SetSelected(i == currentSelectedSlot);
        }
    }

    [Header("Debug")]
    [SerializeField] private bool enableCooldownDebug = false; // Add this field to toggle debug

    void UpdateCooldownVisuals()
    {
        // Early exit if required references are missing
        if (spellCastingManager == null || inventory == null || inventory.inventory == null || inventorySlots == null)
        {
            if (enableCooldownDebug)
            {
                Debug.LogWarning($"[InventoryManger] UpdateCooldownVisuals: Missing references - " +
                               $"spellCastingManager:{spellCastingManager != null}, " +
                               $"inventory:{inventory != null}, " +
                               $"inventory.inventory:{inventory?.inventory != null}, " +
                               $"inventorySlots:{inventorySlots != null}");
            }
            return;
        }

        // Cache the inventory count to avoid repeated property access
        int inventoryCount = inventory.inventory.Count;

        for (int i = 0; i < inventorySlots.Count; i++)
        {
            // Default to no cooldown
            float cooldownPercent = 0f;

            // Only check cooldown if slot has an item
            if (i < inventoryCount)
            {
                InventoryItem item = inventory.inventory[i];

                // Check if item exists and is a spell
                if (item != null && item.item != null && item.item.isSpell)
                {
                    float remainingCooldown = spellCastingManager.GetSlotCooldownRemaining(i);
                    float totalCooldown = item.item.spellCooldown;

                    // Calculate percentage (1 = full cooldown, 0 = ready)
                    if (totalCooldown > 0f)
                    {
                        cooldownPercent = Mathf.Clamp01(remainingCooldown / totalCooldown);

                        // Only log when there's an active cooldown
                        if (enableCooldownDebug && cooldownPercent > 0f)
                        {
                            Debug.Log($"[InventoryManger] Slot {i}: '{item.item.itemName}' | " +
                                    $"Remaining: {remainingCooldown:F2}s / {totalCooldown:F2}s | " +
                                    $"Percent: {cooldownPercent * 100f:F1}%");
                        }
                    }
                    else if (enableCooldownDebug)
                    {
                        Debug.LogWarning($"[InventoryManger] Slot {i}: Spell '{item.item.itemName}' has invalid totalCooldown: {totalCooldown}");
                    }
                }
            }

            // Update the visual regardless (either with cooldown or 0)
            if (inventorySlots[i] != null)
            {
                inventorySlots[i].UpdateCooldown(cooldownPercent);
            }
            else if (enableCooldownDebug)
            {
                Debug.LogError($"[InventoryManger] Slot {i}: InventorySlot component is null!");
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
            if (slot != null)
            {
                slot.ClearSlot();
                inventorySlots.Add(slot);
            }
            else
            {
                Debug.LogError($"Slot prefab at index {i} is missing InventorySlot component!");
            }
        }
    }

    void DrawHotbar(List<InventoryItem> currentInventory)
    {
        if (currentInventory == null || inventorySlots == null) return;

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
        if (inventorySlots == null) return;

        foreach (InventorySlot slot in inventorySlots)
        {
            if (slot != null)
            {
                slot.ClearSlot();
            }
        }
    }

    public int GetSelectedSlot()
    {
        return currentSelectedSlot;
    }
}