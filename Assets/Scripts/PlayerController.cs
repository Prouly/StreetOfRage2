using UnityEngine;

/// <summary>
/// PlayerController v8
///
/// CONTROLES:
///   Movimiento  → WASD / flechas
///   Salto       → Space
///   Golpe/Recoger → P o Fire1 (Z)
///   Special1    → O
///   Special2    → K
///   Gancho      → Doble tap adelante + P
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
    public GameObject attackHitbox;
    public float      attackHitboxDuration = 0.2f;
    public float      attackCooldown       = 0.4f;

    [Header("Recogida")]
    public float     pickupRadius  = 0.5f;
    public LayerMask pickableLayer;

    [Header("Doble tap (Gancho)")]
    [Tooltip("Tiempo máximo entre dos taps adelante para activar el Gancho")]
    public float doubleTapWindow = 0.3f;

    // ── Componentes ──────────────────────────────────────────
    private Rigidbody2D    rb;
    private Animator       animator;
    private SpriteRenderer spriteRenderer;

    // ── Input ────────────────────────────────────────────────
    private Vector2 moveInput;

    // ── Estado salto ─────────────────────────────────────────
    private bool  isJumping;
    private float jumpTimer;
    private float jumpStartY;

    // ── Estado ataque ────────────────────────────────────────
    private bool  isAttacking;
    private float attackCooldownTimer;

    // ── Doble tap adelante ───────────────────────────────────
    private float lastForwardTapTime  = -999f;
    private bool  doubleTapReady      = false;  // true tras el primer tap
    private bool  ganchoArmed         = false;  // true tras doble tap confirmado

    // ── Hashes Animator ──────────────────────────────────────
    private static readonly int AnimIsWalking  = Animator.StringToHash("isWalking");
    private static readonly int AnimIsJumping  = Animator.StringToHash("isJumping");
    private static readonly int AnimIsFalling  = Animator.StringToHash("isFalling");
    private static readonly int AnimAttack     = Animator.StringToHash("Attack");
    private static readonly int AnimJumpAttack = Animator.StringToHash("JumpAttack");
    private static readonly int AnimPickUp     = Animator.StringToHash("PickUp");
    private static readonly int AnimSpecial1   = Animator.StringToHash("Axel_Special1");
    private static readonly int AnimSpecial2   = Animator.StringToHash("Axel_Special2");
    private static readonly int AnimGancho     = Animator.StringToHash("Axel_Gancho");
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
        if (!isAttacking)
        {
            moveInput.x = Input.GetAxisRaw("Horizontal");
            moveInput.y = Input.GetAxisRaw("Vertical");
        }
        else
        {
            moveInput = Vector2.zero;
        }

        // Salto
        if (Input.GetButtonDown("Jump") && !isJumping && !isAttacking)
            StartJump();

        // ── Doble tap (cualquier dirección horizontal) ────────
        // Detecta dos pulsaciones de la misma dirección (izq+izq o der+der)
        // dentro de doubleTapWindow segundos → arma el Gancho
        bool tapRight = Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow);
        bool tapLeft  = Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow);
        bool tapAny   = tapRight || tapLeft;

        if (tapAny)
        {
            float now = Time.time;
            if (doubleTapReady && (now - lastForwardTapTime) <= doubleTapWindow)
            {
                // Segundo tap dentro del tiempo → Gancho armado
                ganchoArmed    = true;
                doubleTapReady = false;
            }
            else
            {
                // Primer tap
                doubleTapReady      = true;
                ganchoArmed         = false;
                lastForwardTapTime  = now;
            }
        }

        // Si el tiempo expira, resetea todo
        if (doubleTapReady && Time.time - lastForwardTapTime > doubleTapWindow)
        {
            doubleTapReady = false;
            ganchoArmed    = false;
        }

        // ── Golpe / Recoger (P o Fire1) ───────────────────────
        bool attackPressed = Input.GetKeyDown(KeyCode.P) || Input.GetButtonDown("Fire1");

        if (attackPressed && attackCooldownTimer <= 0f)
        {
            if (ganchoArmed)
            {
                // Doble tap confirmado + P → Gancho
                ganchoArmed    = false;
                doubleTapReady = false;
                StartSpecialMove(AnimGancho);
            }
            else if (TryPickup())
            { /* recogida ejecutada */ }
            else
                StartAttack();
        }

        // ── Special1 (O) ──────────────────────────────────────
        if (Input.GetKeyDown(KeyCode.O) && !isAttacking)
            StartSpecialMove(AnimSpecial1);

        // ── Special2 (I) ──────────────────────────────────────
        if (Input.GetKeyDown(KeyCode.I) && !isAttacking)
            StartSpecialMove(AnimSpecial2);
    }

    // ── Movimiento ────────────────────────────────────────────

    private void ApplyMovement()
    {
        rb.linearVelocity = new Vector2(
            moveInput.x * moveSpeedX,
            moveInput.y * moveSpeedY
        );
    }

    // ── Ataque normal ─────────────────────────────────────────

    private void StartAttack()
    {
        isAttacking         = true;
        attackCooldownTimer = attackCooldown;
        animator.SetTrigger(isJumping ? AnimJumpAttack : AnimAttack);

        if (attackHitbox != null)
            StartCoroutine(ActivateHitbox(attackHitboxDuration));
    }

    // ── Movimientos especiales ────────────────────────────────

    private void StartSpecialMove(int animHash)
    {
        isAttacking         = true;
        attackCooldownTimer = attackCooldown;
        animator.SetTrigger(animHash);

        if (attackHitbox != null)
            StartCoroutine(ActivateHitbox(attackHitboxDuration));
    }

    private System.Collections.IEnumerator ActivateHitbox(float duration)
    {
        attackHitbox.SetActive(true);
        yield return new WaitForSeconds(duration);
        attackHitbox.SetActive(false);
        isAttacking = false;
    }

    // ── Recogida ──────────────────────────────────────────────
    // Devuelve true si había un objeto recogible cerca

    private bool TryPickup()
    {
        Collider2D hit = Physics2D.OverlapCircle(
            transform.position, pickupRadius, pickableLayer);

        Debug.Log($"[Pickup] Buscando en radio {pickupRadius} | Layer mask: {pickableLayer.value} | Hit: {(hit != null ? hit.name : "null")}");
        
        if (hit == null) return false;

        Pickable pickable = hit.GetComponent<Pickable>();
        if (pickable == null) return false;

        animator.SetTrigger(AnimPickUp);
        pickable.Collect(this);
        return true;
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
            rb.linearVelocity  = new Vector2(rb.linearVelocity.x, 0f);
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
        Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, pickupRadius);

        if (upperBoundZones == null) return;
        for (int i = 0; i < upperBoundZones.Length; i++)
        {
            var z = upperBoundZones[i];
            Gizmos.color = new Color(0f, 1f, 0.3f, 0.9f);
            Gizmos.DrawLine(new Vector3(z.xStart, z.maxY, 0f), new Vector3(z.xEnd, z.maxY, 0f));
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