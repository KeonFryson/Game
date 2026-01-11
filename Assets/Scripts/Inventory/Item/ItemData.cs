using System;
using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class ItemData : ScriptableObject
{
    [SerializeField] public string itemName;
    [SerializeField] public int itemID;
    [SerializeField] public string itemDescription;
    [SerializeField][Range(1,999)] public int maxStackSize;
    [SerializeField] public Sprite itemIcon;
    [SerializeField] public GameObject itemPrefab;

    [SerializeField] public int damage;
    [SerializeField] public int healingAmount;

}
