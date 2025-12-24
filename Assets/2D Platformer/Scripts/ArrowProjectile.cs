using UnityEngine;
using Platformer; 

public class ArrowProjectile : MonoBehaviour
{
    // CÀI ĐẶT CÓ THỂ CHỈNH TRONG INSPECTOR
    [Header("Movement & Damage")]
    public float speed = 15f;           // Tốc độ di chuyển
    public int damage = 1; 

    [Header("Lifetime Settings")]
    public float maxLifetime = 5f;      
    
    // BIẾN NỘI BỘ: Nhận Vector2 Vận tốc từ ArrowTurret
    [HideInInspector] public Vector2 velocityVector; 
    private Rigidbody2D rb; 

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        if (rb == null)
        {
            Debug.LogError("FATAL ERROR: Rigidbody2D missing on Arrow Prefab.");
            return;
        }
        
        // Áp dụng vận tốc bay ngay lập tức
        rb.velocity = velocityVector;
        
        Destroy(gameObject, maxLifetime); 
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Platformer.PlayerController playerScript = other.GetComponent<Platformer.PlayerController>();

            if (playerScript != null)
            {
                playerScript.TakeDamage(damage); 
            }
            Destroy(gameObject);
        } 
        else if (!other.isTrigger) 
        {
             Destroy(gameObject);
        }
    }
}