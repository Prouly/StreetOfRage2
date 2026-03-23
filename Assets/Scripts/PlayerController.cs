using UnityEngine;

/// <summary>
/// PlayerController v7 — con sistema de ataque y recogida
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeedX = 5f;
    public float moveSpeedY = 3.5f;

    [Header("Jump (pseudo-3D)")]
    public float jumpHeight   = 3f;
    public float jumpDuration = 0.5f;

    [Header("Límite Superior Dinámico")]
    public UpperBoundZone[] upperBoundZones;

    [System.Serializable]
    public struct UpperBoundZone
    {
        public float xStart;
        public float xEnd;
        public float maxY;
    }

    [Header("Ataque")]
    [Tooltip("GameObject hijo AttackHitbox")]
    public GameObject attackHitbox;

    [Tooltip("Duración en segundos que el hitbox está activo")]
    public float attackHitboxDuration = 0.2f;

    [Tooltip("Cooldown entre ataques")]
    public float attackCooldown = 0.4f;

    [Header("Recogida")]
    [Tooltip("Radio de detección de objetos recogibles")]
    public float pickupRadius = 0.5f;
    [Tooltip("Layer de objetos recogibles")]
    public LayerMask pickableLayer;

    // ── Componentes ──────────────────────────────────────────
    private Rigidbody2D    rb;
    private Animator       animator;
    private SpriteRenderer spriteRenderer;

    // ── Input ────────────────────────────────────────────────
    private Vector2 moveInput;

    // ── Estado del salto ─────────────────────────────────────
    private bool  isJumping;
    private float jumpTimer;
    private float jumpStartY;

    // ── Estado del ataque ────────────────────────────────────
    private bool  isAttacking;
    private float attackCooldownTimer;

    // ── Hashes Animator ──────────────────────────────────────
    private static readonly int AnimIsWalking  = Animator.StringToHash("isWalking");
    private static readonly int AnimIsJumping  = Animator.StringToHash("isJumping");
    private static readonly int AnimIsFalling  = Animator.StringToHash("isFalling");
    private static readonly int AnimAttack     = Animator.StringToHash("Attack");
    private static readonly int AnimJumpAttack = Animator.StringToHash("JumpAttack");
    private static readonly int AnimPickUp     = Animator.StringToHash("PickUp");
    private static readonly int AnimHurt       = Animator.StringToHash("Hurt");
    private static readonly int AnimDeath      = Animator.StringToHash("Death");

    // ─────────────────────────────────────────────────────────

    private void Awake()
    {
        rb             = GetComponent<Rigidbody2D>();
        animator       = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb.gravityScale   = 0f;
        rb.freezeRotation = true;

        // El hitbox empieza desactivado siempre
        if (attackHitbox != null)
            attackHitbox.SetActive(false);
    }

    private void Update()
    {
        attackCooldownTimer -= Time.deltaTime;

        GatherInput();
        HandleJump();
        ApplyUpperBound();
        UpdateAnimations();
        FlipSprite();
    }

    private void FixedUpdate()
    {
        ApplyMovement();
    }

    // ── Input ─────────────────────────────────────────────────

    private void GatherInput()
    {
        // Bloquea movimiento durante el ataque
        if (!isAttacking)
        {
            moveInput.x = Input.GetAxisRaw("Horizontal");
            moveInput.y = Input.GetAxisRaw("Vertical");
        }
        else
        {
            moveInput = Vector2.zero;
        }

        if (Input.GetButtonDown("Jump") && !isJumping && !isAttacking)
            StartJump();

        // Ataque (Fire1 = Z o click izquierdo por defecto)
        if (Input.GetButtonDown("Fire1") && attackCooldownTimer <= 0f)
            StartAttack();

        // Recogida (Fire2 = X por defecto)
        if (Input.GetButtonDown("Fire2"))
            TryPickup();
    }

    // ── Movimiento ────────────────────────────────────────────

    private void ApplyMovement()
    {
        rb.linearVelocity = new Vector2(
            moveInput.x * moveSpeedX,
            moveInput.y * moveSpeedY
        );
    }

    // ── Límite superior dinámico ──────────────────────────────

    private void ApplyUpperBound()
    {
        if (isJumping || upperBoundZones == null || upperBoundZones.Length == 0) return;

        float currentMaxY = GetMaxYAtX(transform.position.x);
        Vector3 pos = transform.position;

        if (pos.y > currentMaxY)
        {
            pos.y = currentMaxY;
            transform.position = pos;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        }
    }

    private float GetMaxYAtX(float x)
    {
        if (upperBoundZones.Length == 1) return upperBoundZones[0].maxY;
        if (x <= upperBoundZones[0].xStart) return upperBoundZones[0].maxY;
        if (x >= upperBoundZones[upperBoundZones.Length - 1].xEnd)
            return upperBoundZones[upperBoundZones.Length - 1].maxY;

        for (int i = 0; i < upperBoundZones.Length; i++)
        {
            if (x >= upperBoundZones[i].xStart && x <= upperBoundZones[i].xEnd)
                return upperBoundZones[i].maxY;

            if (i < upperBoundZones.Length - 1)
            {
                var next = upperBoundZones[i + 1];
                if (x > upperBoundZones[i].xEnd && x < next.xStart)
                {
                    float t = Mathf.InverseLerp(upperBoundZones[i].xEnd, next.xStart, x);
                    return Mathf.Lerp(upperBoundZones[i].maxY, next.maxY, t);
                }
            }
        }
        return upperBoundZones[upperBoundZones.Length - 1].maxY;
    }

    // ── Ataque ────────────────────────────────────────────────

    private void StartAttack()
    {
        isAttacking = true;
        attackCooldownTimer = attackCooldown;

        animator.SetTrigger(isJumping ? AnimJumpAttack : AnimAttack);

        if (attackHitbox != null)
            StartCoroutine(ActivateHitbox());
    }

    private System.Collections.IEnumerator ActivateHitbox()
    {
        attackHitbox.SetActive(true);
        yield return new WaitForSeconds(attackHitboxDuration);
        attackHitbox.SetActive(false);
        isAttacking = false;
    }

    // ── Recogida ──────────────────────────────────────────────
    // El jugador pulsa Fire2 cerca de un objeto recogible.
    // Se ejecuta la animación PickUp y el objeto se consume.

    private void TryPickup()
    {
        Collider2D hit = Physics2D.OverlapCircle(
            transform.position, pickupRadius, pickableLayer);

        if (hit == null) return;

        Pickable pickable = hit.GetComponent<Pickable>();
        if (pickable == null) return;

        // Animación de recogida
        animator.SetTrigger(AnimPickUp);

        // Aplicar efecto del objeto
        pickable.Collect(this);
    }

    // ── Salto ─────────────────────────────────────────────────

    private void StartJump()
    {
        isJumping  = true;
        jumpTimer  = 0f;
        jumpStartY = transform.position.y;
    }

    private void HandleJump()
    {
        if (!isJumping) return;

        float totalDuration = jumpDuration * 2f;
        jumpTimer += Time.deltaTime;

        float t       = jumpTimer / totalDuration;
        float offsetY = jumpHeight * Mathf.Sin(t * Mathf.PI);

        Vector3 pos = transform.position;
        pos.y = jumpStartY + offsetY;
        transform.position = pos;

        if (jumpTimer >= totalDuration)
        {
            isJumping = false;
            pos.y     = jumpStartY;
            transform.position = pos;
            rb.linearVelocity  = new Vector2(rb.linearVelocity.x, 0f);
        }
    }

    // ── Animaciones ───────────────────────────────────────────

    private void UpdateAnimations()
    {
        bool isMoving = moveInput.sqrMagnitude > 0.01f && !isJumping && !isAttacking;
        animator.SetBool(AnimIsWalking, isMoving);
        animator.SetBool(AnimIsJumping, isJumping && jumpTimer <  jumpDuration);
        animator.SetBool(AnimIsFalling, isJumping && jumpTimer >= jumpDuration);
    }

    // ── Flip Sprite ───────────────────────────────────────────

    private void FlipSprite()
    {
        if      (moveInput.x > 0) spriteRenderer.flipX = true;
        else if (moveInput.x < 0) spriteRenderer.flipX = false;
    }

    // ── API pública ───────────────────────────────────────────

    public void TriggerHurt() => animator.SetTrigger(AnimHurt);

    public void TriggerDeath()
    {
        animator.SetTrigger(AnimDeath);
        rb.linearVelocity = Vector2.zero;
        enabled = false;
    }

    // ── Gizmos ────────────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        // Radio de recogida (azul)
        Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, pickupRadius);

        // Zonas de límite superior (verde)
        if (upperBoundZones == null) return;
        for (int i = 0; i < upperBoundZones.Length; i++)
        {
            var z = upperBoundZones[i];
            Gizmos.color = new Color(0f, 1f, 0.3f, 0.9f);
            Gizmos.DrawLine(new Vector3(z.xStart, z.maxY, 0f), new Vector3(z.xEnd, z.maxY, 0f));
            Gizmos.DrawWireSphere(new Vector3(z.xStart, z.maxY, 0f), 0.1f);
            Gizmos.DrawWireSphere(new Vector3(z.xEnd,   z.maxY, 0f), 0.1f);
            if (i < upperBoundZones.Length - 1)
            {
                Gizmos.color = new Color(1f, 0.9f, 0f, 0.6f);
                Gizmos.DrawLine(
                    new Vector3(z.xEnd, z.maxY, 0f),
                    new Vector3(upperBoundZones[i+1].xStart, upperBoundZones[i+1].maxY, 0f));
            }
        }
    }
}
