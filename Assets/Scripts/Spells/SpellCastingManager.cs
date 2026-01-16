using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class SpellCastingManager : MonoBehaviour
{
    [Header("Caster Type")]
    [SerializeField] private bool isPlayerControlled = true;

    [Header("Player References")]
    [SerializeField] private InventoryManger inventoryManager;
    [SerializeField] private Inventroy inventory;

    [Header("Shared References")]
    [SerializeField] private Transform spellCastPoint;
    [SerializeField] private MonoBehaviour movementController; // PlayerMovement or Enemy

    [Header("Stats")]
    [SerializeField] private int maxMana = 100;
    [SerializeField] private float manaRegenRate = 5f;
    private int currentMana;

    [Header("Spell Settings")]
    [SerializeField] private float globalCooldownTime = 0.5f;
    private float globalCooldownTimer = 0f;

    [Header("Channel Settings")]
    [SerializeField] private bool canMoveWhileChanneling = false;
    [SerializeField] private bool showChannelEffect = true;
    [SerializeField] private GameObject chargeEffectPrefab;
    [SerializeField] private float chargeEffectRadius = 2f;
    [SerializeField] private float minChannelPercentForCast = 0.1f;

    private Dictionary<int, float> spellCooldowns = new Dictionary<int, float>();
    private InputSystem_Actions inputActions;

    // Channeling state
    private bool isChanneling = false;
    private float channelTimer = 0f;
    private ItemData channelingSpell = null;
    private GameObject channelEffectInstance = null;
    private GameObject chargeEffectInstance = null;
    private Vector3 originalEffectScale = Vector3.one;

    public delegate void ManaChangedDelegate(int currentMana, int maxMana);
    public static event ManaChangedDelegate OnManaChanged;

    public delegate void SpellCastDelegate(ItemData spell);
    public static event SpellCastDelegate OnSpellCast;

    public delegate void SpellChannelDelegate(ItemData spell, float progress);
    public static event SpellChannelDelegate OnSpellChannelStart;
    public static event SpellChannelDelegate OnSpellChannelUpdate;
    public static event SpellChannelDelegate OnSpellChannelComplete;
    public static event SpellChannelDelegate OnSpellChannelInterrupted;

    [SerializeField] public TMPro.TextMeshProUGUI manaText;

    private void Awake()
    {
        if (isPlayerControlled)
        {
            if (inventoryManager == null)
            {
                inventoryManager = GetComponent<InventoryManger>();
            }

            if (inventory == null)
            {
                inventory = GetComponent<Inventroy>();
            }

            inputActions = new InputSystem_Actions();
        }

        if (movementController == null)
        {
            movementController = GetComponent<PlayerMovement>();
            if (movementController == null)
            {
                movementController = GetComponent<Enemy>();
            }
        }

        if (spellCastPoint == null)
        {
            GameObject spellPointObj = GameObject.Find("SpellPoint");
            if (spellPointObj != null)
            {
                spellCastPoint = spellPointObj.transform;
            }
            else
            {
                spellCastPoint = transform;
            }
        }

        currentMana = maxMana;
    }

    private void OnEnable()
    {
        if (isPlayerControlled && inputActions != null)
        {
            inputActions.Player.Enable();
            inputActions.Player.Attack.performed += OnSpellKeyPressed;
            inputActions.Player.Attack.canceled += OnSpellKeyReleased;
        }
    }

    private void OnDisable()
    {
        if (isPlayerControlled && inputActions != null)
        {
            inputActions.Player.Attack.performed -= OnSpellKeyPressed;
            inputActions.Player.Attack.canceled -= OnSpellKeyReleased;
            inputActions.Player.Disable();
        }
    }

    private void Start()
    {
        OnManaChanged?.Invoke(currentMana, maxMana);
    }

    private void Update()
    {
        UpdateCooldowns();
        RegenerateMana();
        UpdateChanneling();

        if (manaText != null && isPlayerControlled)
        {
            manaText.text = $"Mana: {currentMana} / {maxMana}";
        }
    }

    private void OnSpellKeyPressed(InputAction.CallbackContext context)
    {
        if (!isChanneling)
        {
            TryStartCastSpell();
        }
    }

    private void OnSpellKeyReleased(InputAction.CallbackContext context)
    {
        if (isChanneling)
        {
            ReleaseChannel();
        }
    }

    private void UpdateCooldowns()
    {
        if (globalCooldownTimer > 0f)
        {
            globalCooldownTimer -= Time.deltaTime;
        }

        List<int> keysToUpdate = new List<int>(spellCooldowns.Keys);
        foreach (int spellID in keysToUpdate)
        {
            if (spellCooldowns[spellID] > 0f)
            {
                spellCooldowns[spellID] -= Time.deltaTime;
            }
        }
    }

    private void UpdateChanneling()
    {
        if (!isChanneling || channelingSpell == null) return;

        channelTimer += Time.deltaTime;
        float progress = Mathf.Clamp01(channelTimer / channelingSpell.spellChannelTime);

        Transform currentCastPoint = GetSpellCastPoint(channelingSpell);

        if (channelEffectInstance != null)
        {
            if (channelingSpell.channelScaleCurve != null)
            {
                float scaleMultiplier = channelingSpell.channelScaleCurve.Evaluate(progress);
                channelEffectInstance.transform.localScale = originalEffectScale * scaleMultiplier;
            }

            channelEffectInstance.transform.position = currentCastPoint.position;
            channelEffectInstance.transform.rotation = currentCastPoint.rotation;
        }

        if (chargeEffectInstance != null)
        {
            ParticleSystem ps = chargeEffectInstance.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var emission = ps.emission;
                emission.rateOverTime = 50f + (progress * 150f);

                var shape = ps.shape;
                shape.radius = chargeEffectRadius * (1f - (progress * 0.5f));
            }
        }

        OnSpellChannelUpdate?.Invoke(channelingSpell, progress);

        if (channelTimer >= channelingSpell.spellChannelTime)
        {
            CompleteChannel();
        }
    }

    private void RegenerateMana()
    {
        if (currentMana < maxMana && !isChanneling)
        {
            currentMana = Mathf.Min(currentMana + Mathf.RoundToInt(manaRegenRate * Time.deltaTime), maxMana);
            OnManaChanged?.Invoke(currentMana, maxMana);
        }
    }

    private Transform GetSpellCastPoint(ItemData spell)
    {
        if (spell.customSpellCastPoint != null)
        {
            return spell.customSpellCastPoint;
        }
        return spellCastPoint;
    }

    private Vector3 GetSpellSpawnPosition(ItemData spell)
    {
        Transform castPoint = GetSpellCastPoint(spell);
        return castPoint.position + castPoint.TransformDirection(spell.spellCastPointOffset);
    }

    private Quaternion GetSpellSpawnRotation(ItemData spell)
    {
        Transform castPoint = GetSpellCastPoint(spell);
        return castPoint.rotation;
    }

    // PUBLIC API: Player uses this (via inventory selection)
    public bool TryStartCastSpell()
    {
        if (!isPlayerControlled)
        {
            Debug.LogWarning("TryStartCastSpell should only be called for player-controlled casters");
            return false;
        }

        if (inventory == null || inventoryManager == null)
        {
            Debug.LogWarning("Missing inventory references");
            return false;
        }

        int selectedSlot = inventoryManager.GetSelectedSlot();
        if (selectedSlot < 0 || selectedSlot >= inventory.inventory.Count)
        {
            return false;
        }

        InventoryItem currentItem = inventory.inventory[selectedSlot];
        if (currentItem == null || currentItem.item == null || !currentItem.item.isSpell)
        {
            return false;
        }

        return StartCastSpell(currentItem.item);
    }

    // PUBLIC API: AI uses this (direct spell casting)
    public bool CastSpell(ItemData spell, Transform customCastPoint = null)
    {
        if (spell == null || !spell.isSpell)
        {
            Debug.LogWarning("Invalid spell data provided");
            return false;
        }

        // Temporarily override cast point if provided
        Transform originalCastPoint = null;
        if (customCastPoint != null)
        {
            originalCastPoint = spellCastPoint;
            spellCastPoint = customCastPoint;
        }

        bool result = StartCastSpell(spell);

        // Restore original cast point
        if (originalCastPoint != null)
        {
            spellCastPoint = originalCastPoint;
        }

        return result;
    }

    // PUBLIC API: Force release channel (useful for interrupts)
    public void ForceReleaseChannel()
    {
        if (isChanneling)
        {
            ReleaseChannel();
        }
    }

    // PUBLIC API: Force interrupt channel
    public void ForceInterruptChannel()
    {
        if (isChanneling)
        {
            InterruptChannel();
        }
    }

    private bool StartCastSpell(ItemData spell)
    {
        if (!CanCastSpell(spell))
        {
            return false;
        }

        if (spell.spellChannelTime > 0f)
        {
            StartChanneling(spell);
            return true;
        }
        else
        {
            return ExecuteSpell(spell);
        }
    }

    private void StartChanneling(ItemData spell)
    {
        isChanneling = true;
        channelingSpell = spell;
        channelTimer = 0f;

        currentMana -= spell.manaCost;
        OnManaChanged?.Invoke(currentMana, maxMana);

        if (!canMoveWhileChanneling && movementController != null)
        {
            movementController.enabled = false;
        }

        Vector3 spawnPosition = GetSpellSpawnPosition(spell);
        // Use identity rotation instead of cast point rotation
        Quaternion spawnRotation = Quaternion.identity;

        if (showChannelEffect && spell.spellEffectPrefab != null)
        {
            channelEffectInstance = Instantiate(spell.spellEffectPrefab, spawnPosition, spawnRotation);
            Debug.Log("Spawned channel effect instance" + spawnRotation);
            originalEffectScale = spell.spellEffectPrefab.transform.localScale;
            channelEffectInstance.transform.localScale = Vector3.zero;

            Transform castPoint = GetSpellCastPoint(spell);
            channelEffectInstance.transform.SetParent(castPoint);
        }

        if (chargeEffectPrefab != null)
        {
            chargeEffectInstance = Instantiate(chargeEffectPrefab, spawnPosition, Quaternion.identity);
            Transform castPoint = GetSpellCastPoint(spell);
            chargeEffectInstance.transform.SetParent(castPoint);
        }

        OnSpellChannelStart?.Invoke(spell, 0f);
        Debug.Log($"Started channeling: {spell.itemName} for {spell.spellChannelTime}s");
    }
    private void CompleteChannel()
    {
        if (!isChanneling || channelingSpell == null) return;
        CastChanneledSpell(1f);
    }

    private void ReleaseChannel()
    {
        if (!isChanneling || channelingSpell == null) return;

        float progress = channelTimer / channelingSpell.spellChannelTime;

        if (progress >= minChannelPercentForCast)
        {
            CastChanneledSpell(progress);
        }
        else
        {
            InterruptChannel();
        }
    }

    private void CastChanneledSpell(float channelPercent)
    {
        if (!isChanneling || channelingSpell == null) return;

        ItemData spell = channelingSpell;
        isChanneling = false;
        channelingSpell = null;
        channelTimer = 0f;

        if (!canMoveWhileChanneling && movementController != null)
        {
            movementController.enabled = true;
        }

        globalCooldownTimer = globalCooldownTime;
        spellCooldowns[spell.itemID] = spell.spellCooldown;

        if (channelEffectInstance != null)
        {
            channelEffectInstance.transform.SetParent(null);
            channelEffectInstance.transform.localScale = originalEffectScale;

            Transform castPoint = GetSpellCastPoint(spell);
            SpellProjectile projectile = channelEffectInstance.GetComponent<SpellProjectile>();
            if (projectile != null)
            {
                projectile.Initialize(castPoint.forward, spell.spellSpeed, spell.spellChannelTime, channelPercent);
            }
            else
            {
                Debug.LogWarning($"Spell effect prefab for {spell.itemName} is missing SpellProjectile component!");
            }

            ParticleSystem ps = channelEffectInstance.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var main = ps.main;
                main.loop = false;
                Destroy(channelEffectInstance, main.duration + main.startLifetime.constantMax);
            }
            else
            {
                Destroy(channelEffectInstance, 5f);
            }

            channelEffectInstance = null;
        }

        if (chargeEffectInstance != null)
        {
            Destroy(chargeEffectInstance);
            chargeEffectInstance = null;
        }

        if (channelPercent >= 1f)
        {
            OnSpellChannelComplete?.Invoke(spell, 1f);
        }

        OnSpellCast?.Invoke(spell);

        if (channelPercent >= 1f)
        {
            Debug.Log($"Completed channeling: {spell.itemName}");
        }
        else
        {
            Debug.Log($"Released channeling early: {spell.itemName} at {channelPercent * 100f:F1}% power");
        }
    }

    private void InterruptChannel()
    {
        if (!isChanneling || channelingSpell == null) return;

        ItemData spell = channelingSpell;
        float progress = channelTimer / spell.spellChannelTime;

        isChanneling = false;
        channelingSpell = null;
        channelTimer = 0f;

        if (!canMoveWhileChanneling && movementController != null)
        {
            movementController.enabled = true;
        }

        if (channelEffectInstance != null)
        {
            Destroy(channelEffectInstance);
            channelEffectInstance = null;
        }

        if (chargeEffectInstance != null)
        {
            Destroy(chargeEffectInstance);
            chargeEffectInstance = null;
        }

        currentMana += spell.manaCost;
        currentMana = Mathf.Min(currentMana, maxMana);
        OnManaChanged?.Invoke(currentMana, maxMana);

        OnSpellChannelInterrupted?.Invoke(spell, progress);
        Debug.Log($"Interrupted channeling: {spell.itemName} at {progress * 100f:F1}% - Mana refunded");
    }

    private bool ExecuteSpell(ItemData spell)
    {
        currentMana -= spell.manaCost;
        OnManaChanged?.Invoke(currentMana, maxMana);

        globalCooldownTimer = globalCooldownTime;
        spellCooldowns[spell.itemID] = spell.spellCooldown;

        CastSpellEffect(spell);
        OnSpellCast?.Invoke(spell);

        return true;
    }

    private bool CanCastSpell(ItemData spell)
    {
        if (isChanneling)
        {
            Debug.Log("Already channeling a spell");
            return false;
        }

        if (globalCooldownTimer > 0f)
        {
            Debug.Log("Global cooldown active");
            return false;
        }

        if (spellCooldowns.ContainsKey(spell.itemID) && spellCooldowns[spell.itemID] > 0f)
        {
            Debug.Log($"{spell.itemName} is on cooldown: {spellCooldowns[spell.itemID]:F1}s remaining");
            return false;
        }

        if (currentMana < spell.manaCost)
        {
            Debug.Log($"Not enough mana. Need {spell.manaCost}, have {currentMana}");
            return false;
        }

        return true;
    }

    private void CastSpellEffect(ItemData spell)
    {
        Debug.Log($"Casting spell: {spell.itemName}");
        if (spell.spellEffectPrefab != null)
        {
            Vector3 spawnPosition = GetSpellSpawnPosition(spell);
            // Use identity rotation instead of cast point rotation
            Quaternion spawnRotation = Quaternion.identity;

            GameObject spellEffect = Instantiate(spell.spellEffectPrefab, spawnPosition, spawnRotation);
            Debug.Log("Spawned spell effect instance" + spawnRotation);
            spellEffect.transform.localScale = spell.spellEffectPrefab.transform.localScale;

            Transform castPoint = GetSpellCastPoint(spell);
            SpellProjectile projectile = spellEffect.GetComponent<SpellProjectile>();
            if (projectile != null)
            {
                Debug.Log("Spell roataion" + spellEffect.transform.rotation);
                projectile.Initialize(castPoint.forward, spell.spellSpeed, spell.spellChannelTime, 1f);
                Debug.Log("Spell roataion" + spellEffect.transform.rotation);
            }
            else
            {
                Debug.LogWarning($"Spell effect prefab for {spell.itemName} is missing SpellProjectile component!");
            }

            Debug.Log($"Cast spell: {spell.itemName}");
        }
        else
        {
            Debug.LogWarning($"Spell {spell.itemName} has no effect prefab assigned");
        }
    }

    public void AddMana(int amount)
    {
        currentMana = Mathf.Min(currentMana + amount, maxMana);
        OnManaChanged?.Invoke(currentMana, maxMana);
    }

    public void ConsumeMana(int amount)
    {
        currentMana = Mathf.Max(currentMana - amount, 0);
        OnManaChanged?.Invoke(currentMana, maxMana);
    }

    public float GetSpellCooldownRemaining(int spellID)
    {
        if (spellCooldowns.ContainsKey(spellID))
        {
            return Mathf.Max(0f, spellCooldowns[spellID]);
        }
        return 0f;
    }

    public bool IsGlobalCooldownActive()
    {
        return globalCooldownTimer > 0f;
    }

    public bool IsChanneling()
    {
        return isChanneling;
    }

    public float GetChannelProgress()
    {
        if (!isChanneling || channelingSpell == null) return 0f;
        return channelTimer / channelingSpell.spellChannelTime;
    }

    public int GetCurrentMana()
    {
        return currentMana;
    }

    public int GetMaxMana()
    {
        return maxMana;
    }

    public void SetMaxMana(int newMaxMana)
    {
        maxMana = newMaxMana;
        currentMana = Mathf.Min(currentMana, maxMana);
        OnManaChanged?.Invoke(currentMana, maxMana);
    }

    public void SetSpellPoint(Transform newSpellPoint)
    {
        spellCastPoint = newSpellPoint;
    }
}