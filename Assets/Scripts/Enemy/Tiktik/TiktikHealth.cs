using UnityEngine;

public class TiktikHealth : MonoBehaviour, ICombatTarget
{
    [SerializeField] private int maxHealth = 8;
    private int currentHealth;

    public int CurrentHealth => currentHealth;
    public bool CanBeHit => currentHealth > 0;
    private Animator animator;

    private void Awake()
    {
        currentHealth = maxHealth;
        animator = GetComponent<Animator>();
    }

    public void TakeDamage(int damage)
    {
        if (damage <= 0 || currentHealth <= 0)
        {
            return;
        }

        currentHealth = Mathf.Max(0, currentHealth - damage);
        if (currentHealth == 0)
        {
            GetComponent<EnemyController>()?.StopMoving();
            animator.SetTrigger("Die");
        }
    }

    private void Dead()
    {
        Destroy(gameObject);
    }
}
