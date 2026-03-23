using UnityEngine;

/// <summary>
/// AttackHitbox — Hitbox del jugador
/// Usa EnemyBase para detectar cualquier tipo de enemigo.
/// </summary>
public class AttackHitbox : MonoBehaviour
{
    [Tooltip("Daño que inflige cada golpe")]
    public int damage = 1;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // ── Objeto rompible ───────────────────────────────────
        BreakableObject breakable = other.GetComponent<BreakableObject>();
        if (breakable != null)
        {
            breakable.TakeHit(transform.root.position.x);
            return;
        }

        // ── Cualquier enemigo (Galsia, Jack, Donovan...) ──────
        EnemyBase enemy = other.GetComponent<EnemyBase>();
        if (enemy != null)
        {
            enemy.TakeHit(damage, transform.root.position.x);
            return;
        }
    }
}
