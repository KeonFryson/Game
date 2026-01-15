using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventorySlot : MonoBehaviour
{
    public Image icon;
    public Image border;
    public TMP_Text quantityText;
    public Slider cooldownSlider; // Add this - the slider overlay

    private void Awake()
    {
        // Make sure cooldown slider is hidden by default if it exists
        if (cooldownSlider != null)
        {
            cooldownSlider.value = 0f;
        }
    }

    public void ClearSlot()
    {
        icon.enabled = false;
        quantityText.enabled = false;
        if (cooldownSlider != null)
        {
            cooldownSlider.value = 0f;
        }
    }

    public void EnableSlot()
    {
        icon.enabled = true;
        quantityText.enabled = true;
    }

    public void DrawSlot(InventoryItem item)
    {
        if (item == null)
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

    public void UpdateCooldown(float cooldownPercent)
    {
        if (cooldownSlider != null)
        {
            // cooldownPercent should be 0-1, where 1 = full cooldown, 0 = ready
            cooldownSlider.value = cooldownPercent;
        }
    }
}