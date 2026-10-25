// Shared by ordinary enemies and bosses. One target can own several colliders.
public interface ICombatTarget
{
    int CurrentHealth { get; }
    bool CanBeHit { get; }
    void TakeDamage(int damage);
}
