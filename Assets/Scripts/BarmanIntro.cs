using System.Collections;
using UnityEngine;

public class BarmanIntro : MonoBehaviour
{
    [Header("Barman")]
    [SerializeField] private GameObject barmanObject;
    [SerializeField] private float exitSpeed = 3f;
    [SerializeField] private float exitX     = 34.5f;

    [Header("Boss")]
    [SerializeField] private GameObject bossPrefab;
    [SerializeField] private Transform  bossSpawnPoint;

    [Header("Delays")]
    [SerializeField] private float delayBeforeCome = 0.4f;
    [SerializeField] private float delayBeforeBoss = 0.5f;

    private bool triggered = false;
    private static readonly int AnimCome = Animator.StringToHash("Come");

    public void OnAllEnemiesDefeated()
    {
        if (triggered) return;
        triggered = true;
        Debug.Log("[BarmanIntro] OnAllEnemiesDefeated llamado");
        StartCoroutine(BarmanExitRoutine());
    }

    private IEnumerator BarmanExitRoutine()
    {
        Debug.Log("[BarmanIntro] Corrutina iniciada");
        yield return new WaitForSeconds(delayBeforeCome);

        if (barmanObject == null)
        {
            Debug.LogError("[BarmanIntro] barmanObject es NULL");
            yield break;
        }

        SpriteRenderer sr = barmanObject.GetComponentInChildren<SpriteRenderer>();
        Animator anim     = barmanObject.GetComponent<Animator>();

        if (sr   != null) sr.flipX = true;
        if (anim != null) anim.SetTrigger(AnimCome);

        yield return new WaitForSeconds(0.2f);

        // Mover hasta salir de pantalla
        while (barmanObject != null && barmanObject.transform.position.x < exitX)
        {
            barmanObject.transform.position += Vector3.right * exitSpeed * Time.deltaTime;
            yield return null;
        }
        
        // Desactivar Sprite Renderer para se pueda terminar de ejecutar el Spawn del Boss antes de desactivar el objeto.
        if (sr != null) sr.enabled = false; 
    
        Debug.Log("[BarmanIntro] Barman invisible. Esperando para spawnear boss...");
        yield return new WaitForSeconds(delayBeforeBoss);

        // Spawneamos al Boss
        SpawnBoss();

        // Ahora que el Boss ya está fuera, se desactiva el objeto del Barman
        if (barmanObject != null) barmanObject.SetActive(false);
    }

    private void SpawnBoss()
    {
        if (bossPrefab == null)
        {
            Debug.LogError("[BarmanIntro] bossPrefab es NULL");
            return;
        }

        Vector3 pos = bossSpawnPoint != null ? bossSpawnPoint.position : transform.position;
        GameObject boss = Instantiate(bossPrefab, pos, Quaternion.identity);
        Debug.Log($"[BarmanIntro] Boss spawneado en {pos}");

        SpriteRenderer sr = boss.GetComponentInChildren<SpriteRenderer>();
    }
}
