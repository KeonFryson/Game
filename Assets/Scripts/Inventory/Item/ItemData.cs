using System;
using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class ItemData : ScriptableObject
{
    [SerializeField] public string itemName;
    [SerializeField] public int itemID;
    [SerializeField] public string itemDescription;
    [SerializeField][Range(1, 999)] public int maxStackSize;
    [SerializeField] public Sprite itemIcon;
    [SerializeField] public GameObject itemPrefab;

    [Header("Weapon Properties")]
    [SerializeField] public bool isWeapon;
    [ConditionalHide("isWeapon", true)]
    [SerializeField] public int damage;

    [Header("Consumable Properties")]
    [SerializeField] public bool isConsumable;
    [ConditionalHide("isConsumable", true)]
    [SerializeField] public int healingAmount;

    [Header("Spell Properties")]
    [SerializeField] public bool isSpell;
    [ConditionalHide("isSpell", true)]
    [SerializeField] public SpellType spellType;
    [ConditionalHide("isSpell", true)]
    [SerializeField] public int manaCost;
    [ConditionalHide("isSpell", true)]
    [SerializeField] public float spellChannelTime;
    [ConditionalHide("isSpell", true)]
    [SerializeField] public float spellCooldown;
    [ConditionalHide("isSpell", true)]
    [SerializeField] public float spellSpeed = 10f;
    [ConditionalHide("isSpell", true)]
    [SerializeField] public GameObject spellEffectPrefab;
    [ConditionalHide("isSpell", true)]
    [SerializeField] public AnimationCurve channelScaleCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [ConditionalHide("isSpell", true)]
    [SerializeField] public Vector3 spellCastPointOffset = Vector3.zero; // Offset from default cast point
    [ConditionalHide("isSpell", true)]
    [SerializeField] public Transform customSpellCastPoint = null; // Optional: Override cast point entirely
}

public enum SpellType
{
    Fire,
    Water,
    Air,
    Earth
}