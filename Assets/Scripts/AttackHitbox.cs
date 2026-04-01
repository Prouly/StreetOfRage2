using UnityEngine;


/// AttackHitbox
/// evita que el mismo enemigo reciba daño doble en un mismo ataque
/// y garantiza que el siguiente ataque detecta desde el primer frame

public class AttackHitbox : MonoBehaviour
{
    private PlayerController player;

    // Registro de enemigos golpeados en este ataque para no golpear dos veces
    private System.Collections.Generic.HashSet<EnemyBase> hitThisAttack
        = new System.Collections.Generic.HashSet<EnemyBase>();

    // Cuántos enemigos se han golpeado en este ataque (para especiales con self-damage)
    private int enemiesHitThisAttack = 0;

    private void Awake()
    {
        player = GetComponentInParent<PlayerController>();
    }

    // Limpia el registro cada vez que el hitbox se desactiva
    private void OnDisable()
    {
        hitThisAttack.Clear();
        enemiesHitThisAttack = 0;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        HandleHit(other);
    }

    private void HandleHit(Collider2D other)
    {
        // ── Objeto rompible ───────────────────────────────────
        BreakableObject breakable = other.GetComponent<BreakableObject>();
        if (breakable != null)
        {
            breakable.TakeHit(transform.root.position.x);
            return;
        }

        // ── Enemigo ───────────────────────────────────────────
        EnemyBase enemy = other.GetComponent<EnemyBase>()
                       ?? other.GetComponentInParent<EnemyBase>();

        if (enemy != null && !enemy.IsDead && !hitThisAttack.Contains(enemy))
        {
            hitThisAttack.Add(enemy);
            enemiesHitThisAttack++;

            AudioManager.Instance?.PlaySFX(AudioManager.Instance.hitEnemy);
            int dmg = player != null ? player.currentAttackDamage : 1;
            Debug.Log($"[AttackHitbox] Golpeando {enemy.name} con daño {dmg} (Special threshold: {(player != null ? player.damageSpecial1 : 0)})");
            enemy.TakeHit(dmg, transform.root.position.x);

            if (player != null)
            {
                player.OnHitLanded(enemy.gameObject.name);

                // Suma puntos por golpe
                GameManager.Instance?.AddScoreSilent(GameManager.Instance.pointsBasicHit);
                if (dmg >= 2)
                {
                    // Forzamos al enemigo a ir al suelo. 
                    enemy.GetComponent<Animator>().SetTrigger("Floor"); 
                }
                // Special1/Special2 quitan 8% UNA SOLA VEZ por swing, no por enemigo
                bool isSpecial = player.currentAttackDamage >= player.damageSpecial1;
                if (isSpecial && !player.specialSelfDamageApplied)
                {
                    player.specialSelfDamageApplied = true;
                    int selfDmg = Mathf.RoundToInt(player.GetMaxHP() * 0.08f);
                    player.TakeSelfDamage(selfDmg);
                }
            }
        }
    }
}