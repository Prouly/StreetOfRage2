using System.Collections;
using UnityEngine;

/// <summary>
/// EnemyBossBarbon — Boss final
/// Más HP, más daño, ataque con área (golpe al suelo).
/// Sobreescribe AttackRoutine para el ataque especial de boss.
/// </summary>
public class EnemyBossBarbon : EnemyBase
{
    [Header("Boss — Ataque especial")]
    [Tooltip("Radio del golpe al suelo")]
    public float groundSlamRadius  = 2f;
    [Tooltip("Layer del jugador para el slam")]
    public UnityEngine.LayerMask playerLayer;

    // Trigger extra para el ataque especial del boss
    private static readonly int AnimSlam = UnityEngine.Animator.StringToHash("Slam");

    protected override void Awake()
    {
        maxHP          = 20;
        moveSpeed      = 1.8f;
        damage         = 3;
        attackRange    = 1.5f;
        attackCooldown = 2f;
        knockbackDistance = 1f;

        base.Awake();
    }

    // El boss alterna entre Punch normal y Slam especial
    private int attackCount = 0;

    protected override IEnumerator AttackRoutine()
    {
        isAttacking = true;
        attackTimer = attackCooldown;
        rb.linearVelocity = UnityEngine.Vector2.zero;

        attackCount++;

        // Cada 3 golpes usa el Slam
        if (attackCount % 3 == 0)
            yield return StartCoroutine(SlamRoutine());
        else
            yield return StartCoroutine(PunchRoutine());

        isAttacking = false;
    }

    private IEnumerator PunchRoutine()
    {
        animator.SetTrigger(AnimPunch);
        yield return new UnityEngine.WaitForSeconds(attackHitboxDelay);

        if (enemyHitbox != null)
        {
            enemyHitbox.SetActive(true);
            yield return new UnityEngine.WaitForSeconds(attackHitboxDuration);
            enemyHitbox.SetActive(false);
        }
    }

    private IEnumerator SlamRoutine()
    {
        animator.SetTrigger(AnimSlam);
        yield return new UnityEngine.WaitForSeconds(0.4f);

        // Daño en área alrededor del boss
        UnityEngine.Collider2D hit = UnityEngine.Physics2D.OverlapCircle(
            transform.position, groundSlamRadius, playerLayer);

        if (hit != null)
        {
            PlayerController pc = hit.GetComponent<PlayerController>();
            if (pc != null) pc.TriggerHurt();
        }

        yield return new UnityEngine.WaitForSeconds(0.3f);
    }
}
