using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Inventroy : MonoBehaviour
{
    public static event Action<List<InventoryItem>> OnInventoryChanged;

    public List<InventoryItem> inventory;
    private Dictionary<int, List<InventoryItem>> itemDictionary;



    [SerializeField] private int maxInventorySize = 4; // Maximum number of inventory slots


    private void Awake()
    {
        inventory = new List<InventoryItem>();
        itemDictionary = new Dictionary<int, List<InventoryItem>>();
    }


    public bool Add(ItemData item)
    {
        if (itemDictionary.TryGetValue(item.itemID, out List<InventoryItem> itemStacks))
        {
            // Try to add to an existing stack that isn't full
            InventoryItem stackWithSpace = itemStacks.FirstOrDefault(stack =>
                item.maxStackSize > 1 && stack.stackSize < item.maxStackSize);

            if (stackWithSpace != null)
            {
                stackWithSpace.AddToStack();
                Debug.Log("Added to existing stack: " + item.itemName + " New stack size: " + stackWithSpace.stackSize);
                return true;
            }
            else
            {
                // Check if inventory is full before creating new stack
                if (inventory.Count >= maxInventorySize)
                {
                    Debug.LogWarning("Inventory is full! Cannot add: " + item.itemName);
                    return false;
                }

                // Create new stack/entry
                InventoryItem newInventroyItem = new InventoryItem(item);
                inventory.Add(newInventroyItem);
                itemStacks.Add(newInventroyItem);
                Debug.Log("Created new stack for: " + item.itemName);
                OnInventoryChanged?.Invoke(inventory);
                return true;
            }
        }
        else
        {
            // Check if inventory is full before adding first instance
            if (inventory.Count >= maxInventorySize)
            {
                Debug.LogWarning("Inventory is full! Cannot add: " + item.itemName);
                return false;
            }

            // First instance of this item type
            InventoryItem newInventroyItem = new InventoryItem(item);
            inventory.Add(newInventroyItem);
            itemDictionary.Add(item.itemID, new List<InventoryItem> { newInventroyItem });
            Debug.Log("Added new item to inventory: " + item.itemName);
            OnInventoryChanged?.Invoke(inventory);
            return true;
        }
    }

    public void Remove(ItemData item)
    {
        if (itemDictionary.TryGetValue(item.itemID, out List<InventoryItem> itemStacks) && itemStacks.Count > 0)
        {
            // Remove from the first available stack
            InventoryItem inventroyItem = itemStacks[0];
            inventroyItem.RemoveFromStack();

            if (inventroyItem.stackSize <= 0)
            {
                inventory.Remove(inventroyItem);
                itemStacks.Remove(inventroyItem);

                // Clean up dictionary entry if no stacks remain
                if (itemStacks.Count == 0)
                {
                    itemDictionary.Remove(item.itemID);
                }
                OnInventoryChanged?.Invoke(inventory);
            }
            Debug.Log("Removed item from inventory: " + item.itemName + " New stack size: " + inventroyItem.stackSize);
        }
    }

    public bool IsFull()
    {
        return inventory.Count >= maxInventorySize;
    }

    public int GetCurrentSize()
    {
        return inventory.Count;
    }

    public int GetMaxSize()
    {
        return maxInventorySize;
    }
}