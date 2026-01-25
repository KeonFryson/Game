using UnityEngine;

public class Spray_Imp : Enemy
{
    [Header("Combat Settings")]
    [SerializeField] private ItemData sprayBulletSpell;
    [SerializeField] private GameObject deathBurstSpell;
    [SerializeField] private Transform spellCastPoint;
    [SerializeField] private SpellCastingManager spellCastingManager;

    [Header("Attack Settings")]
    [SerializeField] private float attackRange = 12f;
    [SerializeField] private float minAttackRange = 6f;
    [SerializeField] private float attackCooldown = 0.3f;
    [SerializeField] private int sprayBulletsPerBurst = 3;
    [SerializeField] private float sprayAngle = 15f;

    [Header("Flight Settings")]
    [SerializeField] private float hoverHeight = 3f;
    [SerializeField] private float hoverSpeed = 5f;
    [SerializeField] private float hoverBobAmount = 0.3f;
    [SerializeField] private float hoverBobSpeed = 2f;

    [Header("Strafe Settings")]
    [SerializeField] private float strafeSpeed = 6f;
    [SerializeField] private float strafeRadius = 8f;
    [SerializeField] private float repositionInterval = 3f;
    [SerializeField] private float repositionDuration = 1f;

    [Header("Death Burst Settings")]
    [SerializeField] private int deathBurstCount = 8;
    [SerializeField] private float deathBurstGlowDuration = 0.5f;
    [SerializeField] private Material glowMaterial;

    [Header("Visual Effects")]
    [SerializeField] private ParticleSystem firingEffect;
    [SerializeField] private Light glowLight;

    private float lastAttackTime;
    private float lastRepositionTime;
    private bool isRepositioning;
    private float repositionTimer;
    private Vector3 targetStrafePosition;
    private float strafeAngle;
    private Material originalMaterial;
    private Renderer enemyRenderer;
    private bool isGlowing;
    private float hoverBobTimer;

    protected override void Awake()
    {
        base.Awake();

        // Disable gravity for flying
        rb.useGravity = false;

        // Flying enemies don't need pathfinding - they can move directly through air
        usePathfinding = false;
    }

    protected override void Start()
    {
        base.Start();

        if (spellCastingManager == null)
        {
            spellCastingManager = GetComponent<SpellCastingManager>();
            if (spellCastingManager == null)
            {
                Debug.LogWarning($"{gameObject.name}: Missing SpellCastingManager component!");
            }
        }

        if (spellCastPoint == null)
        {
            GameObject spawnObj = new GameObject("SpellCastPoint");
            spawnObj.transform.SetParent(transform);
            spawnObj.transform.localPosition = new Vector3(0, 0.5f, 0.5f);
            spellCastPoint = spawnObj.transform;
        }

        enemyRenderer = GetComponentInChildren<Renderer>();
        if (enemyRenderer != null)
        {
            originalMaterial = enemyRenderer.material;
        }

        if (glowLight != null)
        {
            glowLight.enabled = false;
        }

        // Start at random angle around player
        strafeAngle = Random.Range(0f, 360f);
        CalculateNextStrafePosition();

        // Set initial height
        Vector3 pos = transform.position;
        pos.y = hoverHeight;
        transform.position = pos;
    }

    protected override void Update()
    {
        base.Update();

        if (!isAIEnabled || isDead || player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // Update hover bob timer
        hoverBobTimer += Time.deltaTime * hoverBobSpeed;

        // Handle repositioning
        if (isRepositioning)
        {
            repositionTimer -= Time.deltaTime;
            if (repositionTimer <= 0f)
            {
                isRepositioning = false;
                CalculateNextStrafePosition();
            }
        }
        else
        {
            // Check if we need to reposition
            lastRepositionTime += Time.deltaTime;
            if (lastRepositionTime >= repositionInterval)
            {
                StartRepositioning();
            }

            // Fire at player if in range
            if (distanceToPlayer <= attackRange && distanceToPlayer >= minAttackRange)
            {
                if (Time.time - lastAttackTime >= attackCooldown)
                {
                    FireSprayBurst();
                }
            }
        }
    }

    protected override void FixedUpdate()
    {
        if (!isAIEnabled || isDead)
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }

        if (player == null) return;

        if (isRepositioning)
        {
            // Fly directly to reposition point (no pathfinding needed)
            MoveTowardsPositionFlying(targetStrafePosition, moveSpeed);
        }
        else
        {
            // Strafe around player
            StrafeAroundPlayer();
        }

        // Maintain hover height with bobbing
        MaintainHoverHeight();
    }

