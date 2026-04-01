using System.Collections;
using UnityEngine;

/// <summary>
/// EnemyJack — Mini-boss con dos fases: puños y cuchillo
///
/// SETUP:
///   1. Añadir al prefab Enemy_Jack (hereda de EnemyBase)
///   2. Rigidbody2D: Kinematic, Gravity Scale 0, Freeze Rotation Z
///   3. CapsuleCollider2D: Is Trigger ON, Layer: Enemy
///   4. Hijo "JackHitbox": BoxCollider2D Is Trigger ON, Layer: EnemyAttack, script EnemyHitbox
///   5. Asignar knifeThrowPrefab (el prefab KnifeThrow)
///   6. Asignar knifeSpawnPoint (Transform hijo vacío en la mano)
///   7. El GO empieza DESACTIVADO — SpawnPoint lo activa al llegar a la zona X
///
/// ANIMATOR — parámetros necesarios:
///   Bool    → isWalking
///   Bool    → isKnifeMode      (activa la fase cuchillo)
///   Trigger → Hit              (golpe recibido normal)
///   Trigger → Floor            (caída al suelo)
///   Trigger → StandUp          (levantarse)
///   Trigger → Punch            (ataque puño)
///   Trigger → KnifeOut         (sacar el cuchillo — transición de fase)
///   Trigger → KnifeAttack      (ataque cuerpo a cuerpo con cuchillo)
///   Trigger → KnifeThrow       (lanzar cuchillo)
///   Bool    → isKnifeRunning   (correr con cuchillo hacia el player)
///
/// TRANSICIONES ANIMATOR:
///   Enemy_Jack_Idle   → Enemy_Jack_Walk       : isWalking = true
///   Enemy_Jack_Walk   → Enemy_Jack_Idle       : isWalking = false
///   Any State         → Enemy_Jack_Hit        : Trigger Hit    | Has Exit Time OFF
///   Enemy_Jack_Hit    → Enemy_Jack_Idle       : Has Exit Time ON, Exit Time 1
///   Any State         → Enemy_Jack_Floor      : Trigger Floor  | Has Exit Time OFF
///   Enemy_Jack_Floor  → Enemy_Jack_StandUp    : Trigger StandUp| Has Exit Time OFF
///   Enemy_Jack_StandUp→ Enemy_Jack_Idle       : Has Exit Time ON, Exit Time 1
///   Enemy_Jack_Idle   → Enemy_Jack_Punch      : Trigger Punch  | Has Exit Time OFF
///   Enemy_Jack_Punch  → Enemy_Jack_Idle       : Has Exit Time ON, Exit Time 1
///   --- Fase cuchillo ---
///   Enemy_Jack_Idle   → Enemy_Jack_KnifeOut   : Trigger KnifeOut
///   Enemy_Jack_KnifeOut→Enemy_Jack_Knife_Idle : Has Exit Time ON, Exit Time 1
///   Enemy_Jack_Knife_Idle→Enemy_Jack_KnifeRun : isKnifeRunning = true
///   Enemy_Jack_KnifeRun→Enemy_Jack_Knife_Idle : isKnifeRunning = false
///   Any State         → Enemy_Jack_KnifeAttack: Trigger KnifeAttack | Has Exit Time OFF
///   Enemy_Jack_KnifeAttack→Enemy_Jack_Knife_Idle: Has Exit Time ON, Exit Time 1
///   Any State         → Enemy_Jack_KnifeThrow : Trigger KnifeThrow  | Has Exit Time OFF (si tienes la anim)
/// </summary>

using System.Collections;
using UnityEngine;

/// EnemyJack — Sin lanzamiento de cuchillo

public class EnemyJack : EnemyBase
{
    [Header("Jack — Fase cuchillo")]
    [SerializeField] private float knifePhaseThreshold = 0.5f;
    [SerializeField] private int   knifeMeleeDamage    = 3;

    [Header("Jack — Spawn")]
    [SerializeField] private float spawnTriggerX   = 50f;
    [SerializeField] private float spawnPositionY  = -2f;

