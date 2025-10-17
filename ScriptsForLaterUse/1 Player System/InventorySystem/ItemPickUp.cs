using MoreMountains.InventoryEngine;
using UnityEngine;

public class ItemPickUp : MonoBehaviour
{
    public int itemID;
    private Item item;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        item = ItemLoader.LoadItem(itemID);
        GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>(item.IconLocation);
    }

    public virtual void OnTriggerEnter2D(Collider2D collider)
    {
        // if what's colliding with the picker ain't a characterBehavior, we do nothing and exit
        if (!collider.CompareTag("Player"))
        {
            return;
        }


        bool wasAdded = collider.gameObject.GetComponent<CharacterInventoryBetter>().AddItemToInventory(item);
        if (wasAdded)
        {
            Destroy(gameObject);
        }
    }
}
