using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// CinemachinePositionClamp — Extension para Cinemachine Camera
///
/// SETUP:
///   1. Añadir este script a CameraCinemachine (el mismo GO que tiene CinemachineCamera)
///   2. En CinemachineCamera → Add Extension → seleccionar este componente
///      (o simplemente añadirlo como componente — Cinemachine lo detecta automáticamente)
///   3. Eliminar CinemachineConfiner2D si lo tienes
///   4. Asignar el Player en el campo Player del Inspector
///
/// CALIBRACIÓN:
///   - Activa Gizmos en Scene View
///   - Verás rectángulos verdes por zona y líneas amarillas para límites X
///   - Entra en Play, mueve al jugador a cada esquina del escenario
///     y anota las coordenadas X e Y — esos son tus valores
/// </summary>
public class CinemachinePositionClamp : CinemachineExtension
{
    [Header("Target")]
    public Transform player;

    [Header("Límites X")]
    public float minX = -8f;
    public float maxX = 100f;

    [Header("Zonas Y (una por sección del nivel)")]
    public CameraZone[] zones;

    [System.Serializable]
    public struct CameraZone
    {
        [Tooltip("X donde empieza esta zona")]
        public float xStart;
        [Tooltip("X donde termina esta zona")]
        public float xEnd;
        [Tooltip("Y mínima de la cámara en esta zona")]
        public float minY;
        [Tooltip("Y máxima de la cámara en esta zona")]
        public float maxY;
    }

    // ── CinemachineExtension override ─────────────────────────
    // PostPipelineStageCallback se llama DESPUÉS de que Cinemachine
    // calcula la posición — aquí aplicamos el clamp encima.

    protected override void PostPipelineStageCallback(
        CinemachineVirtualCameraBase vcam,
        CinemachineCore.Stage stage,
        ref CameraState state,
        float deltaTime)
    {
        // Solo actuamos en la etapa final de posición
        if (stage != CinemachineCore.Stage.Finalize) return;
        if (player == null) return;

        // Obtener la cámara para calcular half-sizes
        Camera cam = vcam.GetComponentInChildren<Camera>();
        if (cam == null) cam = Camera.main;

        float halfH = cam != null ? cam.orthographicSize : 3f;
        float halfW = cam != null ? halfH * cam.aspect   : 5f;

        // Límites Y según la X actual del jugador
        float camMinY, camMaxY;
        GetYLimitsAtX(player.position.x, out camMinY, out camMaxY);

        // Clamp de posición
        Vector3 pos = state.RawPosition;
        pos.x = Mathf.Clamp(pos.x, minX + halfW,  maxX - halfW);
        pos.y = Mathf.Clamp(pos.y, camMinY + halfH, camMaxY - halfH);

        state.RawPosition = pos;
    }

    // ── Interpolación de límites Y ────────────────────────────

    private void GetYLimitsAtX(float x, out float minY, out float maxY)
    {
        if (zones == null || zones.Length == 0)
        {
            minY = -100f; maxY = 100f; return;
        }

        if (x <= zones[0].xStart)
        {
            minY = zones[0].minY; maxY = zones[0].maxY; return;
        }

        if (x >= zones[zones.Length - 1].xEnd)
        {
            minY = zones[zones.Length - 1].minY;
            maxY = zones[zones.Length - 1].maxY;
            return;
        }

        for (int i = 0; i < zones.Length; i++)
        {
            if (x >= zones[i].xStart && x <= zones[i].xEnd)
            {
                minY = zones[i].minY; maxY = zones[i].maxY; return;
            }

            if (i < zones.Length - 1)
            {
                var next = zones[i + 1];
                if (x > zones[i].xEnd && x < next.xStart)
                {
                    float t = Mathf.InverseLerp(zones[i].xEnd, next.xStart, x);
                    minY = Mathf.Lerp(zones[i].minY, next.minY, t);
                    maxY = Mathf.Lerp(zones[i].maxY, next.maxY, t);
                    return;
                }
            }
        }

        minY = zones[zones.Length - 1].minY;
        maxY = zones[zones.Length - 1].maxY;
    }

    // ── Gizmos ────────────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        // Límites X (amarillo)
        Gizmos.color = new Color(1f, 0.9f, 0f, 0.8f);
        Gizmos.DrawLine(new Vector3(minX, -50f, 0f), new Vector3(minX, 50f, 0f));
        Gizmos.DrawLine(new Vector3(maxX, -50f, 0f), new Vector3(maxX, 50f, 0f));

        if (zones == null) return;

        for (int i = 0; i < zones.Length; i++)
        {
            var z = zones[i];

            // Rectángulo de zona (verde)
            Gizmos.color = new Color(0f, 1f, 0.4f, 0.5f);
            Vector3 center = new Vector3(
                (z.xStart + z.xEnd) / 2f,
                (z.minY   + z.maxY) / 2f, 0f);
            Vector3 size = new Vector3(
                z.xEnd - z.xStart,
                z.maxY - z.minY, 0f);
            Gizmos.DrawWireCube(center, size);

            // Transición a siguiente zona (cian)
            if (i < zones.Length - 1)
            {
                Gizmos.color = new Color(0f, 0.8f, 1f, 0.6f);
                Gizmos.DrawLine(
                    new Vector3(z.xEnd,             (z.minY   + z.maxY)                         / 2f, 0f),
                    new Vector3(zones[i+1].xStart,  (zones[i+1].minY + zones[i+1].maxY) / 2f, 0f)
                );
            }
        }
    }
}