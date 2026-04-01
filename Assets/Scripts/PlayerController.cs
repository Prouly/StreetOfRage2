using System.Collections;
using UnityEngine;

/// <summary>
/// PlayerController v10
/// - Caída al suelo tras 3 golpes seguidos en ventana de tiempo
/// - Levantarse con WASD
/// - Muerte con fade si HP llega a 0
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeedX = 5f;
    public float moveSpeedY = 3.5f;

    [Header("Jump (pseudo-3D)")]
    public float jumpHeight   = 1.5f;
    public float jumpDuration = 0.5f;

    [Header("Límites por zona")]
    [Tooltip("Una zona por sección del nivel. Define los límites X e Y de cada parte del escenario.")]
    public BoundZone[] boundZones;

    [System.Serializable]
    public struct BoundZone
    {
        [Tooltip("X donde empieza esta zona")]
        public float xStart;
        [Tooltip("X donde termina esta zona")]
        public float xEnd;
        [Tooltip("Y máxima (límite superior — fondo del escenario)")]
        public float maxY;
        [Tooltip("Y mínima (límite inferior — suelo visual)")]
        public float minY;
    }

    [Header("HP")]
    public int maxHP = 200;

    [Header("Knockdown")]
    [Tooltip("Golpes seguidos necesarios para caer al suelo")]
    [SerializeField] private int   knockdownHits   = 3;
    [Tooltip("Ventana de tiempo en segundos para contar golpes seguidos")]
    [SerializeField] private float knockdownWindow = 2f;
    [Tooltip("Tiempo en el suelo antes de poder levantarse")]
    [SerializeField] private float floorMinTime    = 1f;

    [Header("Muerte — Fade")]
    [SerializeField] private float deathFadeDuration = 1.5f;

    [Header("Ataque — Hitbox")]
    [SerializeField] private GameObject attackHitbox;
    [SerializeField] private float      attackHitboxDuration = 0.2f;
    [SerializeField] private float      attackCooldown       = 0.4f;

    [Header("Ataque — Daño por tipo")]
    [SerializeField] private int damagePunch    = 1;
    [SerializeField] private int damageKick     = 1;
    [SerializeField] private int damageHighKick = 1;
    [SerializeField] private int damageGancho   = 3;
    public int damageSpecial1 = 5;
    [SerializeField] private int damageSpecial2 = 5;

    [Header("Combo")]
    [SerializeField] private float comboWindow = 0.6f;

    [Header("Recogida")]
    [SerializeField] private float     pickupRadius  = 0.5f;
    [SerializeField] private LayerMask pickableLayer;

    [Header("Doble tap (Gancho)")]
    [SerializeField] private float doubleTapWindow = 0.3f;

    // ── Componentes ──────────────────────────────────────────
    private Rigidbody2D    rb;
    private Animator       animator;
    private SpriteRenderer spriteRenderer;

    // ── HP ───────────────────────────────────────────────────
    public int currentHP;
    private bool isDead = false;

    // ── Knockdown ────────────────────────────────────────────
    private int   consecutiveHits = 0;
    private float hitWindowTimer  = 0f;
    private bool  isDown          = false;
    private bool  canStandUp      = false;
    [HideInInspector] public bool isBlocked = false; // bloquea movimiento y ataques

    // ── Input ────────────────────────────────────────────────
    private Vector2 moveInput;

    // ── Salto ────────────────────────────────────────────────
    private bool  isJumping;
    private float jumpTimer;
    private float jumpStartY;
    private bool  jumpWithDirection; // true si saltó con dirección (para JumpHighKick)

    // ── Ataque ───────────────────────────────────────────────
    private bool  isAttacking;
    private float attackCooldownTimer;

    // ── Combo ────────────────────────────────────────────────
    private int   comboStep           = 0;
    private float comboTimer          = 0f;
    private bool  comboFinishing      = false;
    [HideInInspector] public bool specialSelfDamageApplied = false; // evita daño múltiple

    // ── Doble tap ────────────────────────────────────────────
    private float lastForwardTapTime = -999f;
    private bool  doubleTapReady     = false;
    private bool  ganchoArmed        = false;

    // ── Daño actual ──────────────────────────────────────────
    [HideInInspector] public int currentAttackDamage = 1;

    // ── Hashes Animator ──────────────────────────────────────
    private static readonly int AnimIsWalking   = Animator.StringToHash("isWalking");
    private static readonly int AnimJumpLowKick  = Animator.StringToHash("Axel_JumpLowKick");   // salto + ataque sin dirección
    private static readonly int AnimJumpHighKick = Animator.StringToHash("Axel_JumpHighKick"); // salto + dirección + ataque
    private static readonly int AnimIsJumping  = Animator.StringToHash("isJumping");
    private static readonly int AnimIsFalling  = Animator.StringToHash("isFalling");
    private static readonly int AnimAttack     = Animator.StringToHash("Attack");
    private static readonly int AnimKick       = Animator.StringToHash("Kick");
    private static readonly int AnimHighKick   = Animator.StringToHash("HighKick");
    private static readonly int AnimJumpAttack = Animator.StringToHash("JumpAttack");
    private static readonly int AnimPickUp     = Animator.StringToHash("PickUp");
    private static readonly int AnimSpecial1   = Animator.StringToHash("Axel_Special1");
    private static readonly int AnimSpecial2   = Animator.StringToHash("Axel_Special2");
    private static readonly int AnimGancho     = Animator.StringToHash("Axel_Gancho");
    private static readonly int AnimHurt       = Animator.StringToHash("Hurt");
    private static readonly int AnimFloor      = Animator.StringToHash("Floor");
    private static readonly int AnimStandUp    = Animator.StringToHash("StandUp");

    // ─────────────────────────────────────────────────────────

    private void Awake()
    {
        rb             = GetComponent<Rigidbody2D>();
        animator       = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb.gravityScale   = 0f;
        rb.freezeRotation = true;

        currentHP = maxHP;

        if (attackHitbox != null)
            attackHitbox.SetActive(false);
    }

    private void Update()
    {
        if (isDead) return;

        attackCooldownTimer -= Time.deltaTime;

        // Contador de ventana de golpes seguidos
        if (consecutiveHits > 0)
        {
            hitWindowTimer -= Time.deltaTime;
            if (hitWindowTimer <= 0f)
                consecutiveHits = 0;
        }

        // Contador de ventana de combo
        if (comboStep > 0)
        {
            comboTimer -= Time.deltaTime;
            if (comboTimer <= 0f)
                ResetCombo();
        }

        if (isDown)
        {
            HandleFloorInput();
            return;
        }

        if (isBlocked)
        {
            moveInput = Vector2.zero;
            rb.linearVelocity = Vector2.zero;
            return;
        }

        GatherInput();
        HandleJump();
        ApplyBounds();
        UpdateAnimations();
        FlipSprite();
    }

    private void FixedUpdate()
    {
        if (isDead || isDown) return;
        ApplyMovement();
    }

    // ── Input ─────────────────────────────────────────────────

    private void GatherInput()
    {
        // Movimiento bloqueado durante CUALQUIER ataque
        if (!isAttacking)
        {
            moveInput.x = Input.GetAxisRaw("Horizontal");
            moveInput.y = Input.GetAxisRaw("Vertical");
        }
        else
        {
            moveInput = Vector2.zero;
            rb.linearVelocity = Vector2.zero;
        }

        bool jumpPressed = Input.GetButtonDown("Jump");
        if (jumpPressed && !isJumping && !isAttacking)
        {
            jumpWithDirection = moveInput.x != 0;
            StartJump();
        }

        // JumpKick: salto + ataque (sin dirección)
        // JumpHighKick: salto + dirección + ataque
        if (isJumping && (Input.GetKeyDown(KeyCode.P) || Input.GetButtonDown("Fire1"))
            && !isAttacking && attackCooldownTimer <= 0f)
        {
            if (jumpWithDirection)
            {
                currentAttackDamage = 2; // JumpHighKick siempre hace 2 de daño
                AudioManager.Instance?.PlaySFX(AudioManager.Instance.axelJumpKick);
                StartJumpAttackMove(AnimJumpHighKick);
            }
            else
            {
                currentAttackDamage = 2; // JumpLowKick siempre hace 2 de daño
                AudioManager.Instance?.PlaySFX(AudioManager.Instance.axelJumpKick);
                StartJumpAttackMove(AnimJumpLowKick);
            }
        }

        // ── Doble tap ─────────────────────────────────────────
        bool tapRight = Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow);
        bool tapLeft  = Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow);

        if (tapRight || tapLeft)
        {
            float now = Time.time;
            if (doubleTapReady && (now - lastForwardTapTime) <= doubleTapWindow)
            {
                ganchoArmed    = true;
                doubleTapReady = false;
            }
            else
            {
                doubleTapReady     = true;
                ganchoArmed        = false;
                lastForwardTapTime = now;
            }
        }

        if (doubleTapReady && Time.time - lastForwardTapTime > doubleTapWindow)
        {
            doubleTapReady = false;
            ganchoArmed    = false;
        }

        // ── Golpe / Recoger ───────────────────────────────────
        bool attackPressed = Input.GetKeyDown(KeyCode.P) || Input.GetButtonDown("Fire1");

        if (attackPressed && attackCooldownTimer <= 0f)
        {
            if (ganchoArmed)
            {
                ganchoArmed    = false;
                doubleTapReady = false;
                currentAttackDamage = damageGancho;
                AudioManager.Instance?.PlaySFX(AudioManager.Instance.axelGancho);
                StartSpecialMove(AnimGancho);
            }
            else if (TryPickup()) { }
            else
                StartComboAttack();
        }

        // Special1/Special2: no se pueden usar mientras se salta
        // ni si el HP es 8% o menos (16 HP de 200)
        float hpThreshold = maxHP * 0.08f;
        bool  canSpecial  = !isJumping && !isAttacking && currentHP > hpThreshold;

        if (Input.GetKeyDown(KeyCode.O) && canSpecial)
        {
            currentAttackDamage = damageSpecial1;
            Debug.Log($"[Player] Special1 activado, daño: {currentAttackDamage}");
            StartSpecialMove(AnimSpecial1);
        }

        if (Input.GetKeyDown(KeyCode.I) && canSpecial)
        {
            currentAttackDamage = damageSpecial2;
            // Special2 no tiene sonido
            Debug.Log($"[Player] Special2 activado, daño: {currentAttackDamage}");
            StartSpecialMove(AnimSpecial2);
        }
    }

    // ── Input en el suelo — espera WASD para levantarse ───────

    private void HandleFloorInput()
    {
        if (!canStandUp) return;

        bool anyMovement = Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.A)
                        || Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.D)
                        || Input.GetKeyDown(KeyCode.UpArrow)   || Input.GetKeyDown(KeyCode.DownArrow)
                        || Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow);

        if (anyMovement)
            StartCoroutine(StandUpRoutine());
    }

    // ── Levantarse ────────────────────────────────────────────

    private IEnumerator StandUpRoutine()
    {
        canStandUp = false;
        animator.SetTrigger(AnimStandUp);

        // Espera la duración real de la animación StandUp
        float duration = GetClipLength("StandUp");
        if (duration <= 0f) duration = 0.8f;
        yield return new WaitForSeconds(duration);

        isDown          = false;
        consecutiveHits = 0;
        rb.linearVelocity = Vector2.zero;
    }

    // ── Sistema de combo ──────────────────────────────────────

    private void StartComboAttack()
    {
        isAttacking         = true;
        attackCooldownTimer = attackCooldown;
        comboTimer          = comboWindow;

        switch (comboStep)
        {
            case 0:
                comboFinishing      = false;
                currentAttackDamage = damagePunch;
                animator.SetTrigger(isJumping ? AnimJumpAttack : AnimAttack);
                break;
            case 1:
                comboFinishing      = false;
                currentAttackDamage = damageKick;
                animator.SetTrigger(AnimKick);
                break;
            case 2:
                comboFinishing      = true;
                currentAttackDamage = damageHighKick;
                animator.SetTrigger(AnimHighKick);
                StartCoroutine(FinishHighKick());
                break;
        }

        if (comboStep < 2)
            StartCoroutine(ActivateHitbox(attackHitboxDuration));
    }

    private IEnumerator FinishHighKick()
    {
        yield return StartCoroutine(ActivateHitbox(attackHitboxDuration));
        ResetCombo();
        comboFinishing = false;
    }

    public void OnHitLanded(string enemyName)
    {
        Debug.Log($"[Player] Golpe acertado → {enemyName} | Daño: {currentAttackDamage} | Combo step: {comboStep}");

        if (comboFinishing) return;

        if (comboStep < 2)
        {
            comboStep++;
            comboTimer = comboWindow;
        }
    }

    private void ResetCombo()
    {
        comboStep  = 0;
        comboTimer = 0f;
    }

    private IEnumerator ActivateHitbox(float duration)
    {
        specialSelfDamageApplied = false; // resetea al inicio de cada ataque
        if (attackHitbox != null)
        {
            attackHitbox.SetActive(true);
            CheckPlayerHitboxOverlap();
            yield return new WaitForSeconds(duration);
            attackHitbox.SetActive(false);
        }
        else
            yield return new WaitForSeconds(duration);

        isAttacking = false;
    }

    private void CheckPlayerHitboxOverlap()
    {
        if (attackHitbox == null) return;
        Collider2D hitCol = attackHitbox.GetComponent<Collider2D>();
        if (hitCol == null) return;

        Collider2D[] hits = new Collider2D[8];
        int count = Physics2D.OverlapCollider(hitCol, new ContactFilter2D().NoFilter(), hits);

        for (int i = 0; i < count; i++)
        {
            if (hits[i] == null) continue;
            if (hits[i].gameObject == gameObject) continue;

            EnemyBase enemy = hits[i].GetComponent<EnemyBase>()
                           ?? hits[i].GetComponentInParent<EnemyBase>();
            if (enemy != null && !enemy.IsDead)
            {
                enemy.TakeHit(currentAttackDamage, transform.position.x);
                OnHitLanded(enemy.gameObject.name);
                continue;
            }

            BreakableObject breakable = hits[i].GetComponent<BreakableObject>();
            if (breakable != null)
                breakable.TakeHit(transform.position.x);
        }
    }

    // ── Ataques en el aire ────────────────────────────────────

    private void StartJumpAttackMove(int animHash)
    {
        isAttacking         = true;
        attackCooldownTimer = attackCooldown;
        // Bloquea movimiento X durante JumpKick
        moveInput.x = 0f;
        animator.SetTrigger(animHash);
        StartCoroutine(ActivateHitbox(attackHitboxDuration));
    }

    // ── Movimientos especiales ────────────────────────────────

    private void StartSpecialMove(int animHash)
    {
        isAttacking         = true;
        attackCooldownTimer = attackCooldown;
        ResetCombo();
        animator.SetTrigger(animHash);
        StartCoroutine(ActivateHitbox(attackHitboxDuration));
    }

    // ── HP y daño recibido ────────────────────────────────────

    public void TakeDamage(int dmg, string attackerName)
    {
        if (isDead || isDown || isInvulnerable) return;

        currentHP -= dmg;
        currentHP  = Mathf.Max(currentHP, 0);

        Debug.Log($"[Player] Golpeado por: {attackerName} | Daño: {dmg} | HP restante: {currentHP}/{maxHP}");
        AudioManager.Instance?.PlaySFX(AudioManager.Instance.hitAxel);
        GameManager.Instance?.NotifyPlayerDamaged(currentHP, maxHP);

        if (currentHP <= 0)
        {
            AudioManager.Instance?.PlaySFX(AudioManager.Instance.axelDead);
            StartCoroutine(DeathRoutine());
            return;
        }

        // Cuenta golpes seguidos para el knockdown
        consecutiveHits++;
        hitWindowTimer = knockdownWindow;

        if (consecutiveHits >= knockdownHits)
        {
            consecutiveHits = 0;
            StartCoroutine(KnockdownRoutine());
        }
        else
        {
            animator.SetTrigger(AnimHurt);
        }
    }

    // ── Caída al suelo (knockdown) ────────────────────────────

    private IEnumerator KnockdownRoutine()
    {
        isDown      = true;
        canStandUp  = false;
        isAttacking = false;
        rb.linearVelocity = Vector2.zero;

        if (attackHitbox != null)
            attackHitbox.SetActive(false);

        animator.SetTrigger(AnimFloor);

        // Espera mínima en el suelo
        yield return new WaitForSeconds(floorMinTime);

        canStandUp = true;
        // El jugador ahora debe pulsar WASD para levantarse
    }

    // ── Muerte con fade ───────────────────────────────────────

    private IEnumerator DeathRoutine()
    {
        isDead      = true;
        isAttacking = false;
        rb.linearVelocity = Vector2.zero;
        respawnPosition = transform.position; // guarda posición para el respawn

        if (attackHitbox != null)
            attackHitbox.SetActive(false);

        animator.SetTrigger(AnimFloor);

        // Espera la animación de caída
        float floorDur = GetClipLength("Floor");
        if (floorDur <= 0f) floorDur = 1f;
        yield return new WaitForSeconds(floorDur);

        // Fade out
        if (spriteRenderer != null)
        {
            float   elapsed  = 0f;
            Color   original = spriteRenderer.color;
            while (elapsed < deathFadeDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0f, elapsed / deathFadeDuration);
                spriteRenderer.color = new Color(original.r, original.g, original.b, alpha);
                yield return null;
            }
        }

        Debug.Log("[Player] Ha muerto.");
        GameManager.Instance?.OnPlayerDeath();
        // El GameManager decide si respawnea o muestra Game Over
    }

    // ── Recogida ──────────────────────────────────────────────

    private bool TryPickup()
    {
        Collider2D hit = Physics2D.OverlapCircle(transform.position, pickupRadius, pickableLayer);
        if (hit == null) return false;

        Pickable pickable = hit.GetComponent<Pickable>();
        if (pickable == null) return false;

        AudioManager.Instance?.PlaySFX(AudioManager.Instance.pickUpHealth);
        animator.SetTrigger(AnimPickUp);
        pickable.Collect(this);
        return true;
    }

    // ── Movimiento ────────────────────────────────────────────

    private void ApplyMovement()
    {
        rb.linearVelocity = new Vector2(
            moveInput.x * moveSpeedX,
            moveInput.y * moveSpeedY);
    }

    // ── Límites por zona ──────────────────────────────────────
    // Clamp de posición por script — fiable con Rigidbody Kinematic.
    // Se llama en Update después del movimiento.

    private void ApplyBounds()
    {
        if (boundZones == null || boundZones.Length == 0) return;

        float x = transform.position.x;
        float y = transform.position.y;

        // Límites globales X (primera y última zona)
        float globalMinX = boundZones[0].xStart;
        float globalMaxX = boundZones[boundZones.Length - 1].xEnd;

        // Obtener límites Y interpolados para la X actual
        float currentMinY, currentMaxY;
        GetYLimitsAtX(x, out currentMinY, out currentMaxY);

        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, globalMinX, globalMaxX);

        // Durante el salto no aplicamos clamp en Y para no cortar el arco
        if (!isJumping)
        {
            pos.y = Mathf.Clamp(pos.y, currentMinY, currentMaxY);

            // Si el clamp en Y frenó el movimiento, para la velocidad en Y
            if (pos.y != transform.position.y)
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        }

        transform.position = pos;
    }

    private void GetYLimitsAtX(float x, out float minY, out float maxY)
    {
        if (boundZones.Length == 1)
        {
            minY = boundZones[0].minY;
            maxY = boundZones[0].maxY;
            return;
        }

        // Antes de la primera zona
        if (x <= boundZones[0].xStart)
        {
            minY = boundZones[0].minY;
            maxY = boundZones[0].maxY;
            return;
        }

        // Después de la última zona
        if (x >= boundZones[boundZones.Length - 1].xEnd)
        {
            minY = boundZones[boundZones.Length - 1].minY;
            maxY = boundZones[boundZones.Length - 1].maxY;
            return;
        }

        for (int i = 0; i < boundZones.Length; i++)
        {
            // Dentro de esta zona
            if (x >= boundZones[i].xStart && x <= boundZones[i].xEnd)
            {
                minY = boundZones[i].minY;
                maxY = boundZones[i].maxY;
                return;
            }

            // Transición interpolada entre zonas
            if (i < boundZones.Length - 1)
            {
                var next = boundZones[i + 1];
                if (x > boundZones[i].xEnd && x < next.xStart)
                {
                    float t = Mathf.InverseLerp(boundZones[i].xEnd, next.xStart, x);
                    minY = Mathf.Lerp(boundZones[i].minY, next.minY, t);
                    maxY = Mathf.Lerp(boundZones[i].maxY, next.maxY, t);
                    return;
                }
            }
        }

        minY = boundZones[boundZones.Length - 1].minY;
        maxY = boundZones[boundZones.Length - 1].maxY;
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

    // ── Flip ─────────────────────────────────────────────────

    private void FlipSprite()
    {
        if      (moveInput.x > 0) spriteRenderer.flipX = true;
        else if (moveInput.x < 0) spriteRenderer.flipX = false;
    }

    // ── Utilidades ────────────────────────────────────────────

    private float GetClipLength(string clipName)
    {
        if (animator == null) return 0f;
        foreach (var clip in animator.runtimeAnimatorController.animationClips)
            if (clip.name.Contains(clipName)) return clip.length;
        return 0f;
    }

    // ── API pública ───────────────────────────────────────────


    /// Daño + knockdown forzado (usado por el cuchillo lanzado de Jack).
    /// El player cae al suelo independientemente de los golpes consecutivos.

    public void TakeDamageAndKnockdown(int dmg, string attackerName)
    {
        if (isDead || isDown || isInvulnerable) return;

        currentHP -= dmg;
        currentHP  = Mathf.Max(currentHP, 0);

        Debug.Log($"[Player] Golpeado por: {attackerName} | Daño: {dmg} | HP restante: {currentHP}/{maxHP}");

        if (currentHP <= 0)
        {
            StartCoroutine(DeathRoutine());
            return;
        }

        // Fuerza knockdown independientemente del contador
        consecutiveHits = 0;
        StartCoroutine(KnockdownRoutine());
    }

    /// Cura al jugador la cantidad indicada, sin superar el máximo.
    public void Heal(int amount)
    {
        if (isDead) return;
        currentHP = Mathf.Min(currentHP + amount, maxHP);
        AudioManager.Instance?.PlaySFX(AudioManager.Instance.pickUpHealth);
        GameManager.Instance?.NotifyPlayerDamaged(currentHP, maxHP);
        Debug.Log($"[Player] Curado: +{amount} | HP: {currentHP}/{maxHP}");
    }

    /// Daño auto-infligido por especiales — no activa Hurt ni knockdown.
    public void TakeSelfDamage(int dmg)
    {
        if (isDead) return;
        currentHP -= dmg;
        currentHP  = Mathf.Max(currentHP, 0);
        GameManager.Instance?.NotifyPlayerDamaged(currentHP, maxHP);
        Debug.Log($"[Player] Self-damage por especial: -{dmg} | HP: {currentHP}/{maxHP}");
        if (currentHP <= 0) StartCoroutine(DeathRoutine());
    }

    public void TriggerHurt() => animator.SetTrigger(AnimHurt);

    public void TriggerDeath() => StartCoroutine(DeathRoutine());

    [Header("Respawn")]
    [Tooltip("Altura desde la que cae el jugador al reaparecer (unidades sobre su posición)")]
    public float respawnDropHeight = 12f;
    [Tooltip("Radio en el que los enemigos cercanos caen al suelo al aterrizar")]
    public float respawnKnockRadius = 3f;

    [HideInInspector] public Vector3 respawnPosition; // posición guardada al morir


    // Reaparece al jugador cayendo desde el cielo en su misma posición X.
    
    public void Respawn()
    {
        isDead          = false;
        isDown          = false;
        isAttacking     = false;
        isJumping       = false;
        consecutiveHits = 0;
        currentHP       = maxHP;
        enabled         = true;

        rb.linearVelocity = Vector2.zero;
        if (attackHitbox != null) attackHitbox.SetActive(false);

        animator.Rebind();
        animator.Update(0f);

        StartCoroutine(SkyDropRoutine());
    }

    private IEnumerator SkyDropRoutine()
    {
        // usar rb para mover, no transform directamente (Cinemachine interfiere)
        // Desactiva física temporalmente
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero;

        // Posición de inicio: misma X, Y muy alta
        Vector3 startPos = new Vector3(respawnPosition.x,
                                       respawnPosition.y + respawnDropHeight,
                                       respawnPosition.z);
        transform.position = startPos;

        // Sprite invisible al inicio
        if (spriteRenderer != null)
        {
            Color c = spriteRenderer.color;
            c.a = 0f;
            spriteRenderer.color = c;
        }

        // Breve pausa para que la cámara se recoloque
        yield return new WaitForSeconds(0.1f);

        animator.SetBool(AnimIsJumping, true);

        float fallSpeed = 0f;
        float gravity   = 20f;
        float fadeSpeed = 2f;

        while (transform.position.y > respawnPosition.y)
        {
            fallSpeed += gravity * Time.deltaTime;
            Vector3 pos = transform.position;
            pos.y -= fallSpeed * Time.deltaTime;
            transform.position = pos;

            if (spriteRenderer != null)
            {
                Color c = spriteRenderer.color;
                c.a = Mathf.Min(c.a + fadeSpeed * Time.deltaTime, 1f);
                spriteRenderer.color = c;
            }

            yield return null;
        }

        // Aterriza exactamente en la posición guardada
        transform.position = respawnPosition;
        rb.bodyType = RigidbodyType2D.Kinematic; // mantiene Kinematic

        if (spriteRenderer != null)
        {
            Color c = spriteRenderer.color; c.a = 1f;
            spriteRenderer.color = c;
        }

        animator.SetBool(AnimIsJumping, false);
        animator.SetBool(AnimIsFalling, false);

        // 3 segundos de invulnerabilidad
        StartCoroutine(InvulnerabilityRoutine(3f));

        KnockdownNearbyEnemies();
    }

    // ── Invulnerabilidad post-respawn ─────────────────────────
    private bool isInvulnerable = false;

    private IEnumerator InvulnerabilityRoutine(float duration)
    {
        isInvulnerable = true;

        // Parpadeo para indicar invulnerabilidad
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (spriteRenderer != null)
                spriteRenderer.enabled = !spriteRenderer.enabled;
            yield return new WaitForSeconds(0.15f);
            elapsed += 0.15f;
        }

        if (spriteRenderer != null) spriteRenderer.enabled = true;
        isInvulnerable = false;
    }

    private void KnockdownNearbyEnemies()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            transform.position, respawnKnockRadius);

        foreach (var hit in hits)
        {
            EnemyBase enemy = hit.GetComponent<EnemyBase>()
                           ?? hit.GetComponentInParent<EnemyBase>();
            if (enemy != null && !enemy.IsDead)
            {
                // Fuerza caída al suelo (daño ficticio > 2 para activar FloorHitRoutine)
                enemy.TakeHit(2, transform.position.x);
            }
        }
    }

    public int GetCurrentHP() => currentHP;
    public int GetMaxHP()     => maxHP;
    
    public void SetHP(int hp)
    {
        currentHP = Mathf.Clamp(hp, 0, maxHP);
    }

    // ── Gizmos ────────────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, pickupRadius);

        if (boundZones == null) return;
        for (int i = 0; i < boundZones.Length; i++)
        {
            var z = boundZones[i];

            // Límite superior (verde)
            Gizmos.color = new Color(0f, 1f, 0.3f, 0.9f);
            Gizmos.DrawLine(new Vector3(z.xStart, z.maxY, 0f), new Vector3(z.xEnd, z.maxY, 0f));

            // Límite inferior (rojo)
            Gizmos.color = new Color(1f, 0.3f, 0.1f, 0.9f);
            Gizmos.DrawLine(new Vector3(z.xStart, z.minY, 0f), new Vector3(z.xEnd, z.minY, 0f));

            // Límites X de zona (amarillo)
            Gizmos.color = new Color(1f, 0.9f, 0f, 0.5f);
            Gizmos.DrawLine(new Vector3(z.xStart, z.minY, 0f), new Vector3(z.xStart, z.maxY, 0f));
            Gizmos.DrawLine(new Vector3(z.xEnd,   z.minY, 0f), new Vector3(z.xEnd,   z.maxY, 0f));

            // Transición a siguiente zona (cian)
            if (i < boundZones.Length - 1)
            {
                var next = boundZones[i + 1];
                Gizmos.color = new Color(0f, 0.8f, 1f, 0.6f);
                Gizmos.DrawLine(
                    new Vector3(z.xEnd,     z.maxY, 0f),
                    new Vector3(next.xStart, next.maxY, 0f));
                Gizmos.DrawLine(
                    new Vector3(z.xEnd,     z.minY, 0f),
                    new Vector3(next.xStart, next.minY, 0f));
            }
        }
    }
}