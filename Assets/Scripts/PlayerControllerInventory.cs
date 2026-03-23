using UnityEngine;


public class PlayerControllerInventory : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Detect Pickup collision with the pickup objects to trigger the pickup action
        if (other.CompareTag("Pickup"))
        {
            // Trigger Debug Message
            Debug.Log("Picked up: " + other.gameObject.name);
            // Get Item Type from the pickup object (assuming the pickup object has a component that stores the item type, e.g., PickupItem)
            Item item = other.GetComponent<Item>();
            if (item == null)                
            {
                Debug.LogError("Pickup object does not have an Item component!");
                return;
            }
            // Add the item to the inventory 
            GameManager.Instance.GetInventory().AddItem(item.itemType);

            Destroy(other.gameObject); // Example: destroy the pickup object after picking it up
        }
    }
}
