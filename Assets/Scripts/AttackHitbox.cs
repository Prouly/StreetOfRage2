using UnityEngine;

public class AttackHitbox : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"[AttackHitbox] Tocando: {other.gameObject.name} | Layer: {LayerMask.LayerToName(other.gameObject.layer)}");

        BreakableObject breakable = other.GetComponent<BreakableObject>();
        if (breakable != null)
        {
            // Pasamos la X del Player (padre del hitbox) para calcular dirección
            float attackerX = transform.root.position.x;
            breakable.TakeHit(attackerX);
            return;
        }

        // Enemigos en el futuro:
        // EnemyController enemy = other.GetComponent<EnemyController>();
        // if (enemy != null) enemy.TakeHit(damage, transform.root.position.x);
    }
}
