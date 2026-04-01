using UnityEngine;


/// SortByY — Ajusta el Order in Layer según la posición Y del personaje.
/// Quien está más abajo en pantalla (Y menor) se dibuja por encima.
///
/// SETUP:
///   Añadir a Player_Axel y a cada enemigo.
///   El SpriteRenderer debe estar en el mismo GameObject o en un hijo.
///
///   baseOrder    → Order in Layer base (mismo para todos los personajes, ej: 10)
///   sortingScale → cuánto afecta la Y al order (10 es un buen valor de inicio)

public class SortByY : MonoBehaviour
{
    [Tooltip("Order in Layer base — igual en todos los personajes")]
    [SerializeField] private int baseOrder = 10;

    [Tooltip("Multiplicador de Y para el sorting. Aumenta si los personajes se solapan mucho en Y")]
    [SerializeField] private float sortingScale = 10f;

    [SerializeField] private SpriteRenderer sr;

    private void Awake()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
    }

    private void LateUpdate()
    {
        if (sr == null) return;
        int order = baseOrder - Mathf.RoundToInt(transform.position.y * sortingScale);
        // Garantiza que el order nunca sea negativo — valores negativos pueden
        // ocultar el sprite detrás de elementos con order 0 del Sorting Layer
        sr.sortingOrder = Mathf.Max(order, 1);
    }
}