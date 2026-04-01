using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(AudioSource))]
public class MainMenuController : MonoBehaviour
{
    [Header("Objetos de Interfaz")]
    [SerializeField] private GameObject panelMenu;
    [SerializeField] private GameObject panelOptions;
    [SerializeField] private GameObject sor2Logo;

    [Header("Configuración")]
    [SerializeField] private string firstLevelName = "Stage 1-1";

    [Header("Audio — Clips")]
    [SerializeField] private AudioClip menuMusic;      
    [SerializeField] private AudioClip navigationSFX;  
    [SerializeField] private AudioClip startGameSFX;   

    private AudioSource audioSource;
    private bool isReady = false; 

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        ShowMainMenu();
        
        PlayBackgroundMusic();

        isReady = true; 
    }

    private void PlayBackgroundMusic()
    {
        if (menuMusic != null && audioSource != null)
        {
            audioSource.clip = menuMusic;
            audioSource.loop = true; 
            audioSource.Play();
        }
    }

    public void StartGame()
    {
        if (startGameSFX != null)
            audioSource.PlayOneShot(startGameSFX);

        PlayerPrefs.DeleteAll();
        StartCoroutine(WaitAndStart());
    }

    private IEnumerator WaitAndStart()
    {
        yield return new WaitForSecondsRealtime(0.5f);
        SceneManager.LoadScene(firstLevelName);
    }

    // --- Botón: Options ---
    public void OpenOptions()
    {
        // Solo suena si el juego ya terminó de cargar el Start
        if (isReady) PlayNavSound();
        
        if (panelMenu != null) panelMenu.SetActive(false);
        if (sor2Logo != null)  sor2Logo.SetActive(false);
        if (panelOptions != null) panelOptions.SetActive(true);
    }

    // --- Botón: Back to Menu (dentro de Options) ---
    public void ShowMainMenu()
    {
        // Solo suena si el juego está listo y el panel de opciones estaba abierto
        if (isReady && panelOptions != null && panelOptions.activeSelf) 
            PlayNavSound();

        if (panelOptions != null) panelOptions.SetActive(false);
        if (panelMenu != null) panelMenu.SetActive(true);
        if (sor2Logo != null)  sor2Logo.SetActive(true);
    }

    // --- Botón: Exit Game ---
    public void ExitGame()
    {
        if (isReady) PlayNavSound();
        Invoke("ActualExit", 0.3f);
    }

    private void ActualExit()
    {
        Application.Quit();
    }

    private void PlayNavSound()
    {
        if (navigationSFX != null && audioSource != null)
            audioSource.PlayOneShot(navigationSFX);
    }
}