    // ── Estado ───────────────────────────────────────────────
    private bool  isKnifeMode    = false;
    private bool  knifeOutDone   = false;
    private bool  isOnFloor      = false;

    // ── Hashes Animator ──────────────────────────────────────
    private static readonly int AnimKnifeMode    = Animator.StringToHash("isKnifeMode");
    private static readonly int AnimKnifeOut     = Animator.StringToHash("KnifeOut");
    private static readonly int AnimKnifeAttack  = Animator.StringToHash("KnifeAttack");
    private static readonly int AnimKnifeRunning = Animator.StringToHash("isKnifeRunning");
    private new static readonly int AnimStandUp  = Animator.StringToHash("StandUp");

    // ─────────────────────────────────────────────────────────

    protected override void Awake()
    {
        maxHP             = 100;
        moveSpeed         = 2f;
        damage            = 2;
        attackRange       = 1.2f;
        attackCooldown    = 1.5f;
        detectionRange    = 8f;
        knockbackDistance = 0.6f;
        base.Awake();
    }

    protected override void Update()
    {
        if (isDead) return;
        if (isOnFloor) return; // bloquea IA mientras está en el suelo

        if (!isKnifeMode && !knifeOutDone &&
            currentHP <= Mathf.RoundToInt(maxHP * knifePhaseThreshold))
        {
            StartCoroutine(KnifePhaseRoutine());
        }

        base.Update();
    }

    // ── IA según fase ─────────────────────────────────────────

    protected override void UpdateAI()
    {
        if (isKnifeMode)
            UpdateKnifeAI();
        else
            base.UpdateAI();
    }

    private void UpdateKnifeAI()
    {
        if (player == null) return;

        float dist = Vector2.Distance(transform.position, player.position);

        if (attackTimer <= 0f)
        {
            if (dist <= attackRange)
            {
                float roll = Random.value;
                if (roll < 0.65f)
                    StartCoroutine(KnifeAttackRoutine());
                else
                    StartCoroutine(KnifeRunAttackRoutine());
            }
            else if (dist <= detectionRange)
            {
                ChaseWithKnife();
            }
        }
        else if (dist <= detectionRange)
        {
            ChaseWithKnife();
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
            animator.SetBool(AnimKnifeRunning, false);
        }
    }

    private void ChaseWithKnife()
    {
        float diffX = player.position.x - transform.position.x;
        float diffY = player.position.y - transform.position.y;

        bool closeX = Mathf.Abs(diffX) <= minDistanceX;
        bool closeY = Mathf.Abs(diffY) <= minDistanceY;

        Vector2 vel = Vector2.zero;
        if (!closeY)
            vel.y = Mathf.Sign(diffY) * moveSpeed * 0.6f;
        else if (!closeX)
            vel.x = Mathf.Sign(diffX) * moveSpeed * 1.2f;

        rb.linearVelocity = vel;
        animator.SetBool(AnimKnifeRunning, vel.sqrMagnitude > 0.01f);
    }

    // ── Fase cuchillo ─────────────────────────────────────────

    private IEnumerator KnifePhaseRoutine()
    {
        knifeOutDone = true;
        isAttacking  = true;
        rb.linearVelocity = Vector2.zero;

        AudioManager.Instance?.PlaySFX(AudioManager.Instance.enemyJackLaught);
        animator.SetTrigger(AnimKnifeOut);

        float dur = GetClipLength("KnifeOut");
        if (dur <= 0f) dur = 1f;
        yield return new WaitForSeconds(dur);

        isKnifeMode = true;
        damage      = knifeMeleeDamage;
        animator.SetBool(AnimKnifeMode, true);
        isAttacking = false;

        EnemyHitbox eh = enemyHitbox != null
            ? enemyHitbox.GetComponent<EnemyHitbox>() : null;
        if (eh != null) eh.overrideDamage = knifeMeleeDamage;
    }

    // ── Ataque melee cuchillo ─────────────────────────────────