    private void MaintainHoverHeight()
    {
        float targetHeight = hoverHeight + Mathf.Sin(hoverBobTimer) * hoverBobAmount;
        float currentY = transform.position.y;
        float yVelocity = (targetHeight - currentY) * hoverSpeed;

        Vector3 velocity = rb.linearVelocity;
        velocity.y = yVelocity;
        rb.linearVelocity = velocity;
    }

    private void StrafeAroundPlayer()
    {
        // Update strafe angle for circular motion
        strafeAngle += (strafeSpeed / strafeRadius) * Time.fixedDeltaTime * Mathf.Rad2Deg;
        if (strafeAngle >= 360f) strafeAngle -= 360f;

        // Calculate strafe position at hover height
        Vector3 offset = new Vector3(
            Mathf.Cos(strafeAngle * Mathf.Deg2Rad) * strafeRadius,
            0f,
            Mathf.Sin(strafeAngle * Mathf.Deg2Rad) * strafeRadius
        );

        Vector3 targetPosition = player.position + offset;

        // Move towards strafe position (Y handled by MaintainHoverHeight)
        Vector3 direction = (targetPosition - transform.position);
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.001f)
        {
            direction.Normalize();
            Vector3 velocity = rb.linearVelocity;
            velocity.x = direction.x * strafeSpeed;
            velocity.z = direction.z * strafeSpeed;
            rb.linearVelocity = velocity;

            // Face the player while strafing (including vertical tilt)
            Vector3 lookDir = player.position - transform.position;
            if (lookDir.sqrMagnitude > 0.001f)
            {
                Quaternion lookRot = Quaternion.LookRotation(lookDir);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    lookRot,
                    rotationSpeed * Time.fixedDeltaTime
                );
            }
        }
    }

    private void MoveTowardsPositionFlying(Vector3 target, float speed)
    {
        Vector3 dir = target - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.001f)
        {
            Vector3 velocity = rb.linearVelocity;
            velocity.x = 0f;
            velocity.z = 0f;
            rb.linearVelocity = velocity;
            return;
        }

        dir.Normalize();
        Vector3 newVelocity = rb.linearVelocity;
        newVelocity.x = dir.x * speed;
        newVelocity.z = dir.z * speed;
        rb.linearVelocity = newVelocity;

        // Look at player with vertical rotation during repositioning
        Vector3 lookDir = player.position - transform.position;
        if (lookDir.sqrMagnitude > 0.001f)
        {
            Quaternion lookRot = Quaternion.LookRotation(lookDir);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                lookRot,
                rotationSpeed * Time.fixedDeltaTime
            );
        }
    }

    private void CalculateNextStrafePosition()
    {
        // Pick a new random angle around the player
        strafeAngle = Random.Range(0f, 360f);
    }

    private void StartRepositioning()
    {
        isRepositioning = true;
        repositionTimer = repositionDuration;
        lastRepositionTime = 0f;

        // Pick a new position at random angle and distance
        float randomAngle = Random.Range(0f, 360f);
        float randomDistance = Random.Range(minAttackRange, attackRange);

        Vector3 offset = new Vector3(
            Mathf.Cos(randomAngle * Mathf.Deg2Rad) * randomDistance,
            0f,
            Mathf.Sin(randomAngle * Mathf.Deg2Rad) * randomDistance
        );

        targetStrafePosition = player.position + offset;
    }

    private void FireSprayBurst()
    {
        if (spellCastingManager == null || spellCastPoint == null || sprayBulletSpell == null)
        {
            Debug.LogWarning($"{gameObject.name}: Missing spray bullet spell or components!");
            return;
        }

        lastAttackTime = Time.time;

        // Calculate base direction to player (including vertical aim)
        Vector3 directionToPlayer = (player.position - spellCastPoint.position).normalized;

        // Fire spread of bullets
        float angleStep = sprayBulletsPerBurst > 1 ? sprayAngle / (sprayBulletsPerBurst - 1) : 0f;
        float startAngle = -sprayAngle / 2f;

        for (int i = 0; i < sprayBulletsPerBurst; i++)
        {
            float currentAngle = startAngle + (angleStep * i);

            // Get the right vector relative to the aim direction
            Vector3 upVector = Vector3.up;
            Vector3 rightVector = Vector3.Cross(upVector, directionToPlayer).normalized;

            // If direction is too vertical, use forward as reference instead
            if (rightVector.sqrMagnitude < 0.001f)
            {
                rightVector = Vector3.Cross(Vector3.forward, directionToPlayer).normalized;
            }

            // Create rotation around the right vector for horizontal spread
            Quaternion spreadRotation = Quaternion.AngleAxis(currentAngle, rightVector);
            Vector3 spreadDirection = spreadRotation * directionToPlayer;

            // Create temporary transform for cast point with correct rotation
            GameObject tempCastPoint = new GameObject("TempCastPoint");
            tempCastPoint.transform.position = spellCastPoint.position;
            tempCastPoint.transform.rotation = Quaternion.LookRotation(spreadDirection);

            // Cast the spell
            spellCastingManager.CastSpell(sprayBulletSpell, tempCastPoint.transform);

            // Clean up
            Destroy(tempCastPoint);
        }

        // Visual feedback
        if (firingEffect != null)
        {
            firingEffect.Play();
        }

        if (glowLight != null && !isGlowing)
        {
            glowLight.enabled = true;
            Invoke(nameof(DisableGlow), 0.1f);
        }
    }

    private void DisableGlow()
    {
        if (glowLight != null)
        {
            glowLight.enabled = false;
        }
    }

    protected override void Die()
    {
        if (isDead) return;

        isDead = true;
        rb.linearVelocity = Vector3.zero;

        // Start death glow warning
        if (glowMaterial != null && enemyRenderer != null)
        {
            enemyRenderer.material = glowMaterial;
            isGlowing = true;
        }

        if (glowLight != null)
        {
            glowLight.enabled = true;
        }

        // Delay burst to give warning
        Invoke(nameof(FireDeathBurst), deathBurstGlowDuration);
    }

    private void FireDeathBurst()
    {
        if (spellCastingManager == null || deathBurstSpell == null)
        {
            DestroyImmediate();
            return;
        }

        // Fire spiral burst
        float angleStep = 360f / deathBurstCount;
        Vector3 burstOrigin = transform.position + Vector3.up;

        for (int i = 0; i < deathBurstCount; i++)
        {
            float angle = i * angleStep;
            Vector3 direction = new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad),
                0f,
                Mathf.Sin(angle * Mathf.Deg2Rad)
            );

            // Create temporary transform for cast point with correct rotation
            GameObject tempCastPoint = new GameObject("TempCastPoint");
            tempCastPoint.transform.position = burstOrigin;
            tempCastPoint.transform.rotation = Quaternion.LookRotation(direction);

            // Cast the spell
            //Instantiate<GameObject>(deathBurstSpell, tempCastPoint.transform.position, tempCastPoint.transform.rotation);

            // Clean up
            Destroy(tempCastPoint);
        }

        Instantiate<GameObject>(deathBurstSpell,  transform.position,  transform.rotation);

        DestroyImmediate();
    }

    private void DestroyImmediate()
    {
        Destroy(gameObject);
    }

    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();

        if (!isDebugging) return;

        // Draw attack range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Draw min attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, minAttackRange);

        // Draw strafe radius when playing
        if (Application.isPlaying && player != null)
        {
            // Draw hover height plane
            Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
            Vector3 hoverCenter = player.position;
            hoverCenter.y = hoverHeight;
            Gizmos.DrawWireSphere(hoverCenter, strafeRadius);

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(player.position, strafeRadius);

            // Draw current strafe position
            Vector3 offset = new Vector3(
                Mathf.Cos(strafeAngle * Mathf.Deg2Rad) * strafeRadius,
                0f,
                Mathf.Sin(strafeAngle * Mathf.Deg2Rad) * strafeRadius
            );
            Vector3 strafePos = player.position + offset;
            strafePos.y = hoverHeight;
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(strafePos, 0.5f);

            // Draw reposition target
            if (isRepositioning)
            {
                Gizmos.color = Color.magenta;
                Vector3 targetPos = targetStrafePosition;
                targetPos.y = hoverHeight;
                Gizmos.DrawSphere(targetPos, 0.5f);
                Gizmos.DrawLine(transform.position, targetPos);
            }

            // Draw spray cone
            if (spellCastPoint != null && !isRepositioning)
            {
                Vector3 directionToPlayer = (player.position - spellCastPoint.position).normalized;
                float angleStep = sprayBulletsPerBurst > 1 ? sprayAngle / (sprayBulletsPerBurst - 1) : 0f;
                float startAngle = -sprayAngle / 2f;

                Gizmos.color = Color.red;
                for (int i = 0; i < sprayBulletsPerBurst; i++)
                {
                    float currentAngle = startAngle + (angleStep * i);

                    Vector3 upVector = Vector3.up;
                    Vector3 rightVector = Vector3.Cross(upVector, directionToPlayer).normalized;

                    if (rightVector.sqrMagnitude < 0.001f)
                    {
                        rightVector = Vector3.Cross(Vector3.forward, directionToPlayer).normalized;
                    }

                    Quaternion spreadRotation = Quaternion.AngleAxis(currentAngle, rightVector);
                    Vector3 spreadDirection = spreadRotation * directionToPlayer;

                    Gizmos.DrawLine(spellCastPoint.position, spellCastPoint.position + spreadDirection * 3f);
                }
            }
        }
    }
}