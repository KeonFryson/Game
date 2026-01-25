using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Enemy : MonoBehaviour
{
    [Header("Detection Settings")]
    [SerializeField] protected float detectionRange = 10f;
    [SerializeField] protected float detectionAngle = 45f;
    [SerializeField] protected LayerMask obstacleLayers;

    [Header("Movement Settings")]
    [SerializeField] protected float moveSpeed = 4.5f;
    [SerializeField] protected float rotationSpeed = 8f;
    [SerializeField] protected float stopDistance = 0.5f;
    [SerializeField] protected float waypointReachedDistance = 1f;

    protected Rigidbody rb;

    [Header("Pathfinding Settings")]
    [SerializeField] protected float pathUpdateInterval = 0.5f;
    [SerializeField] protected bool usePathfinding = true;
    protected float pathUpdateTimer;
    protected List<Vector3> currentPath;
    protected int currentWaypointIndex;

    [Header("Random Walking Settings")]
    [SerializeField] protected float randomWalkSpeed = 2f;
    [SerializeField] protected float randomWalkRadius = 5f;
    [SerializeField] protected float idleTimeMin = 1f;
    [SerializeField] protected float idleTimeMax = 3f;
    protected Vector3 randomWalkTarget;
    protected bool isWalking;
    protected float idleTimer;

    [Header("Health Settings")]
    [SerializeField] protected float maxHealth = 100;
    protected float currentHealth;
    [SerializeField] protected float deathAnimationDuration = 1f;

    [Header("AI Settings")]
    [SerializeField] protected bool isAIEnabled = true;
    [SerializeField] protected float playerLostTimeout = 60f;

    [Header("References")]
    [SerializeField] protected Transform detectionTransform;

    protected Transform player;
    protected bool playerDetected;
    protected bool playerInPursuitMode;
    protected Vector3 lastKnownPlayerPosition;
    protected bool hasLastKnownPosition;
    protected float timeSincePlayerSeen;
    protected Animator animator;
    protected bool isDead;

    [SerializeField] protected bool isDebugging = false;

    protected float detectionRangeSqr;
    protected float waypointReachedDistanceSqr;
    protected float cosDetectionAngle;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody>();

        rb.freezeRotation = true;
        rb.linearDamping = 0f;
        rb.angularDamping = 0f;
        rb.interpolation = RigidbodyInterpolation.None;
        rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
        rb.mass = 1f;

        animator = GetComponent<Animator>();

        if (detectionTransform == null)
        {
            GameObject obj = new GameObject("DetectionOrigin");
            obj.transform.SetParent(transform);
            obj.transform.localPosition = Vector3.zero;
            detectionTransform = obj.transform;
        }

        detectionRangeSqr = detectionRange * detectionRange;
        waypointReachedDistanceSqr = waypointReachedDistance * waypointReachedDistance;
        cosDetectionAngle = Mathf.Cos(detectionAngle * Mathf.Deg2Rad);
    }

    protected virtual void Start()
    {
        currentHealth = maxHealth;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;

        SetNewRandomWalkTarget();
    }

    protected virtual void Update()
    {
        if (!isAIEnabled || isDead || player == null) return;

        playerDetected = IsPlayerInDetectionCone();

        if (playerDetected)
        {
            playerInPursuitMode = true;
            lastKnownPlayerPosition = player.position;
            hasLastKnownPosition = true;
            timeSincePlayerSeen = 0f;
        }
        else if (playerInPursuitMode)
        {
            playerInPursuitMode = false;
        }

        if (!playerDetected && hasLastKnownPosition)
        {
            timeSincePlayerSeen += Time.deltaTime;

            if (timeSincePlayerSeen >= playerLostTimeout)
            {
                hasLastKnownPosition = false;
                timeSincePlayerSeen = 0f;
                SetNewRandomWalkTarget();
            }
        }

        if (usePathfinding && AStarPathfinder.Instance != null)
        {
            pathUpdateTimer += Time.deltaTime;
            if (pathUpdateTimer >= pathUpdateInterval)
            {
                pathUpdateTimer = 0f;
                UpdatePath();
            }
        }

        if (!playerDetected && !hasLastKnownPosition)
        {
            UpdateRandomWalk();
        }
    }

    protected virtual void FixedUpdate()
    {
        if (!isAIEnabled || isDead)
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }

        if (usePathfinding && currentPath != null && currentPath.Count > 0)
        {
            FollowPath();
        }
        else
        {
            FallbackMovement();
        }
    }

    protected virtual bool IsPlayerInDetectionCone()
    {
        Vector3 direction = player.position - transform.position;
        float distSqr = direction.sqrMagnitude;

        if (distSqr > detectionRangeSqr) return false;

        float dist = Mathf.Sqrt(distSqr);
        direction /= dist;

        if (Vector3.Dot(detectionTransform.forward, direction) < cosDetectionAngle)
            return false;

        if (Physics.Raycast(transform.position, direction, dist, obstacleLayers))
            return false;

        return true;
    }

    protected virtual void MoveTowardsPosition(Vector3 target, float speed = -1f)
    {
        if (speed < 0) speed = moveSpeed;

        Vector3 dir = target - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.001f)
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }

        dir.Normalize();
        rb.linearVelocity = dir * speed;

        Quaternion lookRot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            lookRot,
            rotationSpeed * Time.fixedDeltaTime
        );
    }

    protected virtual void FollowPath()
    {
        if (currentWaypointIndex >= currentPath.Count)
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }

        while (currentWaypointIndex < currentPath.Count &&
               (transform.position - currentPath[currentWaypointIndex]).sqrMagnitude
               <= waypointReachedDistanceSqr * 4f)
        {
            currentWaypointIndex++;
        }

        if (currentWaypointIndex >= currentPath.Count)
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }

        int lookAheadIndex = Mathf.Min(currentWaypointIndex + 1, currentPath.Count - 1);
        Vector3 moveTarget = currentPath[lookAheadIndex];

        MoveTowardsPosition(moveTarget);
    }

    protected virtual void FallbackMovement()
    {
        if (playerDetected)
            MoveTowardsPosition(player.position);
        else if (hasLastKnownPosition)
            MoveTowardsPosition(lastKnownPlayerPosition);
        else if (isWalking)
            MoveTowardsPosition(randomWalkTarget, randomWalkSpeed);
        else
            rb.linearVelocity = Vector3.zero;
    }

    protected virtual void SetNewRandomWalkTarget()
    {
        Vector3 random = Random.insideUnitSphere * randomWalkRadius;
        random.y = 0f;
        randomWalkTarget = transform.position + random;
        isWalking = true;
    }

    protected virtual void UpdateRandomWalk()
    {
        if ((transform.position - randomWalkTarget).sqrMagnitude <= waypointReachedDistanceSqr)
        {
            isWalking = false;
            idleTimer = Random.Range(idleTimeMin, idleTimeMax);
        }

        if (!isWalking)
        {
            idleTimer -= Time.deltaTime;
            if (idleTimer <= 0f)
                SetNewRandomWalkTarget();
        }
    }

    protected virtual void UpdatePath()
    {
        Vector3 target;

        if (playerDetected)
            target = player.position;
        else if (hasLastKnownPosition)
            target = lastKnownPlayerPosition;
        else
        {
            currentPath = null;
            return;
        }

        List<Vector3> newPath =
            AStarPathfinder.Instance.FindPath(transform.position, target);

        if (newPath != null && newPath.Count > 0)
        {
            currentPath = newPath;
            currentWaypointIndex = 0;
        }
    }

    public virtual void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        if (currentHealth <= 0)
            Die();
    }

    protected virtual void Die()
    {
        isDead = true;
        rb.linearVelocity = Vector3.zero;
        StartCoroutine(DeathCoroutine());
    }

    protected virtual IEnumerator DeathCoroutine()
    {
        yield return new WaitForSeconds(deathAnimationDuration);
        Destroy(gameObject);
    }

    protected virtual void OnDrawGizmos()
    {
        if (!isDebugging) return;

        Transform gizmoTransform = detectionTransform != null ? detectionTransform : transform;

        Gizmos.color = playerDetected ? Color.red : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Vector3 forward = gizmoTransform.forward;

        Vector3 leftBoundary =
            Quaternion.Euler(0f, detectionAngle, 0f) * forward * detectionRange;
        Vector3 rightBoundary =
            Quaternion.Euler(0f, -detectionAngle, 0f) * forward * detectionRange;

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + leftBoundary);
        Gizmos.DrawLine(transform.position, transform.position + rightBoundary);

        int segments = 24;
        Vector3 prevPoint = transform.position + leftBoundary;

        for (int i = 1; i <= segments; i++)
        {
            float angle = Mathf.Lerp(-detectionAngle, detectionAngle, i / (float)segments);
            Vector3 point =
                transform.position +
                (Quaternion.Euler(0f, angle, 0f) * forward * detectionRange);

            Gizmos.DrawLine(prevPoint, point);
            prevPoint = point;
        }

        if (Application.isPlaying && player != null)
        {
            Vector3 dir = player.position - transform.position;
            float dist = dir.magnitude;

            if (dist <= detectionRange)
            {
                if (Physics.Raycast(transform.position, dir.normalized, out RaycastHit hit, dist, obstacleLayers))
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawLine(transform.position, hit.point);
                }
                else
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawLine(transform.position, player.position);
                }
            }
        }

        if (hasLastKnownPosition && Application.isPlaying)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(lastKnownPlayerPosition, 0.4f);
            Gizmos.DrawLine(transform.position, lastKnownPlayerPosition);
        }

        if (Application.isPlaying && currentPath != null && currentPath.Count > 0)
        {
            Gizmos.color = Color.cyan;

            for (int i = 0; i < currentPath.Count - 1; i++)
            {
                Gizmos.DrawLine(currentPath[i], currentPath[i + 1]);
                Gizmos.DrawWireSphere(currentPath[i], 0.2f);
            }

            Gizmos.DrawWireSphere(currentPath[currentPath.Count - 1], 0.2f);

            if (currentWaypointIndex < currentPath.Count)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(currentPath[currentWaypointIndex], 0.3f);
            }
        }

        if (Application.isPlaying && !playerDetected && !hasLastKnownPosition)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(randomWalkTarget, 0.3f);
            Gizmos.DrawLine(transform.position, randomWalkTarget);
        }
    }
}
