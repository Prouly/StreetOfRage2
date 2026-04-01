using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


/// EnemyHPBar — Barra de vida del enemigo en el Canvas
///
/// SETUP en el Canvas:
///   1. Crear Panel "EnemyHPPanel" (empieza desactivado)
///   2. Dentro: Image de fondo + Image roja (fillAmount) + Image amarilla (fillAmount)
///   3. Crear un GameObject con Image por cada tipo de enemigo (icono)
///      — todos en la misma posición, todos desactivados por defecto
///   4. Asignar referencias en este script
///
/// USO desde EnemyBase:
///   EnemyHPBar.Instance?.ShowEnemy(this, maxHP);
///   EnemyHPBar.Instance?.UpdateHP(currentHP, maxHP);
///   EnemyHPBar.Instance?.Hide();

public class EnemyHPBar : MonoBehaviour
{
    public static EnemyHPBar Instance { get; private set; }

    [Header("Panel")]
    [Tooltip("Panel raíz de la barra — empieza desactivado")]
    [SerializeField] private GameObject panel;

    [Header("Barras")]
    [Tooltip("Image amarilla (vida actual) — Filled, Horizontal")]
    [SerializeField] private Image healthBar;
    [Tooltip("Image roja (daño reciente) — Filled, Horizontal")]
    [SerializeField] private Image damageBar;

    [Header("Iconos por tipo de enemigo")]
    [Tooltip("Uno por tipo, en el mismo sitio, empiezan desactivados")]
    [SerializeField] private EnemyIcon[] enemyIcons;

    [System.Serializable]
    public struct EnemyIcon
    {
        public string      enemyTypeName; // "EnemyGalsia", "EnemyJack", etc.
        public GameObject  iconObject;
        [Tooltip("Objeto con el nombre del enemigo (opcional)")]
        public GameObject  nameObject;
    }

    [Header("Timeout")]
    [Tooltip("Segundos sin recibir golpes para ocultar la barra")]
    public float hideDelay = 5f;

    // ── Estado ───────────────────────────────────────────────
    private float hideTimer     = 0f;
    private bool  isVisible     = false;
    private float damageDelay   = 0.8f;
    private float damageTimer   = 0f;
    private float damageTarget  = 1f;

    // ─────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (panel != null) panel.SetActive(false);
    }

    private void Update()
    {
        if (!isVisible) return;

        // Timeout para ocultar
        hideTimer -= Time.deltaTime;
        if (hideTimer <= 0f) { Hide(); return; }

        // Barra roja con delay
        if (damageTimer > 0f)
            damageTimer -= Time.deltaTime;
        else if (damageBar != null)
            damageBar.fillAmount = Mathf.MoveTowards(
                damageBar.fillAmount, damageTarget, Time.deltaTime * 0.5f);
    }

    // ── API pública ───────────────────────────────────────────

    // Muestra la barra para un tipo de enemigo con su HP actual
    public void ShowEnemy(EnemyBase enemy, int currentHP, int maxHP)
    {
        if (panel == null) return;

        panel.SetActive(true);
        isVisible  = true;
        hideTimer  = hideDelay;

        // Activa el icono y nombre correctos
        // Busca por tipo exacto Y por tipo base (para subclases)
        System.Type enemyType = enemy.GetType();
        Debug.Log($"[EnemyHPBar] Tipo: {enemyType.Name} | BaseType: {enemyType.BaseType?.Name}");

        bool anyMatch = false;
        foreach (var icon in enemyIcons)
        {
            // Compara tanto el tipo exacto como posibles variantes de nombre
            bool match = string.Equals(icon.enemyTypeName, enemyType.Name,
                             System.StringComparison.OrdinalIgnoreCase);
            if (icon.iconObject != null)  icon.iconObject.SetActive(match);
            if (icon.nameObject != null)  icon.nameObject.SetActive(match);
            if (match) anyMatch = true;
        }
        if (!anyMatch)
            Debug.LogWarning($"[EnemyHPBar] No se encontró icono para: {enemyType.Name}");

        // Barras a lleno al mostrar un nuevo enemigo
        float norm = (float)currentHP / maxHP;
        if (healthBar != null) { healthBar.color = new Color(1f, 0.85f, 0f); healthBar.fillAmount = norm; }
        if (damageBar != null) { damageBar.color = new Color(0.85f, 0.1f, 0.1f); damageBar.fillAmount = norm; }
        damageTarget = norm;
    }

    // Actualiza la barra tras recibir daño.
    public void UpdateHP(int currentHP, int maxHP)
    {
        if (!isVisible) return;

        float norm = (float)currentHP / maxHP;
        hideTimer  = hideDelay; // resetea el timer

        if (healthBar != null) healthBar.fillAmount = norm;

        // Barra roja con delay
        damageTarget = norm;
        damageTimer  = damageDelay;
    }

    public void Hide()
    {
        isVisible = false;
        if (panel != null) panel.SetActive(false);
    }
}