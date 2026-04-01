using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;


/// LevelManager — Gestiona GO (Jack), Stage Clear (Barbon) y transición de escena
///
/// SETUP:
///   - goObject: sprite GO+flecha, empieza desactivado
///   - doorTrigger: BoxCollider2D trigger en la puerta
///   - stageClearPanel: Panel con Clear/Time/Level Bonus, empieza desactivado
///   - clearBonusText, timeBonusText, levelBonusText: TMP dentro del panel
///   - nextSceneName: nombre exacto en Build Settings

public class LevelManager : MonoBehaviour
{
    [Header("GO — Jack derrotado")]
    [SerializeField] private GameObject    goObject;
    [SerializeField] private float         goShowDelay   = 1f;
    [SerializeField] private BoxCollider2D doorTrigger;

    [Header("Stage Clear — Barbon derrotado")]
    [SerializeField] private GameObject      stageClearPanel;
    [SerializeField] private TextMeshProUGUI clearBonusText;
    [SerializeField] private TextMeshProUGUI timeBonusText;
    [SerializeField] private TextMeshProUGUI levelBonusText;
    [SerializeField] private TextMeshProUGUI totalScoreText;

    [Header("Bonos de nivel")]
    [SerializeField] private int clearBonusAmount = 15000;
    [SerializeField] private int levelBonusAmount = 10000;
    [SerializeField] private int timeBonusPerSecond = 100;

    [Header("Audio")]
    [Tooltip("SFX de Stage Clear — se reproduce una vez, sin bucle")]
    [SerializeField] private AudioClip stageClearSFX;

    [Header("Siguiente escena")]
    [SerializeField] private string nextSceneName    = "Credits";
    [Tooltip("Segundos que se muestra el panel antes de cargar la siguiente escena")]
    [SerializeField] private float  sceneLoadDelay   = 8f;

    // ── Estado ───────────────────────────────────────────────
    private bool jackDefeated  = false;
    private bool bossDefeated  = false;
    private bool canTransition = false;

    // ─────────────────────────────────────────────────────────

    private void Start()
    {
        if (goObject       != null) goObject.SetActive(false);
        if (stageClearPanel != null) stageClearPanel.SetActive(false);
        if (doorTrigger    != null) doorTrigger.enabled = false;
    }

    // ── Jack derrotado → muestra GO ───────────────────────────

    // llamado desde EnemySpawnPoint cuando todos los enemigos han muerto.</summary>
    public void OnAllEnemiesDefeated()
    {
        // En Stage 1-2 esto activa el BarmanIntro si está configurado
        // En Stage 1-1 esto puede ignorarse o conectarse con el GO de Jack
        Debug.Log("[LevelManager] Todos los enemigos derrotados");
    }

    public void OnJackDefeated()
    {
        if (jackDefeated) return;
        jackDefeated = true;
        StartCoroutine(ShowGoRoutine());
    }

    private IEnumerator ShowGoRoutine()
    {
        yield return new WaitForSeconds(goShowDelay);

        if (goObject != null)
        {
            goObject.SetActive(true);
            AudioManager.Instance?.PlaySFX(AudioManager.Instance.goSignal);
            SpriteRenderer sr = goObject.GetComponentInChildren<SpriteRenderer>();
            if (sr != null) yield return StartCoroutine(FadeIn(sr, 0.5f));
        }

        if (doorTrigger != null)
        {
            doorTrigger.enabled = true;
            canTransition       = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!canTransition || !jackDefeated) return;

        PlayerController pc = other.GetComponent<PlayerController>();
        if (pc == null) return;

        canTransition = false;
        StartCoroutine(TransitionRoutine());
    }

    private IEnumerator TransitionRoutine()
    {
        yield return new WaitForSeconds(1f);
        
        // Persistencia de Datos entre Scenes
        if (GameManager.Instance != null)
        {
            PlayerPrefs.SetInt("TotalScore", GameManager.Instance.currentScore);
            PlayerPrefs.SetInt("PlayerLives", GameManager.Instance.currentLives);
        }

        PlayerController pcSave = FindObjectOfType<PlayerController>();
        if (pcSave != null)
        {
            PlayerPrefs.SetInt("PlayerHP", pcSave.currentHP);
        }
        PlayerPrefs.Save();
        //Cambio de Scene
        SceneManager.LoadScene(nextSceneName);
    }

