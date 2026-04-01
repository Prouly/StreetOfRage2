using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI; 

/// CreditsController — Escena de créditos final
///
/// SETUP en la escena Credits:
///   1. Canvas con:
///      - scoreTotalText (TMP) — muestra la puntuación final
///      - subtitleText (TMP) — texto de créditos
///   2. Array spriteShowcases: GameObjects con SpriteRenderer + Animator
///      (personajes del juego mostrando sus animaciones)
///   3. Todos los spriteShowcases empiezan desactivados

public class CreditsController : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioClip creditsMusic;
    [Range(0f, 1f)] public float musicVolume = 0.8f;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip startGameSFX;
    [SerializeField] private AudioClip navigationSFX;
    
    [Header("Scene Main Menu")]
    [SerializeField] private string MenuSceneName = "MainMenu";

    [Header("UI General")]
    [SerializeField] private TextMeshProUGUI scoreTotalText;

    [Header("Showcase Config")]
    [Tooltip("Si está marcado, al terminar la lista volverá a empezar desde el principio")]
    public bool loopCredits = true;
    
    public SpriteShowcase[] showcases;

    [System.Serializable]
    public struct SpriteShowcase {
        public GameObject spriteObject;
        public string animationTrigger; // Nombre del Trigger en el Animator (ej: "Attack", "Punch", "Hit")
        public float displayDuration;
        public float fadeDuration;
    }

    void Start() {
        Time.timeScale = 1f;

        if (creditsMusic != null) {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.clip = creditsMusic;
            audioSource.volume = musicVolume;
            audioSource.loop = true;
            audioSource.Play();
        }

        if (scoreTotalText != null) {
            int finalScore = PlayerPrefs.GetInt("TotalScore", 0);
            scoreTotalText.text = finalScore.ToString("D6"); 
        }

        // Inicializar: todo invisible y apagado
        foreach (var s in showcases) {
            if (s.spriteObject != null) {
                SetAlpha(s.spriteObject, 0f);
                s.spriteObject.SetActive(false);
            }
        }

        StartCoroutine(ShowcaseRoutine());
    }

    private IEnumerator ShowcaseRoutine() {
        yield return new WaitForSeconds(0.5f);
        
        // do-while para que si 'loopCredits' es true, se repita infinitamente
        do {
            foreach (var showcase in showcases) {
                if (showcase.spriteObject == null) continue;

                // 1. Activar objeto
                showcase.spriteObject.SetActive(true);
                
                // 2. LANZAR ANIMACIÓN ESPECÍFICA
                Animator anim = showcase.spriteObject.GetComponent<Animator>();
                if (anim != null && !string.IsNullOrEmpty(showcase.animationTrigger)) {
                    anim.SetTrigger(showcase.animationTrigger);
                }

                // 3. Fade In
                yield return StartCoroutine(FadeRoutine(showcase.spriteObject, 0f, 1f, showcase.fadeDuration));
                
                // 4. Esperar
                yield return new WaitForSeconds(showcase.displayDuration);
                
                // 5. Fade Out
                yield return StartCoroutine(FadeRoutine(showcase.spriteObject, 1f, 0f, showcase.fadeDuration));
                
                showcase.spriteObject.SetActive(false);
                yield return new WaitForSeconds(0.5f);
            }
        } while (loopCredits); 
    }

    private IEnumerator FadeRoutine(GameObject obj, float from, float to, float duration) {
        float elapsed = 0f;
        SpriteRenderer sr = obj.GetComponentInChildren<SpriteRenderer>();
        Image img = obj.GetComponentInChildren<Image>();

        while (elapsed < duration) {
            elapsed += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(from, to, elapsed / duration);
            if (sr != null) { Color c = sr.color; c.a = alpha; sr.color = c; }
            if (img != null) { Color c = img.color; c.a = alpha; img.color = c; }
            yield return null;
        }
    }

    private void SetAlpha(GameObject obj, float alpha) {
        SpriteRenderer sr = obj.GetComponentInChildren<SpriteRenderer>();
        Image img = obj.GetComponentInChildren<Image>();
        if (sr != null) { Color c = sr.color; c.a = alpha; sr.color = c; }
        if (img != null) { Color c = img.color; c.a = alpha; img.color = c; }
    }
    
    // --- Botón: Exit Game ---
    public void ExitGame()
    {
        PlayNavSound();
        Invoke("ActualExit", 0.3f);
    }
    
    private void ActualExit()
    {
        Application.Quit();
    }
    
    // --- Botón: Main Menu ---
    public void MainMenu()
    {
        if (startGameSFX != null)
            audioSource.PlayOneShot(startGameSFX);

        PlayerPrefs.DeleteAll();
        StartCoroutine(WaitAndStart());
    }
    
    private IEnumerator WaitAndStart()
    {
        yield return new WaitForSecondsRealtime(0.5f);
        SceneManager.LoadScene(MenuSceneName);
    }
    
    private void PlayNavSound()
    {
        if (navigationSFX != null && audioSource != null)
            audioSource.PlayOneShot(navigationSFX);
    }
}