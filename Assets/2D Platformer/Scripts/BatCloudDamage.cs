using UnityEngine;
using System.Collections;

// Yêu cầu bắt buộc phải có Rigidbody2D và Collider2D (Is Trigger) trên Prefab
[RequireComponent(typeof(Rigidbody2D))] 
[RequireComponent(typeof(Collider2D))] 
public class BatCloudDamage : MonoBehaviour
{
    [Header("Cài đặt Sát thương")]
    [Tooltip("Sát thương gây ra cho Player mỗi lần tick.")]
    public int batDamage = 1; 
    
    [Tooltip("Khoảng thời gian (giây) giữa các lần gây sát thương DOT.")]
    public float damageTickRate = 0.5f; 
    
    [Tooltip("Thời gian tồn tại của đám mây dơi (Nên bằng độ dài Animation).")]
    public float damageDuration = 4f; 

    private Platformer.PlayerController playerControllerScript; 
    private bool playerIsInCloud = false;

    void Start()
    {
        // Khởi tạo Rigidbody (được set vận tốc từ EvilWizardBoss)
        if (TryGetComponent<Rigidbody2D>(out Rigidbody2D rb))
        {
             // Đảm bảo không bị trọng lực ảnh hưởng
            rb.gravityScale = 0f; 
        }
        
        // Bắt đầu vòng đời: Gây sát thương và tự hủy
        StartCoroutine(LifeCycleCoroutine());
    }

    // Coroutine quản lý thời gian sống và gây sát thương lặp lại
    private IEnumerator LifeCycleCoroutine()
    {
        float startTime = Time.time;
        float nextDamageTime = 0f;

        // Vòng lặp kéo dài bằng thời gian Animation dơi
        while (Time.time < startTime + damageDuration)
        {
            // Nếu Player ở trong vùng và đã đến lúc gây sát thương
            if (playerIsInCloud && Time.time >= nextDamageTime)
            {
                if (playerControllerScript != null)
                {
                    // GỌI HÀM SÁT THƯƠNG TRÊN PLAYER
                    playerControllerScript.TakeDamage(batDamage); 
                    Debug.Log($"🦇 Bat Cloud DOT HIT Player! Sát thương: {batDamage}");
                }
                
                nextDamageTime = Time.time + damageTickRate;
            }
            
            yield return null; 
        }

        // Hủy Prefab khi hết thời gian tồn tại
        Destroy(gameObject);
    }
    
    // --- Xử lý va chạm Trigger ---

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // Lấy PlayerController khi Player bước vào
            if (other.GetComponentInParent<Platformer.PlayerController>() != null)
            {
                 playerControllerScript = other.GetComponentInParent<Platformer.PlayerController>();
                 playerIsInCloud = true;
            }
        }
        // Hủy ngay nếu dơi chạm vào tường/đất
        else if (other.CompareTag("Ground") || other.CompareTag("Wall"))
        {
            Destroy(gameObject);
        }
    }
    
    void OnTriggerExit2D(Collider2D other)
    {
        // Khi Player rời khỏi vùng, dừng sát thương
        if (other.CompareTag("Player"))
        {
            playerIsInCloud = false;
            playerControllerScript = null; // Bỏ tham chiếu để tránh lỗi
        }
    }
}