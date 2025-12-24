using UnityEngine;

public class DemonFireBall : MonoBehaviour
{
    // CÁC THUỘC TÍNH
    public int damage = 2;
    public float speed = 10f;
    public GameObject explosionPrefab;
    
    [HideInInspector] public Vector2 direction; 
    
    // *** THÊM MỚI: Âm thanh nổ ***
    [HideInInspector] public AudioClip explosionSound;

    [Header("Collision Settings")]
    // Layer nào FireBall chạm vào sẽ NỔ (Cần thiết lập trong Inspector)
    public LayerMask collisionMask; 
    public float lifetime = 5f; 

    private float startTime;

    void Start()
    {
        startTime = Time.time;
        // Tự hủy sau 5 giây để tránh lỗi nếu không va chạm
        Destroy(gameObject, lifetime); 
    }

    // XỬ LÝ DI CHUYỂN VÀ VA CHẠM (Dùng Raycast)
    void Update()
    {
        // 1. Tính khoảng cách sẽ di chuyển trong frame này
        float distanceToMove = speed * Time.deltaTime;
        
        // 2. Bắn Raycast để kiểm tra vật thể trong Collision Mask
        // Raycast kiểm tra từ vị trí hiện tại theo hướng (direction) và khoảng cách di chuyển (distanceToMove)
        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, distanceToMove, collisionMask);

        if (hit.collider != null)
        {
            // Va chạm với vật thể trong Layer đã chọn (Tường/Đất)
            Debug.Log($"FireBall hit: {hit.collider.name}");
            
            // Kích hoạt nổ ngay tại điểm va chạm
            Explode(hit.point); 
            
            Destroy(gameObject); // Hủy FireBall
            return;
        }

        // 3. Di chuyển FireBall nếu không va chạm
        transform.Translate(direction * distanceToMove);
    }
    
    // HÀM TẠO VỤ NỔ
    private void Explode(Vector2 explosionPosition)
    {
        if (explosionPrefab != null)
        {
            Instantiate(explosionPrefab, explosionPosition, Quaternion.identity);
        }
        
        // *** THÊM: Phát âm thanh nổ tại vị trí nổ ***
        if (explosionSound != null)
        {
            AudioSource.PlayClipAtPoint(explosionSound, explosionPosition);
        }
    }

    // XỬ LÝ VA CHẠM VỚI PLAYER (Trigger)
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Nếu chạm Player (hoặc vật thể có tag "Player")
        if (other.CompareTag("Player")) 
        {
            Platformer.PlayerController playerScript = other.GetComponent<Platformer.PlayerController>();
            if (playerScript != null)
            {
                playerScript.TakeDamage(damage);
            }

            Explode(transform.position); 
            Destroy(gameObject);
        }
    }
}