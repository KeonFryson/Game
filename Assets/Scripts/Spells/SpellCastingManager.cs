using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class SpellCastingManager : MonoBehaviour
{
    [Header("Caster Type")]
    [SerializeField] private bool isPlayerControlled = true;

    [Header("Debug")]
    [SerializeField] private bool enableDebug = false;

    [Header("Player References")]
    [SerializeField] private InventoryManger inventoryManager;
    [SerializeField] private Inventroy inventory;

    [Header("Shared References")]
    [SerializeField] private Transform spellCastPoint;
    [SerializeField] private MonoBehaviour movementController; // PlayerMovement or Enemy

    [Header("Crosshair Aiming")]
    [SerializeField] private Camera aimCamera;
    [SerializeField] private float maxAimDistance = 100f;
    [SerializeField] private LayerMask aimLayerMask = ~0; // Aim at everything by default

    [Header("Stats")]
    [SerializeField] private float maxMana = 100;
    [SerializeField] private float manaRegenRate = 5f;
    [SerializeField] private float currentMana;

    [Header("Spell Settings")]
    [SerializeField] private float globalCooldownTime = 0.5f;
    private float globalCooldownTimer = 0f;

    [Header("Channel Settings")]
    [SerializeField] private bool canMoveWhileChanneling = false;
    [SerializeField] private bool showChannelEffect = true;
    [SerializeField] private GameObject chargeEffectPrefab;
    [SerializeField] private float chargeEffectRadius = 2f;
    [SerializeField] private float minChannelPercentForCast = 0.1f;

    // Changed: Track cooldowns by slot index instead of spell ID
    private Dictionary<int, float> slotCooldowns = new Dictionary<int, float>();

    // Added: For AI enemies, track spell ID cooldowns since they don't use slots
    private Dictionary<int, float> spellCooldowns = new Dictionary<int, float>();

    private InputSystem_Actions inputActions;

    // Channeling state
    private bool isChanneling = false;
    private float channelTimer = 0f;
    private ItemData channelingSpell = null;
    private int channelingSlotIndex = -1; // Track which slot is channeling
    private GameObject channelEffectInstance = null;
    private GameObject chargeEffectInstance = null;
    private Vector3 originalEffectScale = Vector3.one;

    // Changed: Made events non-static (instance-based)
    public delegate void ManaChangedDelegate(float currentMana, float maxMana);
    public event ManaChangedDelegate OnManaChanged;

    public delegate void SpellCastDelegate(ItemData spell);
    public event SpellCastDelegate OnSpellCast;

    public delegate void SpellChannelDelegate(ItemData spell, float progress);
    public event SpellChannelDelegate OnSpellChannelStart;
    public event SpellChannelDelegate OnSpellChannelUpdate;
    public event SpellChannelDelegate OnSpellChannelComplete;
    public event SpellChannelDelegate OnSpellChannelInterrupted;

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

            // Get the camera from PlayerMovement if not assigned
            if (aimCamera == null)
            {
                PlayerMovement pm = GetComponent<PlayerMovement>();
                if (pm != null && pm.playerCamera != null)
                {
                    aimCamera = pm.playerCamera.GetComponent<Camera>();
                }
            }

            // Fallback to main camera
            if (aimCamera == null)
            {
                aimCamera = Camera.main;
            }
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

        // Update slot cooldowns (for player)
        List<int> slotKeysToUpdate = new List<int>(slotCooldowns.Keys);
        foreach (int slotIndex in slotKeysToUpdate)
        {
            if (slotCooldowns[slotIndex] > 0f)
            {
                slotCooldowns[slotIndex] -= Time.deltaTime;
            }
        }

        // Update spell ID cooldowns (for AI)
        List<int> spellKeysToUpdate = new List<int>(spellCooldowns.Keys);
        foreach (int spellID in spellKeysToUpdate)
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

            // Update position to follow cast point
            channelEffectInstance.transform.position = currentCastPoint.position;

            // Orient towards crosshair during channeling (world space rotation)
            Vector3 aimDirection = GetAimDirection();
            channelEffectInstance.transform.rotation = Quaternion.LookRotation(aimDirection);
        }

        if (chargeEffectInstance != null)
        {
            // Update charge effect position to follow cast point
            chargeEffectInstance.transform.position = currentCastPoint.position;

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
            currentMana = Mathf.Min(currentMana +  manaRegenRate * Time.deltaTime, maxMana);
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



    /// <summary>
    /// Get the direction to aim the spell based on crosshair position (screen center)
    /// </summary>
    private Vector3 GetAimDirection()
    {
        if (aimCamera == null)
        {
            // Fallback to forward direction if no camera
            return transform.forward;
        }

        // Raycast from screen center (crosshair position)
        Ray ray = aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        Vector3 targetPoint;

        if (Physics.Raycast(ray, out RaycastHit hit, maxAimDistance, aimLayerMask))
        {
            // Hit something, aim at that point
            targetPoint = hit.point;
        }
        else
        {
            // Nothing hit, aim at max distance
            targetPoint = ray.GetPoint(maxAimDistance);
        }

        // Calculate direction from spell cast point to target
        Transform castPoint = spellCastPoint != null ? spellCastPoint : transform;
        Vector3 direction = (targetPoint - castPoint.position).normalized;

        return direction;
    }



    // PUBLIC API: Player uses this (via inventory selection)
    public bool TryStartCastSpell()
    {
        if (!isPlayerControlled)
        {
            if (enableDebug) Debug.LogWarning("TryStartCastSpell should only be called for player-controlled casters");
            return false;
        }

        if (inventory == null || inventoryManager == null)
        {
            if (enableDebug) Debug.LogWarning("Missing inventory references");
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

        // Changed: Pass slot index to spell casting
        return StartCastSpell(currentItem.item, selectedSlot);
    }

    // PUBLIC API: AI uses this (direct spell casting)
    public bool CastSpell(ItemData spell, Transform customCastPoint = null)
    {
        if (spell == null || !spell.isSpell)
        {
            if (enableDebug) Debug.LogWarning("Invalid spell data provided");
            return false;
        }

        // Temporarily override cast point if provided
        Transform originalCastPoint = null;
        if (customCastPoint != null)
        {
            originalCastPoint = spellCastPoint;
            spellCastPoint = customCastPoint;
        }

        // Changed: AI uses spell ID for cooldowns, pass -1 for slot to indicate AI casting
        bool result = StartCastSpell(spell, -1);

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

    // Changed: Add slotIndex parameter
    private bool StartCastSpell(ItemData spell, int slotIndex)
    {
        if (!CanCastSpell(spell, slotIndex))
        {
            return false;
        }

        if (spell.spellChannelTime > 0f)
        {
            StartChanneling(spell, slotIndex);
            return true;
        }
        else
        {
            return ExecuteSpell(spell, slotIndex);
        }
    }

    // Changed: Add slotIndex parameter
    private void StartChanneling(ItemData spell, int slotIndex)
    {
        isChanneling = true;
        channelingSpell = spell;
        channelingSlotIndex = slotIndex;
        channelTimer = 0f;

        currentMana -= spell.manaCost;
        OnManaChanged?.Invoke(currentMana, maxMana);

        if (!canMoveWhileChanneling && movementController != null)
        {
            movementController.enabled = false;
        }

        Vector3 spawnPosition = GetSpellSpawnPosition(spell);
        Vector3 aimDirection = GetAimDirection();
        Quaternion spawnRotation = Quaternion.LookRotation(aimDirection);

        if (showChannelEffect && spell.spellEffectPrefab != null)
        {
            channelEffectInstance = Instantiate(spell.spellEffectPrefab, spawnPosition, spawnRotation);
            if (enableDebug) Debug.Log("Spawned channel effect instance, aiming towards crosshair");
            originalEffectScale = spell.spellEffectPrefab.transform.localScale;
            channelEffectInstance.transform.localScale = Vector3.zero;

            // Don't parent the channel effect - let it stay in world space so rotation works properly
            // Position and rotation will be updated in UpdateChanneling()
        }

        if (chargeEffectPrefab != null)
        {
            chargeEffectInstance = Instantiate(chargeEffectPrefab, spawnPosition, Quaternion.identity);
            // Don't parent the charge effect either - update position in UpdateChanneling()
        }

        OnSpellChannelStart?.Invoke(spell, 0f);
        if (enableDebug) Debug.Log($"Started channeling: {spell.itemName} for {spell.spellChannelTime}s");
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
        int slotIndex = channelingSlotIndex;
        isChanneling = false;
        channelingSpell = null;
        channelingSlotIndex = -1;
        channelTimer = 0f;

        if (!canMoveWhileChanneling && movementController != null)
        {
            movementController.enabled = true;
        }

        globalCooldownTimer = globalCooldownTime;

        // Changed: Set cooldown based on whether this is player (slot) or AI (spell ID)
        if (slotIndex >= 0)
        {
            // Player casting - use slot cooldown
            slotCooldowns[slotIndex] = spell.spellCooldown;
        }
        else
        {
            // AI casting - use spell ID cooldown
            spellCooldowns[spell.itemID] = spell.spellCooldown;
        }

        if (channelEffectInstance != null)
        {
            // Already in world space, no need to unparent
            channelEffectInstance.transform.localScale = originalEffectScale;

            Vector3 aimDirection = GetAimDirection();
            SpellProjectile projectile = channelEffectInstance.GetComponent<SpellProjectile>();
            if (projectile != null)
            {
                projectile.Initialize(aimDirection, spell.spellSpeed, spell.spellChannelTime, channelPercent);
            }
            else
            {
                if (enableDebug) Debug.LogWarning($"Spell effect prefab for {spell.itemName} is missing SpellProjectile component!");
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

        if (enableDebug)
        {
            if (channelPercent >= 1f)
            {
                Debug.Log($"Completed channeling: {spell.itemName}");
            }
            else
            {
                Debug.Log($"Released channeling early: {spell.itemName} at {channelPercent * 100f:F1}% power");
            }
        }
    }

    private void InterruptChannel()
    {
        if (!isChanneling || channelingSpell == null) return;

        ItemData spell = channelingSpell;
        float progress = channelTimer / spell.spellChannelTime;

        isChanneling = false;
        channelingSpell = null;
        channelingSlotIndex = -1;
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
        if (enableDebug) Debug.Log($"Interrupted channeling: {spell.itemName} at {progress * 100f:F1}% - Mana refunded");
    }

    // Changed: Add slotIndex parameter
    private bool ExecuteSpell(ItemData spell, int slotIndex)
    {
        currentMana -= spell.manaCost;
        OnManaChanged?.Invoke(currentMana, maxMana);

        globalCooldownTimer = globalCooldownTime;

        // Changed: Set cooldown based on whether this is player (slot) or AI (spell ID)
        if (slotIndex >= 0)
        {
            // Player casting - use slot cooldown
            slotCooldowns[slotIndex] = spell.spellCooldown;
        }
        else
        {
            // AI casting - use spell ID cooldown
            spellCooldowns[spell.itemID] = spell.spellCooldown;
        }

        CastSpellEffect(spell);
        OnSpellCast?.Invoke(spell);

        return true;
    }

    // Changed: Add slotIndex parameter
    private bool CanCastSpell(ItemData spell, int slotIndex)
    {
        if (isChanneling)
        {
            if (enableDebug) Debug.Log("Already channeling a spell");
            return false;
        }

        if (globalCooldownTimer > 0f)
        {
            if (enableDebug) Debug.Log("Global cooldown active");
            return false;
        }

        // Changed: Check cooldown based on whether this is player (slot) or AI (spell ID)
        if (slotIndex >= 0)
        {
            // Player casting - check slot cooldown
            if (slotCooldowns.ContainsKey(slotIndex) && slotCooldowns[slotIndex] > 0f)
            {
                if (enableDebug) Debug.Log($"{spell.itemName} in slot {slotIndex} is on cooldown: {slotCooldowns[slotIndex]:F1}s remaining");
                return false;
            }
        }
        else
        {
            // AI casting - check spell ID cooldown
            if (spellCooldowns.ContainsKey(spell.itemID) && spellCooldowns[spell.itemID] > 0f)
            {
                if (enableDebug) Debug.Log($"{spell.itemName} (ID: {spell.itemID}) is on cooldown: {spellCooldowns[spell.itemID]:F1}s remaining");
                return false;
            }
        }

        if (currentMana < spell.manaCost)
        {
            if (enableDebug) Debug.Log($"Not enough mana. Need {spell.manaCost}, have {currentMana}");
            return false;
        }

        return true;
    }

    private void CastSpellEffect(ItemData spell)
    {
        if (enableDebug) Debug.Log($"Casting spell: {spell.itemName}");
        if (spell.spellEffectPrefab != null)
        {
            Vector3 spawnPosition = GetSpellSpawnPosition(spell);
            Vector3 aimDirection = GetAimDirection();
            Quaternion spawnRotation = Quaternion.LookRotation(aimDirection);

            GameObject spellEffect = Instantiate(spell.spellEffectPrefab, spawnPosition, spawnRotation);
            if (enableDebug) Debug.Log("Spawned spell effect instance, aiming towards crosshair");
            spellEffect.transform.localScale = spell.spellEffectPrefab.transform.localScale;

            SpellProjectile projectile = spellEffect.GetComponent<SpellProjectile>();
            if (projectile != null)
            {
                projectile.Initialize(aimDirection, spell.spellSpeed, spell.spellChannelTime, 1f);
            }
            else
            {
                if (enableDebug) Debug.LogWarning($"Spell effect prefab for {spell.itemName} is missing SpellProjectile component!");
            }

            if (enableDebug) Debug.Log($"Cast spell: {spell.itemName}");
        }
        else
        {
            if (enableDebug) Debug.LogWarning($"Spell {spell.itemName} has no effect prefab assigned");
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

    // Changed: Get cooldown for a specific slot
    public float GetSlotCooldownRemaining(int slotIndex)
    {
        if (slotCooldowns.ContainsKey(slotIndex))
        {
            return Mathf.Max(0f, slotCooldowns[slotIndex]);
        }
        return 0f;
    }

    // Changed: Get cooldown percentage for a specific slot (for UI)
    public float GetSlotCooldownPercent(int slotIndex, float maxCooldown)
    {
        if (maxCooldown <= 0f) return 0f;
        float remaining = GetSlotCooldownRemaining(slotIndex);
        return Mathf.Clamp01(remaining / maxCooldown);
    }

    // Added: Get spell cooldown for AI enemies
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

    public float GetCurrentMana()
    {
        return currentMana;
    }

    public float GetMaxMana()
    {
        return maxMana;
    }

    public void SetMaxMana(float newMaxMana)
    {
        maxMana = newMaxMana;
        currentMana = Mathf.Min(currentMana, maxMana);
        OnManaChanged?.Invoke(currentMana, maxMana);
    }

    public void SetSpellPoint(Transform newSpellPoint)
    {
        spellCastPoint = newSpellPoint;
    }

    public void SetAimCamera(Camera camera)
    {
        aimCamera = camera;
    }

    public void OnDrawGizmos()
    {
        if (!enableDebug || aimCamera == null || spellCastPoint == null)
            return;

        // Draw the raycast from camera
        Ray ray = aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        Vector3 targetPoint;
        bool didHit = false;

        if (Physics.Raycast(ray, out RaycastHit hit, maxAimDistance, aimLayerMask))
        {
            // Hit something
            targetPoint = hit.point;
            didHit = true;

            // Draw camera ray to hit point in green
            Gizmos.color = Color.green;
            Gizmos.DrawLine(ray.origin, hit.point);

            // Draw hit point as a sphere
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(hit.point, 0.1f);

            // Draw hit normal
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(hit.point, hit.point + hit.normal * 0.5f);
        }
        else
        {
            // Nothing hit, aim at max distance
            targetPoint = ray.GetPoint(maxAimDistance);
            didHit = false;

            // Draw camera ray to max distance in cyan
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(ray.origin, targetPoint);

            // Draw end point
            Gizmos.color = Color.white;
            Gizmos.DrawSphere(targetPoint, 0.2f);
        }

        // Draw spell direction from cast point to target
        Vector3 direction = (targetPoint - spellCastPoint.position).normalized;
        Gizmos.color = didHit ? Color.blue : Color.magenta;
        Gizmos.DrawLine(spellCastPoint.position, spellCastPoint.position + direction * 3f);
        Gizmos.DrawSphere(spellCastPoint.position, 0.15f);

        // Draw arrow head for direction
        Vector3 arrowTip = spellCastPoint.position + direction * 3f;
        Vector3 right = Vector3.Cross(direction, Vector3.up).normalized * 0.2f;
        Vector3 up = Vector3.Cross(right, direction).normalized * 0.2f;

        Gizmos.DrawLine(arrowTip, arrowTip - direction * 0.3f + right);
        Gizmos.DrawLine(arrowTip, arrowTip - direction * 0.3f - right);
        Gizmos.DrawLine(arrowTip, arrowTip - direction * 0.3f + up);
        Gizmos.DrawLine(arrowTip, arrowTip - direction * 0.3f - up);
    }
}