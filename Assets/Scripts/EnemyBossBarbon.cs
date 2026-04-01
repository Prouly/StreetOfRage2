using System.Collections;
using UnityEngine;

/// EnemyBossBarbon — Boss final Stage 1
///
/// SETUP:
///   - Añadir script + EnemyHitbox hijo
///   - Asignar bossMusic en Inspector
///   - En Animator: triggers Punch, Kick, HighKick, Hit, Floor
///
/// ANIMATOR — nuevos triggers:
///   Trigger → Kick
///   Trigger → HighKick

public class EnemyBossBarbon : EnemyBase
{
    [Header("Boss — Música")]
    [Tooltip("Música que suena al aparecer Barbon")]
    [SerializeField] private AudioClip bossMusic;

    [Header("Boss — Ataques")]
    [SerializeField] private float kickCooldown     = 2f;
    [SerializeField] private float highKickCooldown = 3f;

    // Hashes adicionales
    private static readonly int AnimKick     = Animator.StringToHash("Kick");
    private static readonly int AnimHighKick = Animator.StringToHash("HighKick");

    // Contadores para variar ataques
    private int attackSequenceIndex = 0;
    private static readonly int[] attackSequence = { 0, 1, 0, 2, 0, 1, 2, 0 };
    // 0=Punch, 1=Kick, 2=HighKick — aleatorizados con variación

    private bool musicStarted = false;

    protected override void Awake()
    {
        maxHP          = 200;
        moveSpeed      = 1.8f;
        damage         = 3;
        attackRange    = 1.5f;
        attackCooldown = 1.8f;
        detectionRange = 9f;
        knockbackDistance = 1f;
        base.Awake();
    }

    protected override void Update()
    {
        // Inicia música la primera vez que detecta al player
        if (!musicStarted && player != null)
        {
            float dist = Vector2.Distance(transform.position, player.position);
            if (dist <= detectionRange)
            {
                musicStarted = true;
                if (bossMusic != null)
                    AudioManager.Instance?.PlayMusic(bossMusic);
            }
        }

        base.Update();
    }

    protected override IEnumerator AttackRoutine()
    {
        isAttacking = true;

        // Cooldown con variación aleatoria (±0.3s) para evitar ataques sincronizados
        attackTimer = attackCooldown + Random.Range(-0.3f, 0.5f);

        rb.linearVelocity = Vector2.zero;

        // Selecciona el siguiente ataque de la secuencia + variación aleatoria
        int attackType;
        float roll = Random.value;

        if (roll < 0.45f)
            attackType = 0; // Punch — más frecuente
        else if (roll < 0.75f)
            attackType = 1; // Kick
        else
            attackType = 2; // HighKick — menos frecuente

        switch (attackType)
        {
            case 0: yield return StartCoroutine(PunchRoutine()); break;
            case 1: yield return StartCoroutine(KickRoutine());  break;
            case 2: yield return StartCoroutine(HighKickRoutine()); break;
        }

        isAttacking = false;
    }

    private IEnumerator PunchRoutine()
    {
        animator.SetTrigger(AnimPunch);
        yield return new WaitForSeconds(attackHitboxDelay);
        yield return StartCoroutine(ActivateHitboxBoss());
    }

    private IEnumerator KickRoutine()
    {
        animator.SetTrigger(AnimKick);
        yield return new WaitForSeconds(attackHitboxDelay + 0.1f);
        damage = 3;
        yield return StartCoroutine(ActivateHitboxBoss());
    }

    private IEnumerator HighKickRoutine()
    {
        animator.SetTrigger(AnimHighKick);
        yield return new WaitForSeconds(attackHitboxDelay + 0.15f);
        damage = 4;
        yield return StartCoroutine(ActivateHitboxBoss());
        damage = 3; // restaura daño base
    }

    private IEnumerator ActivateHitboxBoss()
    {
        if (enemyHitbox != null)
        {
            enemyHitbox.SetActive(true);
            CheckBossHitboxOverlap();
            yield return new WaitForSeconds(attackHitboxDuration);
            enemyHitbox.SetActive(false);
        }
        else
            yield return new WaitForSeconds(attackHitboxDuration);
    }

    private void CheckBossHitboxOverlap()
    {
        if (enemyHitbox == null) return;
        Collider2D col = enemyHitbox.GetComponent<Collider2D>();
        if (col == null) return;

        Collider2D[] hits = new Collider2D[4];
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(LayerMask.GetMask("Player"));
        filter.useTriggers = true;

        int count = Physics2D.OverlapCollider(col, filter, hits);
        for (int i = 0; i < count; i++)
        {
            if (hits[i] == null) continue;
            EnemyHitbox eh = enemyHitbox.GetComponent<EnemyHitbox>();
            if (eh != null) eh.ForceHit(hits[i]);
        }
    }

    // Notifica al LevelManager al morir
    protected override IEnumerator DieRoutine(float attackerX)
    {
        yield return StartCoroutine(base.DieRoutine(attackerX));

        LevelManager lm = FindFirstObjectByType<LevelManager>();
        if (lm != null) lm.OnBossDefeated();
    }
}