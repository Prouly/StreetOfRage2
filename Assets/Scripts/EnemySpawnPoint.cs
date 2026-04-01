using System.Collections;
using UnityEngine;

/// EnemySpawnPoint — Spawn de enemigos basado en zonas X del escenario
///
/// SETUP:
///   1. Crear GameObjects vacíos en escena llamados "SpawnPoint_1", "SpawnPoint_2"...
///   2. Añadir este script a cada uno
///   3. Asignar los prefabs de enemigos en "enemiesToSpawn"
///   4. Asignar el Player en el Inspector o déjalo vacío (se busca por tag)
///
/// FUNCIONAMIENTO:
///   Cuando el Player cruza "triggerX", el spawn se activa.
///   Spawnea los enemigos con un pequeño delay entre cada uno.
///   Si "spawnOnce" es true (recomendado), solo se activa una vez.

public class EnemySpawnPoint : MonoBehaviour
{
    [Header("Trigger")]
    [Tooltip("El spawn se activa cuando el Player supera esta X")]
    public float triggerX = 10f;

    [Tooltip("Si true, solo se activa una vez aunque el player retroceda")]
    public bool spawnOnce = true;

    [Header("Enemigos a spawnear")]
    public SpawnEntry[] enemiesToSpawn;

    [System.Serializable]
    public struct SpawnEntry
    {
        [Tooltip("Prefab del enemigo")]
        public GameObject prefab;
        [Tooltip("Posición donde aparece")]
        public Vector3 spawnPosition;
        [Tooltip("Delay en segundos antes de que aparezca este enemigo")]
        public float delay;
    }

    [Header("Referencias")]
    public Transform player;

    // ── Estado ───────────────────────────────────────────────
    private bool triggered  = false;
    private bool spawned    = false;

    [Tooltip("Si true, notifica al LevelManager cuando todos los enemigos de este spawn mueren")]
    public bool notifyLevelManagerOnClear = false;

    // Lista de enemigos spawneados para saber cuándo han muerto todos
    private System.Collections.Generic.List<GameObject> spawnedEnemies
        = new System.Collections.Generic.List<GameObject>();

    // Evento opcional: se dispara cuando todos los enemigos de este spawn mueren
    public System.Action OnAllEnemiesDead;

    [Header("Barman Intro (opcional)")]
    [Tooltip("Si está asignado, se llama OnAllEnemiesDefeated cuando todos mueren")]
    public BarmanIntro barmanIntro;

    // ─────────────────────────────────────────────────────────

    private void Awake()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
    }

    private void Update()
    {
        if (spawned && spawnOnce) return;
        if (player == null) return;

        if (!triggered && player.position.x >= triggerX)
        {
            triggered = true;
            StartCoroutine(SpawnRoutine());
        }
    }

    private IEnumerator SpawnRoutine()
    {
        spawned = true;

        foreach (var entry in enemiesToSpawn)
        {
            yield return new WaitForSeconds(entry.delay);

            if (entry.prefab == null) continue;

            GameObject enemy = Instantiate(entry.prefab, entry.spawnPosition, Quaternion.identity);
            spawnedEnemies.Add(enemy);

            // Pequeño efecto de aparición (opcional)
            SpriteRenderer sr = enemy.GetComponentInChildren<SpriteRenderer>();
            if (sr != null) StartCoroutine(FadeIn(sr, 0.3f));
        }

        // Monitoriza cuando mueren todos
        StartCoroutine(MonitorEnemies());
    }

    private IEnumerator MonitorEnemies()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.5f);
            spawnedEnemies.RemoveAll(e => e == null);

            if (spawnedEnemies.Count == 0)
            {
                OnAllEnemiesDead?.Invoke();

                // Notifica al LevelManager si está configurado para ello
                if (notifyLevelManagerOnClear)
                {
                    LevelManager lm = FindFirstObjectByType<LevelManager>();
                    if (lm != null) lm.OnAllEnemiesDefeated();
                }
                Debug.Log($"[SpawnPoint] Todos muertos. BarmanIntro: {barmanIntro != null}");
                if (barmanIntro != null)
                {
                    barmanIntro.OnAllEnemiesDefeated();
                }
                yield break;
            }
        }
    }

    private IEnumerator FadeIn(SpriteRenderer sr, float duration)
    {
        Color c = sr.color;
        c.a     = 0f;
        sr.color = c;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            c.a      = Mathf.Lerp(0f, 1f, elapsed / duration);
            sr.color = c;
            yield return null;
        }

        c.a      = 1f;
        sr.color = c;
    }

    // ── Gizmos — muestra el trigger y las posiciones de spawn ─

    private void OnDrawGizmos()
    {
        // Línea del trigger X (amarillo)
        Gizmos.color = new Color(1f, 0.9f, 0f, 0.8f);
        Gizmos.DrawLine(new Vector3(triggerX, -20f, 0f), new Vector3(triggerX, 20f, 0f));

        if (enemiesToSpawn == null) return;

        // Posiciones de spawn (rojo)
        foreach (var entry in enemiesToSpawn)
        {
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.8f);
            Gizmos.DrawWireSphere(entry.spawnPosition, 0.4f);
            Gizmos.DrawLine(new Vector3(triggerX, entry.spawnPosition.y, 0f), entry.spawnPosition);
        }
    }
}