using UnityEngine;
using UnityEngine.UI;

public class InventoryPicker : MonoBehaviour
{
    [SerializeField] private int maxNumberOfItems = 5; // TODO: adapt width of the shop to this value
    [SerializeField] private GameObject itemPrefab;

    // Array of items (Item) in the inventory
    private IPickable[] _items;
    private int _numItems = 0;

    private void Start()
    {
        _items = new IPickable[maxNumberOfItems];
    }

    public void AddItem(IPickable pickable)
    {
        if (_numItems < maxNumberOfItems)
        {
            // add the item to the inventory
            _items[_numItems] = pickable;
            _numItems++;

            // draw the item in the inventory (instantiate a new item prefab and set its sprite to the sprite of the item)
            Transform inventoryTransform = GameObject.FindGameObjectWithTag("Inventory").transform;
            GameObject item = Instantiate(itemPrefab, Vector2.zero, Quaternion.identity, inventoryTransform);
            item.GetComponent<Image>().sprite = pickable.GetItemSprite();
        }
    }
}