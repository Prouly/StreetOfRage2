using UnityEngine;

public class CameraFollow2D : MonoBehaviour
{
    [SerializeField] private Transform playerTransform;
    
    [SerializeField] private bool freezeY = true;
    [SerializeField] private float offsetX = 2f;
    [SerializeField] private float horizonY = 0f;

    private void Start()
    {
        if (playerTransform == null)
        {
            playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
        }
            
    }

    private void LateUpdate()
    {
        Vector3 newPosition = transform.position;
        float distanceX = playerTransform.position.x - newPosition.x;

        if (Mathf.Abs(distanceX) > offsetX)
        {
            newPosition.x = playerTransform.position.x - Mathf.Sign(distanceX) * offsetX;
        }

        if (!freezeY)
        {
            newPosition.y = playerTransform.position.y;
        }

        if (newPosition.y < horizonY)
        {
            newPosition.y = horizonY;
        }
        transform.position = newPosition;
    }
    private void OnDrawGizmos()
    {
        if (playerTransform != null)
        {
            // draw the player's position as a red sphere and the camera's position as a blue sphere
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(playerTransform.position, 0.1f);
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(transform.position, 0.1f);
            
            // draw a line between the camera and the player
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, playerTransform.position);
            
            if (offsetX != 0)
            {
                // draw the camera offset
                Gizmos.color = Color.red;
                Gizmos.DrawLine(
                    new Vector3(transform.position.x - offsetX, transform.position.y - 5f, 0f),
                    new Vector3(transform.position.x - offsetX, transform.position.y + 5f, 0f));
                Gizmos.DrawLine(
                    new Vector3(transform.position.x + offsetX, transform.position.y - 5f, 0f),
                    new Vector3(transform.position.x + offsetX, transform.position.y + 5f, 0f));
            }
        }
    }
}