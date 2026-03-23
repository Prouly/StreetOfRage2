using System.Collections;
using UnityEngine;

/// <summary>
/// EnemyBase — Clase base para todos los enemigos
///
/// SETUP COMÚN A TODOS LOS ENEMIGOS:
///   1. Rigidbody2D → Kinematic, Gravity Scale 0, Freeze Rotation Z
///   2. CapsuleCollider2D → Layer: Enemy
///   3. Añadir el script hijo (EnemyGalsia, EnemyJack...) NO este directamente
///   4. Crear hijo "EnemyHitbox" con BoxCollider2D (Is Trigger ON, Layer: EnemyAttack)
///   5. Tag "Player" asignado al Player_Axel
///
/// ANIMATOR — parámetros comunes:
///   Bool    → isWalking
///   Trigger → Hit     (animación de recibir golpe)
///   Trigger → Floor   (animación de caída/muerte)
///   Trigger → Punch   (animación de ataque)
/// </summary>
public abstract class EnemyBase : MonoBehaviour
{
    [Header("Stats")]
    public int   maxHP           = 3;
    public float moveSpeed       = 2f;
    public int   damage          = 1;

    [Header("Detección")]
    public float detectionRange  = 6f;
    public float attackRange     = 1f;

    [Header("Ataque")]
    public float attackCooldown       = 1.5f;
    public float attackHitboxDelay    = 0.2f;
    public float attackHitboxDuration = 0.2f;
    public GameObject enemyHitbox;

    [Header("Knockback")]
    public float knockbackDistance = 0.5f;
    public float knockbackDuration = 0.1f;

    // ── Componentes ──────────────────────────────────────────
    protected Rigidbody2D    rb;
    protected Animator       animator;
    protected SpriteRenderer sr;
    protected Transform      player;

    // ── Estado ───────────────────────────────────────────────
    protected int   currentHP;
    protected bool  isDead      = false;
    protected bool  isHurt      = false;
    protected bool  isAttacking = false;
    protected float attackTimer = 0f;

    // ── Hashes Animator ──────────────────────────────────────
    protected static readonly int AnimIsWalking = Animator.StringToHash("isWalking");
    protected static readonly int AnimHit       = Animator.StringToHash("Hit");
    protected static readonly int AnimFloor     = Animator.StringToHash("Floor");
    protected static readonly int AnimPunch     = Animator.StringToHash("Punch");

    // ─────────────────────────────────────────────────────────

    protected virtual void Awake()
    {
        rb        = GetComponent<Rigidbody2D>();
        animator  = GetComponent<Animator>();
        sr        = GetComponentInChildren<SpriteRenderer>();

        rb.gravityScale   = 0f;
        rb.freezeRotation = true;

        currentHP = maxHP;

        if (enemyHitbox != null)
            enemyHitbox.SetActive(false);

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }

    protected virtual void Update()
    {
        if (isDead || player == null) return; // ya estaba

        attackTimer -= Time.deltaTime;

        if (!isHurt && !isAttacking)
            UpdateAI();

        UpdateAnimations();
        FlipToPlayer();
    }

    // ── IA ────────────────────────────────────────────────────

    protected virtual void UpdateAI()
    {
        float dist = Vector2.Distance(transform.position, player.position);

        if (dist <= attackRange && attackTimer <= 0f)
            StartCoroutine(AttackRoutine());
        else if (dist <= attackRange) // Ya en rango pero cooldown activo → parar
            rb.linearVelocity = Vector2.zero;
        else if (dist <= detectionRange)
            ChasePlayer();
        else
            rb.linearVelocity = Vector2.zero;
    }

    protected virtual void ChasePlayer()
    {
        Vector2 dir = ((Vector2)player.position - (Vector2)transform.position).normalized;
        rb.linearVelocity = dir * moveSpeed;
    }

    // ── Ataque (sobreescribible por cada enemigo) ─────────────

    protected virtual IEnumerator AttackRoutine()
    {
        isAttacking = true;
        attackTimer = attackCooldown;
        rb.linearVelocity = Vector2.zero;

        animator.SetTrigger(AnimPunch);

        yield return new WaitForSeconds(attackHitboxDelay);

        if (enemyHitbox != null)
        {
            enemyHitbox.SetActive(true);
            Debug.Log($"[{gameObject.name}] ¡Golpea al player!");
            yield return new WaitForSeconds(attackHitboxDuration);
            enemyHitbox.SetActive(false);
        }
        else
            yield return new WaitForSeconds(attackHitboxDuration);

        isAttacking = false;
    }

    // ── Recibir daño ──────────────────────────────────────────
    public virtual void TakeHit(int dmg, float attackerX)
    {
        if (isDead) return;

        currentHP -= dmg;
        Debug.Log($"[{gameObject.name}] HP restante: {currentHP}/{maxHP}");

        if (currentHP <= 0)
        {
            isDead = true;          // ✅ Marca muerto ANTES de StopAllCoroutines
            StopAllCoroutines();    // Cancela HurtRoutine si estaba activa
            StartCoroutine(DieRoutine(attackerX));
        }
        else
            StartCoroutine(HurtRoutine(attackerX));
    }

    protected virtual IEnumerator HurtRoutine(float attackerX)
    {
        isHurt = true;
        rb.linearVelocity = Vector2.zero;

        animator.SetTrigger(AnimHit);
        yield return StartCoroutine(ApplyKnockback(attackerX, 1f));

        isHurt = false;
    }

    protected virtual IEnumerator DieRoutine(float attackerX)
    {
        //isDead = true;
        StopAllCoroutines(); // ojo: esto cancela DieRoutine también si se llama mal
        rb.linearVelocity = Vector2.zero;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        animator.ResetTrigger(AnimHit);
        animator.ResetTrigger(AnimPunch);
        animator.SetBool(AnimIsWalking, false);
        animator.SetTrigger(AnimFloor);

        yield return StartCoroutine(ApplyKnockback(attackerX, 2f));

        // Espera a que termine la animación Floor y ENTONCES congela el último frame
        yield return new WaitForSeconds(1.2f);
        animator.speed = 0f; // ✅ Congela el último frame de Floor
        yield return new WaitForSeconds(0.5f); // pequeña pausa congelado
        Destroy(gameObject);
    }

    protected IEnumerator ApplyKnockback(float attackerX, float multiplier)
    {
        float   dir      = transform.position.x >= attackerX ? 1f : -1f;
        Vector3 startPos = transform.position;
        Vector3 endPos   = startPos + new Vector3(dir * knockbackDistance * multiplier, 0f, 0f);
        float   elapsed  = 0f;

        while (elapsed < knockbackDuration)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos, endPos,
                Mathf.SmoothStep(0f, 1f, elapsed / knockbackDuration));
            yield return null;
        }
        transform.position = endPos;
    }

    // ── Animaciones ───────────────────────────────────────────

    protected virtual void UpdateAnimations()
    {
        if (isDead) return; // Si está muerto no toques el Animator
        bool moving = rb.linearVelocity.sqrMagnitude > 0.01f && !isHurt && !isAttacking;
        animator.SetBool(AnimIsWalking, moving);
    }

    // ── Flip ──────────────────────────────────────────────────

    protected virtual void FlipToPlayer()
    {
        if (player == null || sr == null) return;
        sr.flipX = transform.position.x < player.position.x;
    }

    // ── Gizmos ────────────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.9f, 0f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
