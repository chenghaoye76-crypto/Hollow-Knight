using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 4;
    private int currentHealth;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public event System.Action<int, int> Changed;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        if (damage <= 0 || currentHealth <= 0)
            return;

        var spells = GetComponent<KnightSpells>();
        if (spells != null && spells.IsInvulnerable) return;
        int before = currentHealth;
        currentHealth = Mathf.Max(0, currentHealth - damage);
        Changed?.Invoke(before, currentHealth);
        GetComponent<KnightAudio>()?.Hurt(currentHealth == 0);
        OptionsRuntime.Shake();

        if (currentHealth == 0)
        {
            Die();
        }
    }

    public void Heal(int amount)
    {
        if (amount <= 0 || currentHealth <= 0)
            return;

        int before = currentHealth;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        if (before != currentHealth) Changed?.Invoke(before, currentHealth);
    }

    private void Die()
    {
        // 播放死亡动画后重启场景：触发 PlayerController 的死亡流程（若存在）
        PlayerController pc = GetComponent<PlayerController>();
        if (pc != null)
        {
            pc.HandleDeath(() => SaveGameManager.Instance.Respawn());
        }
        else
        {
            // 回退：直接重载场景
            SaveGameManager.Instance.Respawn();
        }
    }
}
