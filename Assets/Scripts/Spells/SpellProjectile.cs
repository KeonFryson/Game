using UnityEngine;

public class SpellProjectile : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed = 10f;
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private bool useGravity = false;

    [Header("Damage")]
    [SerializeField] private float baseDamage = 10f;
    private float damageMultiplier = 1f;

    [Header("Effects")]
    [SerializeField] private GameObject impactEffect;

    [Header("Homing (Optional)")]
    [SerializeField] private bool isHoming = false;
    [SerializeField] private float homingStrength = 5f;
    [SerializeField] private Transform target;

    private Vector3 direction;
    private Rigidbody rb;
    private float spawnTime;
    private bool isInitialized = false;

    [Header("Rotation")]
    [SerializeField] private Vector3 rotationOffset = new Vector3(0, 90, 0);

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;



    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        rb.useGravity = useGravity;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        spawnTime = Time.time;
    }

    public void Initialize(Vector3 direction, float speed, float channeltime, float channelPercent = 1f)
    {
        this.direction = direction;
        this.speed = speed;
        this.damageMultiplier = channelPercent;
        lifetime += channeltime; // Extend lifetime by channel time

        // Set initial rotation with rotation offset
        transform.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(rotationOffset);

        if (debugLogs)
            Debug.Log($"SpellProjectile initialized with direction: {this.direction}, speed: {speed}, channelPercent: {channelPercent}");

        // Ensure rb exists (in case Initialize is called before Awake)
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
                rb.useGravity = useGravity;
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            }
        }

        if (rb != null)
        {
            rb.linearVelocity = this.direction * this.speed;
            isInitialized = true;
        }
        else
        {
            if (debugLogs)
                Debug.LogError("SpellProjectile: Rigidbody is null after initialization attempt!");
        }
    }

    public void SetTarget(Transform target)
    {
        this.target = target;
        isHoming = true;
    }

    public float GetDamage()
    {
        return baseDamage * damageMultiplier;
    }

    private void FixedUpdate()
    {
        // Don't update physics if not initialized
        if (!isInitialized || rb == null)
        {
            return;
        }

        // Check lifetime
        if (Time.time - spawnTime >= lifetime)
        {
            Destroy(gameObject);
            return;
        }

        // Homing behavior
        if (isHoming && target != null)
        {
            Vector3 targetDirection = (target.position - transform.position).normalized;
            Vector3 newDirection = Vector3.Lerp(rb.linearVelocity.normalized, targetDirection, homingStrength * Time.fixedDeltaTime);
            rb.linearVelocity = newDirection * speed;

            // Update rotation to face the new direction with rotation offset
            transform.rotation = Quaternion.LookRotation(newDirection) * Quaternion.Euler(rotationOffset);
        }
        else if (!useGravity)
        {
            // Maintain constant velocity if not using gravity
            rb.linearVelocity = direction * speed;
            transform.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(rotationOffset);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Handle collision (damage, effects, etc.)
        float finalDamage = GetDamage();
        if (debugLogs)
            Debug.Log($"Spell hit: {collision.gameObject.name} for {finalDamage:F1} damage ({damageMultiplier * 100f:F0}% power)");

        bool hitTarget = false;

        Enemy enemy = collision.gameObject.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.TakeDamage(finalDamage);
            hitTarget = true;
        }

        PlayerHealth playerHealth = collision.gameObject.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(finalDamage);
            hitTarget = true;
        }

        transform.position = collision.contacts[0].point;

        if (!hitTarget && impactEffect != null)
        {
            Instantiate(impactEffect, transform.position, Quaternion.identity);
        }

        // Destroy the projectile on impact
        Destroy(gameObject);
    }
}