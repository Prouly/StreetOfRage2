
public class EnemyDonovan : EnemyBase
{
    protected override void Awake()
    {
        maxHP          = 25;
        moveSpeed      = 2.5f;
        damage         = 2;
        attackCooldown = 1f;

        base.Awake();
    }
}
