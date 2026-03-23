using UnityEngine;

/// <summary>
/// PlayerController v5 — Streets of Rage 2
///
/// LÍMITES:
///   - LimitDown: EdgeCollider2D físico (colisiona con Player layer)
///   - LimitUp:   Clamp por script (no colisión física, evita el bloqueo)
///   - LimitLeft: BoxCollider2D físico
///
/// CINEMACHINE:
///   Position Damping → 0,0,0
///   Follow Offset    → 0,0,-10
/// </summary>

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class PlayerController_Test : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeedX = 5f;
    public float moveSpeedY = 3.5f;

    [Header("Jump (pseudo-3D)")]
    public float jumpHeight   = 3f;
    public float jumpDuration = 0.5f;

    [Header("Límite Superior (LimitUp)")]
    [Tooltip("Activa el clamp de Y superior por script")]
    public bool  useUpperBound = true;
    [Tooltip("Y máxima que puede alcanzar el jugador (borde superior del camino)")]
    public float maxY = 1.5f;

    [Header("Límite Inferior (solo backup)")]
    [Tooltip("Solo activo si el EdgeCollider falla — normalmente OFF")]
    public bool  useLowerBound = false;
    public float minY = -3.5f;

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

    // ── Hashes Animator ──────────────────────────────────────
    private static readonly int AnimIsWalking  = Animator.StringToHash("isWalking");
    private static readonly int AnimIsJumping  = Animator.StringToHash("isJumping");
    private static readonly int AnimIsFalling  = Animator.StringToHash("isFalling");
    private static readonly int AnimAttack     = Animator.StringToHash("Attack");
    private static readonly int AnimJumpAttack = Animator.StringToHash("JumpAttack");
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
    }

    private void Update()
    {
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
        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");

        if (Input.GetButtonDown("Jump") && !isJumping)
            StartJump();

        if (Input.GetButtonDown("Fire1"))
            TriggerAttack();
    }

    // ── Movimiento libre pseudo-3D ────────────────────────────

    private void ApplyMovement()
    {
        rb.linearVelocity = new Vector2(
            moveInput.x * moveSpeedX,
            moveInput.y * moveSpeedY
        );
    }

    // ── Límite superior por script ────────────────────────────
    // LimitUp NO tiene colisión física con el Player.
    // El script impide que el jugador cruce la Y máxima.
    // Durante el salto se desactiva para no cortar el arco.

    private void ApplyUpperBound()
    {
        if (!useUpperBound || isJumping) return;

        Vector3 pos = transform.position;
        if (pos.y > maxY)
        {
            pos.y = maxY;
            transform.position = pos;
            // Detiene la velocidad en Y para que no "luche" contra el límite
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        }

        // Límite inferior de backup (normalmente lo gestiona el EdgeCollider)
        if (useLowerBound && pos.y < minY)
        {
            pos.y = minY;
            transform.position = pos;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        }
    }

    // ── Salto pseudo-3D (arco senoidal) ──────────────────────

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
        bool isMoving = moveInput.sqrMagnitude > 0.01f && !isJumping;
        animator.SetBool(AnimIsWalking, isMoving);
        animator.SetBool(AnimIsJumping, isJumping && jumpTimer <  jumpDuration);
        animator.SetBool(AnimIsFalling, isJumping && jumpTimer >= jumpDuration);
    }

    // ── Flip Sprite ───────────────────────────────────────────
    // Sprite por defecto mira a la IZQUIERDA → lógica invertida

    private void FlipSprite()
    {
        if      (moveInput.x > 0) spriteRenderer.flipX = true;
        else if (moveInput.x < 0) spriteRenderer.flipX = false;
    }

    // ── Combate ───────────────────────────────────────────────

    private void TriggerAttack()
    {
        animator.SetTrigger(isJumping ? AnimJumpAttack : AnimAttack);
    }

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
        // Línea verde = límite superior (script)
        if (useUpperBound)
        {
            Gizmos.color = new Color(0f, 1f, 0.3f, 0.8f);
            Gizmos.DrawLine(
                new Vector3(transform.position.x - 3f, maxY, 0f),
                new Vector3(transform.position.x + 3f, maxY, 0f)
            );
        }
        // Línea roja = límite inferior (backup)
        if (useLowerBound)
        {
            Gizmos.color = new Color(1f, 0.3f, 0f, 0.8f);
            Gizmos.DrawLine(
                new Vector3(transform.position.x - 3f, minY, 0f),
                new Vector3(transform.position.x + 3f, minY, 0f)
            );
        }
    }
}