using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;


/// GameManager — Puntuación, vidas, timer, HP bar, Game Over, Continue
///
/// SETUP UI (en el Canvas):
///   - scoreText      : TMP del score (ej: "000000")
///   - timerText      : TMP del timer (ej: "99")
///   - livesText      : TMP de las vidas (ej: "x3")
///   - hpHealthBar    : Image amarilla (Filled, Horizontal)
///   - hpDamageBar    : Image roja (Filled, Horizontal, debajo de la amarilla)
///   - gameOverPanel  : Panel con el cartel Game Over (empieza desactivado)
///   - continuePanel  : Panel con "Continue? X" (empieza desactivado)

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // ── Puntuación ────────────────────────────────────────────
    [Header("Puntuación")]
    public int currentScore   = 0;
    [SerializeField] private int lifeEveryPoints = 27000;

    [Header("Puntos por golpe")]
    public int pointsBasicHit  = 20;
    public int pointsKnifeHit  = 50;
    public int pointsPipeHit   = 70;

    [Header("Puntos por enemigo")]
    [SerializeField] private int pointsGalsia    = 300;
    [SerializeField] private int pointsYSignal   = 500;
    [SerializeField] private int pointsDonovan   = 400;
    [SerializeField] private int pointsJack      = 2000;
    [SerializeField] private int pointsBarbon    = 5000;

    [Header("Puntos por objeto")]
    [SerializeField] private int pointsMoneyBag  = 1000;
    [SerializeField] private int pointsGoldBar   = 5000;

    [Header("Bonus de nivel")]
    [SerializeField] private int   clearBonus          = 5000;
    [SerializeField] private int   timeBonusPerSecond  = 100;

    // ── Vidas ─────────────────────────────────────────────────
    [Header("Vidas")]
    [SerializeField] private int startingLives   = 3;
    [SerializeField] private int maxLives        = 9;

    public int currentLives;
    [SerializeField] private int  scoreAtLastLife = 0; // para controlar cada 27k

    // ── Timer ─────────────────────────────────────────────────
    [Header("Timer")]
    public float levelTime     = 99f;
    [Tooltip("Si true, el tiempo no corre (útil para debugging o cinemáticas)")]
    public bool  freezeTime    = false;

    private float currentTime;
    private float sectionStartTime; // para reiniciar al hacer Continue
    private bool  timerRunning = true;

    // ── HP Bar ────────────────────────────────────────────────
    [Header("HP Bar")]
    [SerializeField] private Image hpHealthBar;
    [SerializeField] private Image hpDamageBar;

    private float hpDamageDelay  = 1f;
    private float hpDamageTimer  = 0f;
    private float hpDamageTarget = 1f;

    // ── UI Referencias ────────────────────────────────────────
    [Header("UI")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI livesText;

    [Header("Paneles")]
    public GameObject gameOverPanel;
    public GameObject continuePanel;
    public TextMeshProUGUI continueCountdownText;

    // ── Continue ──────────────────────────────────────────────
    [Header("Continue")]
    public int   continueCount     = 3;   // cuántos continues hay disponibles
    public float continueCountdown = 9f;  // segundos para pulsar Continue

    private int   remainingContinues;
    private bool  isGameOver = false;

    // ── Player ref ────────────────────────────────────────────
    private PlayerController player;

    // ─────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

private void Start()
{
    player = FindFirstObjectByType<PlayerController>();

    // --- CARGA DE VIDAS ---
    if (PlayerPrefs.HasKey("PlayerLives"))
    {
        currentLives = PlayerPrefs.GetInt("PlayerLives");
    }
    else
    {
        currentLives = startingLives; // Solo usa el valor inicial si no hay guardado
    }

    remainingContinues = continueCount;
    currentTime        = levelTime;
    sectionStartTime   = levelTime;
    
    // Si el índice de la escena es 0, se empieza de cero
    if (SceneManager.GetActiveScene().buildIndex == 0) 
    {
        PlayerPrefs.DeleteKey("TotalScore");
        PlayerPrefs.DeleteKey("PlayerLives");
        PlayerPrefs.DeleteKey("PlayerHP");
    }

    // Carga puntuación acumulada entre escenas
    if (PlayerPrefs.HasKey("TotalScore"))
    {
        currentScore    = PlayerPrefs.GetInt("TotalScore");
        scoreAtLastLife = currentScore;
    }
    else
    {
        currentScore    = 0; 
        scoreAtLastLife = 0;
    }

    if (gameOverPanel  != null) gameOverPanel.SetActive(false);
    if (continuePanel  != null) continuePanel.SetActive(false);

    if (hpHealthBar != null) { hpHealthBar.color = new Color(1f, 0.85f, 0f); }
    if (hpDamageBar != null) { hpDamageBar.color = new Color(0.85f, 0.1f, 0.1f); }

    // --- CARGA DE HP Y ACTUALIZACIÓN DE BARRAS ---
    if (PlayerPrefs.HasKey("PlayerHP") && player != null)
    {
        player.currentHP = PlayerPrefs.GetInt("PlayerHP");
        
        // Calculamos el porcentaje de la barra (vidaActual / vidaMaxima)
        float hpPercent = (float)player.currentHP / player.maxHP;
        
        if (hpHealthBar != null) hpHealthBar.fillAmount = hpPercent;
        if (hpDamageBar != null) hpDamageBar.fillAmount = hpPercent;
    }
    else
    {
        // Si no hay HP guardado, empezamos al máximo
        if (hpHealthBar != null) hpHealthBar.fillAmount = 1f;
        if (hpDamageBar != null) hpDamageBar.fillAmount = 1f;
    }

    UpdateScoreUI();
    UpdateTimerUI();
    UpdateLivesUI();
}

    private void Update()
    {
        if (isGameOver) return;

        // Timer
        if (timerRunning && !freezeTime)
        {
            // Timer a mitad de velocidad: resta 1 cada 2 segundos reales
            currentTime -= Time.deltaTime * 0.5f;
            currentTime  = Mathf.Max(currentTime, 0f);
            UpdateTimerUI();
            if (currentTime <= 0f) { timerRunning = false; OnTimeUp(); }
        }

        UpdateHPBar();
    }

    // ── HP Bar ────────────────────────────────────────────────

    private void UpdateHPBar()
    {
        if (player == null) return;

        float norm = (float)player.GetCurrentHP() / player.GetMaxHP();
        if (hpHealthBar != null) hpHealthBar.fillAmount = norm;

        if (hpDamageTimer > 0f)
            hpDamageTimer -= Time.deltaTime;
        else if (hpDamageBar != null)
            hpDamageBar.fillAmount = Mathf.MoveTowards(
                hpDamageBar.fillAmount, hpDamageTarget, Time.deltaTime * 0.4f);
    }

    public void NotifyPlayerDamaged(int newHP, int maxHP)
    {
        hpDamageTarget = (float)newHP / maxHP;
        hpDamageTimer  = hpDamageDelay;
    }

    // ── Puntuación ────────────────────────────────────────────

    public void AddScore(int points)
    {
        AddScoreSilent(points);
        // No reproduce sonido aquí — cada sistema lo gestiona
    }

    public void AddScoreSilent(int points)
    {
        currentScore += points;
        UpdateScoreUI();
        CheckLifeBonus();
    }

    private void CheckLifeBonus()
    {
        if (lifeEveryPoints <= 0) return;

        int earned = (currentScore - scoreAtLastLife) / lifeEveryPoints;
        if (earned > 0)
        {
            scoreAtLastLife += earned * lifeEveryPoints;
            AddLives(earned);
        }
    }

    public void AddEnemyKillScore(string enemyType)
    {
        int points = enemyType switch
        {
            "EnemyGalsia"     => pointsGalsia,
            "EnemyYSignal"    => pointsYSignal,
            "EnemyDonovan"    => pointsDonovan,
            "EnemyJack"       => pointsJack,
            "EnemyBossBarbon" => pointsBarbon,
            _                 => pointsGalsia
        };
        AddScoreSilent(points);
        Debug.Log($"[Score] {enemyType} derrotado: +{points} | Total: {currentScore}");
    }

    // ── Vidas ─────────────────────────────────────────────────

    public void AddLives(int amount)
    {
        currentLives = Mathf.Min(currentLives + amount, maxLives);
        AudioManager.Instance?.PlaySFX(AudioManager.Instance.lifeUp);
        UpdateLivesUI();
        Debug.Log($"[Lives] +{amount} vida(s) | Total: {currentLives}");
    }

    // Llamado desde PlayerController cuando el HP llega a 0.
    public void OnPlayerDeath()
    {
        currentLives--;
        UpdateLivesUI();

        // Guarda posición actual para el respawn (por si es por tiempo)
        if (player != null)
            player.respawnPosition = player.transform.position;

        if (currentLives <= 0)
        {
            // Bloquea al jugador durante Continue
            if (player != null) player.isBlocked = true;
            StartCoroutine(ShowContinueOrGameOver());
        }
        else
        {
            StartCoroutine(RespawnRoutine());
        }
    }

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(2f); // espera el fade out del player

        if (player != null)
        {
            player.Respawn();
            if (hpHealthBar != null) hpHealthBar.fillAmount = 1f;
            if (hpDamageBar != null) hpDamageBar.fillAmount = 1f;
            hpDamageTarget = 1f;
        }

        // Timer continúa salvo que se haya agotado (en ese caso se reinició a 99)
        timerRunning = true;
    }

    // ── Continue / Game Over ──────────────────────────────────

    private IEnumerator ShowContinueOrGameOver()
    {
        yield return new WaitForSeconds(2f);

        if (remainingContinues > 0 && continuePanel != null)
        {
            continuePanel.SetActive(true);
            yield return StartCoroutine(ContinueCountdown());
        }
        else
        {
            ShowGameOver();
        }
    }

    private IEnumerator ContinueCountdown()
    {
        float timer = continueCountdown;

        while (timer > 0f)
        {
            if (continueCountdownText != null)
                continueCountdownText.text = Mathf.CeilToInt(timer).ToString();

            // Cualquier botón hace Continue
            if (Input.anyKeyDown)
            {
                DoContinue();
                yield break;
            }

            timer -= Time.deltaTime;
            yield return null;
        }

        // Se acabó el tiempo del continue
        ShowGameOver();
    }

    public void DoContinue()
    {
        remainingContinues--;
        if (continuePanel != null) continuePanel.SetActive(false);

        currentLives = startingLives;
        currentTime  = sectionStartTime;
        timerRunning = true;

        UpdateLivesUI();
        UpdateTimerUI();

        // Desbloquea al jugador
        if (player != null) player.isBlocked = false;

        StartCoroutine(RespawnRoutine());
    }

    private void ShowGameOver()
    {
        isGameOver   = true;
        timerRunning = false;
        AudioManager.Instance?.StopMusic();
        AudioManager.Instance?.PlaySFX(AudioManager.Instance.gameOver);
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        Debug.Log("[GameManager] GAME OVER");
    }

    private void OnTimeUp()
    {
        Debug.Log("[GameManager] Tiempo agotado");
        // Guarda posición del jugador ANTES de resetear
        if (player != null)
            player.respawnPosition = player.transform.position;

        currentTime  = levelTime;
        timerRunning = false;

        if (player != null)
            OnPlayerDeath();
    }

    // ── Timer ─────────────────────────────────────────────────

    public void StopTimer()    => timerRunning = false;
    public float GetCurrentTime() => currentTime;

    public void ApplyBossDefeatedBonus()
    {
        StopTimer();
        int timeBonus  = Mathf.RoundToInt(currentTime) * timeBonusPerSecond;
        int totalBonus = clearBonus + timeBonus;
        Debug.Log($"[Score] Clear Bonus: {clearBonus} | Time Bonus: {timeBonus} | Total: +{totalBonus}");
        AddScoreSilent(totalBonus);
    }

    // ── UI ────────────────────────────────────────────────────

    private void UpdateScoreUI()
    {
        if (scoreText != null)
            scoreText.text = currentScore.ToString("D6");
    }

    private void UpdateTimerUI()
    {
        if (timerText != null)
            timerText.text = Mathf.CeilToInt(currentTime).ToString("D2");
    }

    private void UpdateLivesUI()
    {
        if (livesText != null)
            livesText.text = currentLives.ToString();
    }
}