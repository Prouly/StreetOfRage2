using UnityEngine;


public class EnemyHitbox : MonoBehaviour
{
    [Tooltip("Tiempo mínimo entre golpes consecutivos al mismo jugador")]
    [SerializeField] private float damageCooldown = 0.5f;

    [Tooltip("Si > 0 sobreescribe el daño del EnemyBase (usado por Jack en fase cuchillo)")]
    public int overrideDamage = 0;

    [SerializeField] private EnemyBase enemy;
    [SerializeField] private float     damageTimer = 0f;

    private void Awake()
    {
        enemy = GetComponentInParent<EnemyBase>();
    }

    private void Update()
    {
        if (damageTimer > 0f)
            damageTimer -= Time.deltaTime;
    }

    // Reset del cooldown cada vez que el hitbox se desactiva
    // así el siguiente ataque siempre puede hacer daño desde el primer frame
    private void OnDisable()
    {
        damageTimer = 0f;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryDamagePlayer(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryDamagePlayer(other);
    }

    private void TryDamagePlayer(Collider2D other)
    {
        if (damageTimer > 0f) return;

        PlayerController player = other.GetComponent<PlayerController>();
        if (player == null) return;

        ApplyDamage(player);
    }

    public void ForceHit(Collider2D other)
    {
        if (damageTimer > 0f) return;

        PlayerController player = other.GetComponent<PlayerController>();
        if (player == null) return;

        ApplyDamage(player);
    }

    private void ApplyDamage(PlayerController player)
    {
        int    dmg          = overrideDamage > 0 ? overrideDamage
            : (enemy != null ? enemy.damage : 1);
        string attackerName = enemy != null ? enemy.gameObject.name : "Enemy";

        player.TakeDamage(dmg, attackerName);
        damageTimer = damageCooldown;
    }
}