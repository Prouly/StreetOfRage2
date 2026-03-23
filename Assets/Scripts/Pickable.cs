using UnityEngine;

/// <summary>
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
/// </summary>
public class Pickable : MonoBehaviour
{
    public enum PickupType { Health, Points }

    [Header("Tipo")]
    public PickupType pickupType = PickupType.Health;

    [Header("Valor")]
    [Tooltip("Cantidad de vida o puntos que otorga")]
    public int value = 50;

    // El jugador llama a este método al recogerlo
    public void Collect(PlayerController player)
    {
        switch (pickupType)
        {
            case PickupType.Health:
                // Conecta aquí con tu sistema de vida cuando lo tengas
                // player.Heal(value);
                Debug.Log($"[Pickable] Vida recuperada: {value}");
                break;

            case PickupType.Points:
                // Conecta aquí con tu GameManager cuando lo tengas
                // GameManager.Instance.AddPoints(value);
                Debug.Log($"[Pickable] Puntos sumados: {value}");
                break;
        }

        Destroy(gameObject);
    }
}
