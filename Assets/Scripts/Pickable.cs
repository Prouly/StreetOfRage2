using UnityEngine;

/// Pickable — Objeto recogible estilo SOR2
///
/// SETUP en Unity:
///   1. Crear prefab con SpriteRenderer + CircleCollider2D (Is Trigger = ON)
///   2. Añadir este script
///   3. Layer: "Pickable"
///   4. Asignar el sprite del objeto en SpriteRenderer
///
/// TIPOS:
///   Health → restaura vida al jugador
///   Points → suma puntos al GameManager

public class Pickable : MonoBehaviour
{
    public enum PickupType { Health, Points }
 
    [SerializeField] private PickupType pickupType = PickupType.Health;
 
    [Tooltip("Cantidad de vida o puntos que otorga")]
    [SerializeField] private int value = 50;
 
    public void Collect(PlayerController player)
    {
        switch (pickupType)
        {
            case PickupType.Health:
                player.Heal(value);
                break;
 
            case PickupType.Points:
                AudioManager.Instance?.PlaySFX(AudioManager.Instance.pickUpPoints);
                GameManager.Instance?.AddScoreSilent(value);
                Debug.Log($"[Pickable] Puntos: +{value}");
                break;
        }
        Destroy(gameObject);
    }
}