    // ── Boss Barbon derrotado → Stage Clear ───────────────────

    public void OnBossDefeated()
    {
        if (bossDefeated) return;
        bossDefeated = true;

        GameManager.Instance?.StopTimer();
        StartCoroutine(StageClearRoutine());
    }
    
    private IEnumerator StageClearRoutine()
    {
        // Pausa para que se vea al boss morir
        yield return new WaitForSeconds(1.5f);

        // Bloquea al jugador durante el panel
        PlayerController pc = FindFirstObjectByType<PlayerController>();
        if (pc != null) pc.isBlocked = true;
        
        // SFX de Stage Clear (una sola vez, no en bucle)
        AudioManager.Instance?.StopMusic();
        if (stageClearSFX != null)
            AudioManager.Instance?.PlaySFX(stageClearSFX);

        // Calculo de puntos
        float timeLeft  = GameManager.Instance != null ? GameManager.Instance.GetCurrentTime() : 0f;
        int   timeBonus = Mathf.RoundToInt(timeLeft) * timeBonusPerSecond;
        int   totalBonus = clearBonusAmount + timeBonus + levelBonusAmount;
        int   scoreNow   = GameManager.Instance != null ? GameManager.Instance.currentScore : 0;

        GameManager.Instance?.AddScoreSilent(totalBonus);
        int finalScore = scoreNow + totalBonus;

        // Muestra el panel (debe tener Image de fondo negro con alpha alto)
        if (stageClearPanel != null)
        {
            Time.timeScale = 0f; // Pausamos el juego
            stageClearPanel.SetActive(true);
    
            UnityEngine.UI.Image bg = stageClearPanel.GetComponent<UnityEngine.UI.Image>();
            if (bg != null)
            {
                float elapsed = 0f;
                float duration = 0.5f;
                while (elapsed < duration)
                {
                    // unscaledDeltaTime para que la animación funcione en pausa
                    elapsed += Time.unscaledDeltaTime; 
                    // 1f para NEGRO TOTAL
                    bg.color = new Color(0f, 0f, 0f, Mathf.Lerp(0f, 1f, elapsed / duration));
                    yield return null;
                }
                bg.color = new Color(0f, 0f, 0f, 1f); 
            }
        }

        if (clearBonusText != null) clearBonusText.text = clearBonusAmount.ToString("D6");
        if (timeBonusText  != null) timeBonusText.text  = timeBonus.ToString("D6");
        if (levelBonusText != null) levelBonusText.text = levelBonusAmount.ToString("D6");
        if (totalScoreText != null) totalScoreText.text = finalScore.ToString("D6");

        // Mantener el panel el tiempo exacto puesto en el Inspector (sceneLoadDelay)
        // Uso de Realtime porque el Time.timeScale arriba se puso a 0f
        yield return new WaitForSecondsRealtime(sceneLoadDelay);
        
        // Persistencia de datos entre Scenes
        if (GameManager.Instance != null)
        {
            PlayerPrefs.SetInt("TotalScore", GameManager.Instance.currentScore);
            PlayerPrefs.SetInt("PlayerLives", GameManager.Instance.currentLives);
        }
        if (pc != null)
        {
            PlayerPrefs.SetInt("PlayerHP", pc.currentHP);
        }
        PlayerPrefs.Save();
        
        // Cambio de escena
        Time.timeScale = 1f; // restaura timeScale por si fue pausado
        SceneManager.LoadScene(nextSceneName);
    }

    // ── Utilidad ──────────────────────────────────────────────

    private IEnumerator FadeIn(SpriteRenderer sr, float duration)
    {
        Color c = sr.color; c.a = 0f; sr.color = c;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(0f, 1f, elapsed / duration);
            sr.color = c;
            yield return null;
        }
        c.a = 1f; sr.color = c;
    }
}