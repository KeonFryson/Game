using UnityEngine;

public class Magic_Enemy : Enemy
{
    [Header("Spell Settings")]
    [SerializeField] private ItemData[] attackSpells; // Changed to array of spells
    [SerializeField] private Transform spellCastPoint;
    [SerializeField] private SpellCastingManager spellCastingManager;
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private float attackRange = 8f;

    [Header("Spell Selection")]
    [SerializeField] private SpellSelectionMode selectionMode = SpellSelectionMode.Random;
   // [SerializeField] private bool cycleInOrder = false; // Only used if selectionMode is Cycle

    private float lastAttackTime;
    private int currentSpellIndex = 0;

    public enum SpellSelectionMode
    {
        Random,     // Pick random spell each time
        Cycle,      // Cycle through spells in order
        Weighted    // Use spell weights (if implemented in ItemData)
    }

    protected override void Start()
    {
        base.Start();

        if (spellCastingManager == null)
        {
            spellCastingManager = GetComponent<SpellCastingManager>();
        }

        // Set stop distance to attack range so enemy doesn't get too close
        stopDistance = attackRange * 0.8f; // Slightly less than attack range for buffer

        // Validate spell list
        if (attackSpells == null || attackSpells.Length == 0)
        {
            Debug.LogWarning($"{gameObject.name}: No attack spells assigned!");
        }
    }

    protected override void Update()
    {
        base.Update();

        if (playerDetected && player != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);

            if (distanceToPlayer <= attackRange && Time.time - lastAttackTime >= attackCooldown)
            {
                Attack();
            }
        }
    }

    protected override void FollowPath()
    {
        if (currentWaypointIndex >= currentPath.Count)
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }

        // Check if player is in attack range - if so, stop moving
        if (playerDetected && player != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);
            if (distanceToPlayer <= attackRange)
            {
                rb.linearVelocity = Vector3.zero;
                // Still rotate to face player
                Vector3 direction = (player.position - transform.position);
                direction.y = 0f;
                if (direction.sqrMagnitude > 0.001f)
                {
                    direction.Normalize();
                    Quaternion lookRot = Quaternion.LookRotation(direction);
                    transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, rotationSpeed * Time.fixedDeltaTime);
                }
                return;
            }
        }

        base.FollowPath();
    }

    protected override void FallbackMovement()
    {
        // Check if player is in attack range - if so, stop moving
        if (playerDetected && player != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);
            if (distanceToPlayer <= attackRange)
            {
                rb.linearVelocity = Vector3.zero;
                // Still rotate to face player
                Vector3 direction = (player.position - transform.position);
                direction.y = 0f;
                if (direction.sqrMagnitude > 0.001f)
                {
                    direction.Normalize();
                    Quaternion lookRot = Quaternion.LookRotation(direction);
                    transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, rotationSpeed * Time.fixedDeltaTime);
                }
                return;
            }
        }

        base.FallbackMovement();
    }

    private void Attack()
    {
        if (spellCastingManager == null || spellCastPoint == null)
        {
            Debug.LogWarning($"{gameObject.name}: Missing spell casting requirements!");
            return;
        }

        if (attackSpells == null || attackSpells.Length == 0)
        {
            Debug.LogWarning($"{gameObject.name}: No attack spells available!");
            return;
        }

        ItemData selectedSpell = GetNextSpell();

        if (selectedSpell != null)
        {
            // Aim the spell cast point at the player
            if (player != null)
            {
                Vector3 directionToPlayer = (player.position - spellCastPoint.position).normalized;
                spellCastPoint.rotation = Quaternion.LookRotation(directionToPlayer);
            }

            bool success = spellCastingManager.CastSpell(selectedSpell, spellCastPoint);

            if (success)
            {
                lastAttackTime = Time.time;
                Debug.Log($"{gameObject.name} cast {selectedSpell.itemName} at player");
            }
        }
        else
        {
            Debug.LogWarning($"{gameObject.name}: Failed to select a spell!");
        }
    }

    private ItemData GetNextSpell()
    {
        if (attackSpells == null || attackSpells.Length == 0)
            return null;

        switch (selectionMode)
        {
            case SpellSelectionMode.Random:
                return GetRandomSpell();

            case SpellSelectionMode.Cycle:
                return GetCycledSpell();

            case SpellSelectionMode.Weighted:
                // For now, fall back to random. Can implement weighted selection later
                return GetRandomSpell();

            default:
                return GetRandomSpell();
        }
    }

    private ItemData GetRandomSpell()
    {
        // Filter out null spells
        var validSpells = System.Array.FindAll(attackSpells, spell => spell != null && spell.isSpell);

        if (validSpells.Length == 0)
            return null;

        int randomIndex = Random.Range(0, validSpells.Length);
        return validSpells[randomIndex];
    }

    private ItemData GetCycledSpell()
    {
        // Filter out null spells
        var validSpells = System.Array.FindAll(attackSpells, spell => spell != null && spell.isSpell);

        if (validSpells.Length == 0)
            return null;

        // Wrap the index around
        currentSpellIndex = currentSpellIndex % validSpells.Length;
        ItemData spell = validSpells[currentSpellIndex];

        // Increment for next time
        currentSpellIndex++;

        return spell;
    }

    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();

        if(!isDebugging) return;
        // Draw attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}