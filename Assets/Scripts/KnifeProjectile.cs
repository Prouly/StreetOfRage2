using System.Collections;
using UnityEngine;


/// KnifeProjectile  — Movimiento en línea recta en X hacia el player
///
/// SETUP prefab KnifeThrow:
///   - SpriteRenderer + Animator (rotación en bucle)
///   - CircleCollider2D: Is Trigger ON, Layer: EnemyAttack
///   - Este script
///   - Sin Rigidbody2D

public class KnifeProjectile : MonoBehaviour
{
    private float speed;
    private int   damage;
    private float directionX; // solo se mueve en X
    private bool  hasHit = false;

    public float lifetime = 5f;
    
    // Inicializa el cuchillo. Se mueve en línea recta en X hacia el player.

    public void Init(Transform playerTarget, float knifeSpeed, int knifeDamage)
    {
        speed     = knifeSpeed;
        damage    = knifeDamage;

        // Dirección en X únicamente — el cuchillo vuela horizontal
        if (playerTarget != null)
            directionX = playerTarget.position.x > transform.position.x ? 1f : -1f;
        else
            directionX = 1f;

        // Orienta el sprite
        transform.localScale = new Vector3(directionX, 1f, 1f);

        StartCoroutine(LifetimeRoutine());
    }

    private void Update()
    {
        if (hasHit) return;
        transform.Translate(Vector3.right * directionX * speed * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit) return;

        PlayerController pc = other.GetComponent<PlayerController>();
        if (pc != null)
        {
            hasHit = true;
            pc.TakeDamageAndKnockdown(damage, "Enemy_Jack (Knife)");
            AudioManager.Instance?.PlaySFX(AudioManager.Instance.hitAxel);
            Destroy(gameObject);
        }
    }

    private IEnumerator LifetimeRoutine()
    {
        yield return new WaitForSeconds(lifetime);
        if (!hasHit) Destroy(gameObject);
    }
}