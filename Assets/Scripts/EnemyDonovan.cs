/// <summary>
/// EnemyDonovan — Más rápido, más agresivo, menos HP
/// </summary>
public class EnemyDonovan : EnemyBase
{
    protected override void Awake()
    {
        maxHP          = 4;
        moveSpeed      = 3.5f;
        damage         = 2;
        attackRange    = 1f;
        attackCooldown = 1f;
        detectionRange = 8f;

        base.Awake();
    }
}
