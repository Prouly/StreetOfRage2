using System.Collections;
using UnityEngine;

public class BreakableObject : MonoBehaviour
{
    [Header("Drop")]
    public GameObject dropPrefab;
    public Vector2    dropOffset = new Vector2(0f, 0.5f);

    [Header("Animación")]
    public string crushTrigger  = "Crush";
    public float  crushDuration = 0.4f;

    [Header("Fade")]
    public float fadeDuration = 0.2f;

    [Header("Knockback al ser golpeado")]
    [Tooltip("Distancia que se desplaza en X")]
    public float knockbackDistance = 0.4f;
    [Tooltip("Duración del desplazamiento en segundos")]
    public float knockbackDuration = 0.1f;

    private SpriteRenderer sr;
    private Animator       anim;
    private bool           broken = false;
    private Vector3        originalPosition;

    private void Awake()
    {
        sr               = GetComponentInChildren<SpriteRenderer>();
        anim             = GetComponent<Animator>();
        originalPosition = transform.position;
    }

    /// <summary>
    /// attackerX — posición X del atacante para saber hacia dónde empujar
    /// </summary>
    public void TakeHit(float attackerX)
    {
        if (broken) return;
        broken = true;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        // Dirección del knockback: opuesta al atacante
        float direction = transform.position.x >= attackerX ? 1f : -1f;

        StartCoroutine(BreakRoutine(direction));
    }

    private IEnumerator BreakRoutine(float direction)
    {
        // 1 — Knockback: desplazamiento suave en X
        Vector3 startPos  = transform.position;
        Vector3 targetPos = startPos + new Vector3(direction * knockbackDistance, 0f, 0f);
        float   elapsed   = 0f;

        while (elapsed < knockbackDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / knockbackDuration);
            transform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }
        transform.position = targetPos;

        // 2 — Animación de rotura
        if (anim != null && !string.IsNullOrEmpty(crushTrigger))
        {
            anim.SetTrigger(crushTrigger);
            yield return new WaitForSeconds(crushDuration);
        }

        // 3 — Fade out
        if (sr != null)
        {
            float   elapsed2  = 0f;
            Color   original  = sr.color;
            while (elapsed2 < fadeDuration)
            {
                elapsed2 += Time.deltaTime;
                sr.color  = new Color(original.r, original.g, original.b,
                    Mathf.Lerp(1f, 0f, elapsed2 / fadeDuration));
                yield return null;
            }
        }

        // 4 — Spawn drop en la posición ORIGINAL (antes del knockback)
        if (dropPrefab != null)
        {
            Vector3 spawnPos = originalPosition + new Vector3(dropOffset.x, dropOffset.y, 0f);
            Instantiate(dropPrefab, spawnPos, Quaternion.identity);
        }

        Destroy(gameObject);
    }
}
