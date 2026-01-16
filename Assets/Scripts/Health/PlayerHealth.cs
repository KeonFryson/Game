using System.Collections;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    [Header("Health Settings")]
    [SerializeField] protected float maxHealth = 100;
    [SerializeField]protected float currentHealth;
    [SerializeField] protected float deathAnimationDuration = 1f;
    protected Rigidbody rb;
    protected bool isDead;

    void Awake()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody>();

    }


    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            Die();
        }
    }


    protected virtual void Die()
    {
        isDead = true;
        rb.linearVelocity = Vector3.zero;
        //animator.SetBool("isDead", true);
        StartCoroutine(DeathCoroutine());
    }

    protected virtual IEnumerator DeathCoroutine()
    {
        yield return new WaitForSeconds(deathAnimationDuration);
        Destroy(gameObject);
    }


    public float GetCurrentHealth()
    {
        return currentHealth;
    }

    public float GetMaxHealth()
    {
        return maxHealth;
    }

    public bool IsDead()
    {
        return isDead;
    }

    public void Heal(float amount)
    {
        if (isDead) return;
        currentHealth += amount;
        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }
    }

    public void Revive()
    {
        if (!isDead) return;
        isDead = false;
        currentHealth = maxHealth;
        //animator.SetBool("isDead", false);
    }

    public void AddMaxHealth(float newMaxHealth)
    {
        maxHealth += newMaxHealth;
        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }
    }

}
