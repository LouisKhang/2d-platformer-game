using UnityEngine;
using Platformer;


public class PickupItem : MonoBehaviour
{
    public enum ItemType { Coin, Heart, Bomb }
    public ItemType itemType;

    public AudioClip pickupSound;
    public AudioSource audioSourcePrefab; // optional: audio source rời

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        GameManager gm = FindObjectOfType<GameManager>();

        if (gm != null)
        {
            switch(itemType)
            {
                case ItemType.Coin:  gm.AddCoin(1); break;
                case ItemType.Heart: gm.AddHeart(); break;
                case ItemType.Bomb:  gm.AddBomb(1); break;
            }
        }

        // Play sound
        if (pickupSound != null)
            AudioSource.PlayClipAtPoint(pickupSound, transform.position);

        Destroy(gameObject);
    }
}
