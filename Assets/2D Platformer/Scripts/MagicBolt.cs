using UnityEngine;
using System.Collections;

public class MagicBolt : MonoBehaviour
{
    public float speed = 10f;
    [Tooltip("Sát thương gây ra cho Player (Đã đặt là 2).")]
    public int damageToPlayer = 2;
    
    [Header("Sound Effects")]
    public AudioClip hitSound;              // Âm thanh khi đạn nổ/va chạm
    [Range(0f, 1f)]
    public float soundVolume = 0.7f;
    
    private Rigidbody2D rb;
    private Animator anim;
    private bool hasHit = false;
    private AudioSource audioSource;

    // Layer của HeadCheck (Layer 9) và Activation Area (Layer 10)
    private const int LAYER_ENEMY_INDEX_9 = 9; 
    private const int LAYER_ENEMY_INDEX_10 = 10; 

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        
        // Khởi tạo AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        audioSource.volume = soundVolume;
        
        if (transform.localScale.x < 1.5f || transform.localScale.y < 1.5f)
        {
            transform.localScale = new Vector3(2f, 2f, 1f); 
        }

        Destroy(gameObject, 5f);
    }

    public void Launch(Vector2 direction)
    {
        if (rb == null)
        {
            Debug.LogError("LỖI MAGICBOLT: Thiếu Rigidbody2D!");
            Destroy(gameObject); 
            return;
        }

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.velocity = direction * speed;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit) return;

        // Bỏ qua va chạm với Layer 9 và Layer 10 trừ khi đó là Player
        bool isIgnoredLayer = other.gameObject.layer == LAYER_ENEMY_INDEX_9 || other.gameObject.layer == LAYER_ENEMY_INDEX_10;
        
        if (other.CompareTag("EvilWizard") || other.CompareTag("FirePoint") || isIgnoredLayer) 
        {
            if (!other.CompareTag("Player"))
            {
                return; 
            }
        }
        
        // ------------------ LOGIC VA CHẠM VỚI PLAYER ------------------
        if (other.CompareTag("Player"))
        {
            var player = other.GetComponentInParent<Platformer.PlayerController>();
            
            if (player != null)
            {
                // GỌI HÀM TRỪ MÁU PLAYER (BẮT BUỘC PHẢI SỬ DỤNG GIÁ TRỊ damageToPlayer)
                player.TakeDamage(damageToPlayer); 
                Debug.Log($"💥 Magic Bolt HIT Player! Sát thương: {damageToPlayer}");
            }
            
            Explode();
        }
        // ------------------ LOGIC VA CHẠM VỚI MAP/VẬT THỂ KHÁC ------------------
        else 
        {
            Explode(); 
        }
    }

    void Explode()
    {
        if (hasHit) return;
        
        hasHit = true;
        
        // Phát âm thanh nổ
        PlaySound(hitSound);
        
        if (rb != null) rb.velocity = Vector2.zero; 
        
        Collider2D col = GetComponent<Collider2D>();
        if(col != null) col.enabled = false; 

        if (anim != null)
        {
            anim.SetTrigger("OnHit"); 
        } 
        
        // Hủy đối tượng sau khi animation nổ kết thúc
        Destroy(gameObject, 0.3f); 
    }

    // Hàm phát âm thanh
    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip, soundVolume);
        }
    }
}