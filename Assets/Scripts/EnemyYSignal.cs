using System.Collections;
using UnityEngine;

/// <summary>
/// EnemyYSignal — Rápido, ataque especial con dash hacia el jugador
/// Sobreescribe AttackRoutine para añadir el dash.
/// </summary>
public class EnemyYSignal : EnemyBase
{
    [Header("Y.Signal — Ataque especial")]
    public float dashSpeed    = 8f;
    public float dashDuration = 0.15f;

    protected override void Awake()
    {
        maxHP      = 3;
        moveSpeed  = 3f;
        damage     = 1;
        attackRange = 2f;
        attackCooldown = 1.2f;

        base.Awake();
    }

    protected override IEnumerator AttackRoutine()
    {
        isAttacking = true;
        attackTimer = attackCooldown;
        rb.linearVelocity = Vector2.zero;

        animator.SetTrigger(AnimPunch);

        // Dash hacia el jugador antes del golpe
        if (player != null)
        {
            float dir     = player.position.x > transform.position.x ? 1f : -1f;
            float elapsed = 0f;
            while (elapsed < dashDuration)
            {
                elapsed += Time.deltaTime;
                rb.linearVelocity = new Vector2(dir * dashSpeed, 0f);
                yield return null;
            }
            rb.linearVelocity = Vector2.zero;
        }

        yield return new WaitForSeconds(attackHitboxDelay);

        if (enemyHitbox != null)
        {
            enemyHitbox.SetActive(true);
            yield return new WaitForSeconds(attackHitboxDuration);
            enemyHitbox.SetActive(false);
        }

        isAttacking = false;
    }
}
