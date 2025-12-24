using UnityEngine;
using System.Collections;
// Thay thế bằng script Player của bạn
// using Platformer; 

[RequireComponent(typeof(Rigidbody2D))] 
public class StoneEffect : MonoBehaviour
{
    private int damageToApply = 0; 
    
    // ⭐ THÊM FIELD CHO ÂM THANH
    [Header("Audio Settings")]
    public AudioClip stoneEffectSound; // Clip âm thanh hóa đá
    private AudioSource audioSource;
    
    // ⭐️ Thay Platformer.PlayerController bằng tên script Player của bạn
    private Platformer.PlayerController playerControllerScript; 
    
    void Awake() 
    {
        // 1. TÌM VÀ CẤU HÌNH AUDIOSOURCE
        // Đảm bảo có AudioSource trên Prefab này, nếu chưa có thì thêm vào
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false; // Tắt tự động phát
        }
    }
    
    // Hàm được gọi từ MedusaAI để thiết lập
    public void Initialize(float duration, int damage)
    {
        damageToApply = damage; 
        
        // 2. Vô hiệu hóa vật lý để Prefab dính vào Player
        if (TryGetComponent<Rigidbody2D>(out Rigidbody2D rb))
        {
            rb.simulated = false;
        }
        
        // 3. Tìm script PlayerController trên đối tượng Parent (Player)
        if (transform.parent != null && transform.parent.TryGetComponent<Platformer.PlayerController>(out playerControllerScript))
        {
             Debug.Log("StoneEffect: Tìm thấy PlayerController.");
        }
        else
        {
             Debug.LogError("StoneEffect: KHÔNG tìm thấy Platformer.PlayerController trên Parent. Sát thương sẽ không được áp dụng.");
        }
        
        // ⭐ 4. PHÁT ÂM THANH
        if (audioSource != null && stoneEffectSound != null)
        {
            audioSource.PlayOneShot(stoneEffectSound);
        }
        else
        {
            Debug.LogWarning("StoneEffect: Không tìm thấy AudioSource hoặc AudioClip để phát.");
        }
        
        // (Phần Animation như bạn đã có)
    }

    // ⭐️ HÀM NÀY PHẢI ĐƯỢC GỌI BẰNG ANIMATION EVENT TỪ CLIP VỠ RA
    public void ApplyDamageAndDestroy()
    {
        // --- GÂY SÁT THƯƠNG VÀ KẾT THÚC HIỆU ỨNG ---
        
        if (playerControllerScript != null)
        {
            // ⭐️ GỌI HÀM SÁT THƯƠNG TRÊN PLAYER
            playerControllerScript.TakeDamage(damageToApply); 
            Debug.Log($"Player mất {damageToApply} tim do hiệu ứng Hóa Đá (Animation Event) kết thúc.");
        }
        
        // Hủy Prefab
        Destroy(gameObject);
    }
}