    private IEnumerator KnifeAttackRoutine()
    {
        isAttacking = true;
        attackTimer = attackCooldown;
        rb.linearVelocity = Vector2.zero;
        animator.SetBool(AnimKnifeRunning, false);
        animator.SetTrigger(AnimKnifeAttack);

        yield return new WaitForSeconds(attackHitboxDelay);

        if (enemyHitbox != null)
        {
            enemyHitbox.SetActive(true);
            CheckHitboxOverlapInternal();
            yield return new WaitForSeconds(attackHitboxDuration);
            enemyHitbox.SetActive(false);
        }

        isAttacking = false;
    }

    // ── KnifeRun + ataque ─────────────────────────────────────

    private IEnumerator KnifeRunAttackRoutine()
    {
        isAttacking = true;
        attackTimer = attackCooldown * 1.5f;

        animator.SetBool(AnimKnifeRunning, true);
        float elapsed = 0f;
        while (elapsed < 0.35f && player != null)
        {
            elapsed += Time.deltaTime;
            float diffX = player.position.x - transform.position.x;
            rb.linearVelocity = new Vector2(Mathf.Sign(diffX) * moveSpeed * 1.5f, 0f);
            yield return null;
        }

        rb.linearVelocity = Vector2.zero;
        animator.SetBool(AnimKnifeRunning, false);
        yield return StartCoroutine(KnifeAttackRoutine());
    }

    // ── Recibir daño ──────────────────────────────────────────

    public override void TakeHit(int dmg, float attackerX)
    {
        if (isDead || isOnFloor) return;

        currentHP -= dmg;
        currentHP  = Mathf.Max(currentHP, 0);

        // Notifica la barra de HP del enemigo en el Canvas
        EnemyHPBar.Instance?.ShowEnemy(this, currentHP, maxHP);
        EnemyHPBar.Instance?.UpdateHP(currentHP, maxHP);

        if (currentHP <= 0)
        {
            EnemyHPBar.Instance?.Hide();
            StartCoroutine(DieRoutine(attackerX));
            return;
        }

        bool isHeavyHit = dmg > 2;
        if (isHeavyHit)
            StartCoroutine(FloorHitRoutine(attackerX));
        else
            StartCoroutine(HurtRoutine(attackerX));
    }

    // ── Caída temporal ────────────────────────────────────────

    protected override IEnumerator FloorHitRoutine(float attackerX)
    {
        isOnFloor   = true;
        isAttacking = false;
        rb.linearVelocity = Vector2.zero;

        if (enemyHitbox != null) enemyHitbox.SetActive(false);
        animator.SetBool(AnimKnifeRunning, false);
        animator.SetTrigger(AnimFloor);

        yield return StartCoroutine(ApplyKnockback(attackerX, 2.5f));
        yield return new WaitForSeconds(1.5f);

        // Levantarse
        animator.SetTrigger(AnimStandUp);
        float standDur = GetClipLength("StandUp");
        if (standDur <= 0f) standDur = 0.8f;
        yield return new WaitForSeconds(standDur);

        isOnFloor   = false;
        isAttacking = false;
        rb.linearVelocity = Vector2.zero;
    }

    // ── Muerte — sobreescribe la base para notificar el GO ────

    protected override IEnumerator DieRoutine(float attackerX)
    {
        // Llama a la lógica de muerte de la base
        yield return StartCoroutine(base.DieRoutine(attackerX));

        // Notifica al LevelManager que Jack ha muerto
        LevelManager lm = FindFirstObjectByType<LevelManager>();
        if (lm != null) lm.OnJackDefeated();
    }

    // ── Animaciones ───────────────────────────────────────────

    protected override void UpdateAnimations()
    {
        if (isOnFloor || isDead) return;
        base.UpdateAnimations();
        animator.SetBool(AnimKnifeRunning,
            rb.linearVelocity.sqrMagnitude > 0.01f && isKnifeMode && !isAttacking);
    }

    // ── Utilidad ──────────────────────────────────────────────

    private void CheckHitboxOverlapInternal()
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

    private float GetClipLength(string clipName)
    {
        if (animator == null) return 0f;
        foreach (var clip in animator.runtimeAnimatorController.animationClips)
            if (clip.name.Contains(clipName)) return clip.length;
        return 0f;
    }
}