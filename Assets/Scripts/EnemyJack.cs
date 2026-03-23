/// <summary>
/// EnemyJack — Más resistente, más lento
/// Sobreescribe los valores por defecto en Awake.
/// </summary>
public class EnemyJack : EnemyBase
{
    protected override void Awake()
    {
        // Sobreescribe stats ANTES de que la base los use
        maxHP      = 6;
        moveSpeed  = 1.5f;
        damage     = 2;
        attackRange = 1.2f;

        base.Awake();
    }
}
