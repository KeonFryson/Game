using UnityEngine;


[System.Serializable]

public class InventoryItem
{
    public ItemData item;
    public int stackSize;

    public InventoryItem(ItemData item)
    {
        this.item = item;

        AddToStack();

    }

    public void AddToStack()
    {
         
        stackSize++;
    }

    public void RemoveFromStack()
    {
        stackSize--;
    }


}
