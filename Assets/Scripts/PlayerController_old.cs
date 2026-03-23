using UnityEngine;

/// <summary>
/// PlayerController v4 — Streets of Rage 2 / Beat 'em up auténtico
///
/// ARQUITECTURA SOR2:
/// Sin física real. Movimiento libre en X e Y (pseudo-3D).
/// El salto es un arco senoidal visual, no física.
///
/// SETUP:
///   Rigidbody2D → Gravity Scale = 0, Freeze Rotation Z
///   No necesitas GroundCheck ni Layer de suelo.
///
/// ANIMATOR — parámetros:
///   Bool    → isWalking
///   Bool    → isJumping
///   Bool    → isFalling
///   Trigger → Attack | JumpAttack | Hurt | Death
///
///   ⚠ Has Exit Time = OFF en todas las transiciones
///   ⚠ Transition Duration = 0
/// </summary>

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class PlayerController_old : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeedX = 5f;
    public float moveSpeedY = 3.5f;

    [Header("Jump (pseudo-3D)")]
    public float jumpHeight   = 3f;
    public float jumpDuration = 0.5f;

    [Header("Screen Bounds")]
    [Tooltip("Activa los límites de movimiento")]
    public bool  useBounds = true;
    [Tooltip("Límite Y inferior — ajústalo al borde del camino")]
    public float minY = -3.5f;
    [Tooltip("Límite Y superior — ajústalo al fondo del escenario")]
    public float maxY =  1.5f;
    [Tooltip("Límite X izquierdo")]
    public float minX = -8f;
    [Tooltip("Límite X derecho")]
    public float maxX =  8f;

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
        ClampPosition();
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

    // ── Límites de pantalla ───────────────────────────────────
    // Se llama después del movimiento para contener al personaje
    // dentro del área jugable. Ajusta min/maxY en el Inspector
    // hasta que coincida con los bordes visuales del escenario.

    private void ClampPosition()
    {
        if (!useBounds) return;

        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, minX, maxX);

        // Durante el salto no aplicamos clamp en Y para no cortar el arco
        if (!isJumping)
            pos.y = Mathf.Clamp(pos.y, minY, maxY);

        transform.position = pos;
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

        float t = jumpTimer / totalDuration;
        float offsetY = jumpHeight * Mathf.Sin(t * Mathf.PI);

        Vector3 pos = transform.position;
        pos.y = jumpStartY + offsetY;
        transform.position = pos;

        if (jumpTimer >= totalDuration)
        {
            isJumping  = false;
            pos.y      = jumpStartY;
            transform.position = pos;
        }
    }

    // ── Animaciones ───────────────────────────────────────────

    private void UpdateAnimations()
    {
        bool isMoving = moveInput.sqrMagnitude > 0.01f && !isJumping;
        animator.SetBool(AnimIsWalking, isMoving);
        animator.SetBool(AnimIsJumping, isJumping && jumpTimer < jumpDuration);
        animator.SetBool(AnimIsFalling, isJumping && jumpTimer >= jumpDuration);
    }

    // ── Flip Sprite ───────────────────────────────────────────
    // El sprite por defecto mira a la IZQUIERDA → lógica invertida

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

    // ── Gizmos — visualiza los límites en el Editor ───────────

    private void OnDrawGizmosSelected()
    {
        if (!useBounds) return;
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.4f);
        Vector3 center = new Vector3((minX + maxX) / 2f, (minY + maxY) / 2f, 0f);
        Vector3 size   = new Vector3(maxX - minX, maxY - minY, 0f);
        Gizmos.DrawWireCube(center, size);
    }
}