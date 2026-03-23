using UnityEngine;

public interface IPickable
{
    // Get the type of the item (e.g., health potion, weapon, etc.)
    string GetItemType();
     
    // Get the sprite of the item to represent it in the inventory
    Sprite GetItemSprite();
     
    // Method to pick up the item
    void PickUp();
}
