using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventorySlot: MonoBehaviour
{ 
    public Image icon;
    public Image border;
    public TMP_Text quantityText;

    public void ClearSlot()
    {
        icon.enabled = false;
        quantityText.enabled = false;
    }

    public void EnableSlot()
    {
        icon.enabled = true;
        quantityText.enabled = true;
    }

public void DrawSlot(InventoryItem item)
    {
        if(item == null)
        {
            ClearSlot();
            return;
        }
        EnableSlot();

        icon.sprite = item.item.itemIcon;
        quantityText.text = item.stackSize.ToString();

    }

    public void SetSelected(bool isSelected)
    {
        border.enabled = isSelected;
         
    }

}