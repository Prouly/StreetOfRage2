using UnityEngine;

/// AudioManager
///
/// SETUP:
///   1. GameObject "AudioManager" en escena con DontDestroyOnLoad
///   2. Dos AudioSource hijos: musicSource y sfxSource
///   3. Asignar todos los clips desde Assets/Audio/SFX/

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Música")]
    [SerializeField] private AudioClip bgMusic;

    [Header("SFX — Player")]
    public AudioClip axelDead;       // Axel_Dead
    public AudioClip axelGancho;     // Axel_Gancho
    public AudioClip axelJumpKick;   // Axel_JumpKick
    public AudioClip hitAxel;        // Hit_Axel (player recibe golpe)

    [Header("SFX — Enemigos")]
    public AudioClip hitEnemy;       // Hit_Enemy (golpe al enemigo)
    public AudioClip enemyDead;      // Enemy_Dead
    public AudioClip enemyJackLaught;// Enemy_Jack_Laught (Jack al sacar cuchillo)
    public AudioClip crush;          // Crush (romper objeto)

    [Header("SFX — Recogida")]
    public AudioClip pickUpHealth;   // PickUpHealth
    public AudioClip pickUpPoints;   // PickUpPoints
    public AudioClip lifeUp;         // LifeUp (vida extra)

    [Header("SFX — Sistema")]
    public AudioClip gameOver;       // Game Over
    public AudioClip goSignal;       // GO → avanzar al siguiente área

    [Header("Música — Jefes")]
    [SerializeField] private AudioClip bossMusic;      // Música del boss Barbon

    [Header("SFX — Stage Clear")]
    [SerializeField] private AudioClip stageClearSFX;  // Se reproduce una vez al completar nivel

    [Header("Volúmenes")]
    [Range(0f, 1f)] public float musicVolume = 0.7f;
    [Range(0f, 1f)] public float sfxVolume   = 1f;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (bgMusic != null) PlayMusic(bgMusic);
    }

    public void PlayMusic(AudioClip clip)
    {
        if (musicSource == null || clip == null) return;
        musicSource.clip   = clip;
        musicSource.loop   = true;
        musicSource.volume = musicVolume;
        musicSource.Play();
    }

    public void StopMusic() => musicSource?.Stop();

    public void PlaySFX(AudioClip clip)
    {
        if (sfxSource == null || clip == null) return;
        sfxSource.PlayOneShot(clip, sfxVolume);
    }
}