using System.Collections;
using UnityEngine;

public class ItemSprite : MonoBehaviour, IPickable
{
    [SerializeField] private ItemType itemType;
    [SerializeField] private Sprite sprite;

    private void Start()
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (sprite == null)
        {
            sprite = spriteRenderer.sprite;
        }
    }

    public string GetItemType()
    {
        return itemType.ToString();
    }

    public Sprite GetItemSprite()
    {
        return sprite;
    }

    public void PickUp()
    {
        GetComponent<Collider>().enabled = false;
        StartCoroutine(DestroyAfterDelay(0.5f));
    }

    private IEnumerator DestroyAfterDelay(float delay)
    {
        float elapsedTime = 0f;
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        Color originalColor = spriteRenderer.color;
        while (elapsedTime < delay)
        {
            spriteRenderer.color = Color.Lerp(originalColor, Color.red, elapsedTime / delay);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }
}
