using UnityEngine;
using System.Collections;

namespace Platformer 
{
    public class PlayerDebuffController : MonoBehaviour
    {
        [Header("Slow Effect Settings")]
        public float defaultSlowFactor = 0.5f; 
        public float fixedStompSlowDuration = 5.0f; 
        public GameObject slowEffectPrefab; 

        // [BỔ SUNG] THUỘC TÍNH SOUND
        [Header("Audio")]
        public AudioClip slowAppliedSound; // Âm thanh khi Slow/Frozen được áp dụng
        // Chúng ta sẽ dùng AudioSource trên Player hoặc phát tạm thời, không cần AudioSource riêng ở đây
        // [KẾT THÚC BỔ SUNG]

        private PlayerController playerController;
        private Rigidbody2D rb;
        
        private Coroutine slowCoroutine; 
        
        private int playerLayer; 
        private int bossLayer; 
        
        public bool IsSlowed { get; private set; } = false; 
        private GameObject currentSlowEffect;

        void Start()
        {
            playerController = GetComponent<PlayerController>();
            rb = GetComponent<Rigidbody2D>();
            
            playerLayer = gameObject.layer; 
            bossLayer = LayerMask.NameToLayer("Enemy"); 

            if (playerLayer == -1 || bossLayer == -1)
            {
                Debug.LogError("Player or Enemy/Boss Layer not found! Check Project Settings.");
            }
                
            Physics2D.IgnoreLayerCollision(playerLayer, bossLayer, false);
            
            if (rb != null)
                rb.constraints = RigidbodyConstraints2D.FreezeRotation; 
        }

        // HÀM REFRESH: Luôn áp dụng lại hiệu ứng Slow (dừng cũ, chạy mới)
        public void ForceApplySlow(float duration, float factor, MinotaurController minotaur = null)
        {
            // 1. Kiểm tra trạng thái
            if (playerController != null && playerController.deathState) return; 
            
            float finalDuration = duration;

            // 2. LOGIC GHI ĐÈ/REFRESH
            // Dừng Coroutine Slow cũ (nếu có)
            if (slowCoroutine != null)
            {
                StopCoroutine(slowCoroutine);
                // Dọn dẹp hiệu ứng cũ (reset tốc độ và hủy hình ảnh) trước khi áp dụng cái mới
                CleanupBeforeNewSlow(); 
            }
            
            // Nếu là Stomp của Minotaur, cố định thời gian Slow
            if (minotaur != null)
            {
                finalDuration = fixedStompSlowDuration; 
            }
            
            // [BỔ SUNG] PHÁT ÂM THANH NGAY KHI HIỆU ỨNG ĐƯỢC ÁP DỤNG
            PlaySlowAppliedSound();
            // [KẾT THÚC BỔ SUNG]

            // 3. Bắt đầu Coroutine Slow mới
            slowCoroutine = StartCoroutine(SlowRoutine(finalDuration, factor));
        }
        
        // [BỔ SUNG] HÀM PHÁT ÂM THANH SLOW/FROZEN
        private void PlaySlowAppliedSound()
        {
            if (slowAppliedSound != null)
            {
                // Sử dụng AudioSource.PlayClipAtPoint để phát âm thanh 3D tại vị trí của Player
                // Hoặc bạn có thể dùng GetComponent<AudioSource>().PlayOneShot(slowAppliedSound);
                // nếu Player đã có sẵn AudioSource và bạn không muốn tạo AudioSource tạm thời.
                
                // Cách 1: PlayClipAtPoint (tạo AudioSource tạm thời, phù hợp cho phản hồi tức thì)
                AudioSource.PlayClipAtPoint(slowAppliedSound, transform.position, 1.0f); 
            }
        }
        // [KẾT THÚC BỔ SUNG]

        // Hàm này được gọi để reset trạng thái (move speed) ngay lập tức
        private void CleanupBeforeNewSlow()
        {
            if (playerController != null) 
            {
                playerController.RemoveSlowFactor(); // Đảm bảo tốc độ được reset về gốc
            }
            EndSlowCleanup(); // Hủy hiệu ứng hình ảnh
            IsSlowed = false;
        }

        private IEnumerator SlowRoutine(float duration, float factor)
        {
            IsSlowed = true;
            
            // 1. Áp dụng Slow Factor
            if (playerController != null) 
            {
                playerController.ApplySlowFactor(factor);
            }
            
            // 2. Kích hoạt hiệu ứng hình ảnh
            if (slowEffectPrefab != null)
            {
                if (currentSlowEffect != null)
                {
                    Destroy(currentSlowEffect);
                }
                
                Vector3 spawnPos = transform.position + new Vector3(0, 1.5f, 0); 
                currentSlowEffect = Instantiate(slowEffectPrefab, spawnPos, Quaternion.identity, transform); 
            }
            
            yield return new WaitForSeconds(duration);

            // 3. Kết thúc Slow
            EndSlow();
            slowCoroutine = null;
        }
        
        private void EndSlow()
        {
            if (!IsSlowed) return;
            
            IsSlowed = false;
            
            // Reset Slow Factor
            if (playerController != null) playerController.RemoveSlowFactor();
            
            // BẬT LẠI VA CHẠM (Đảm bảo)
            if (playerLayer != -1 && bossLayer != -1)
            {
                Physics2D.IgnoreLayerCollision(playerLayer, bossLayer, false); 
            }

            // Hủy hiệu ứng hình ảnh
            EndSlowCleanup();
        }
        
        // Hàm dọn dẹp riêng biệt (chỉ hủy hiệu ứng hình ảnh)
        private void EndSlowCleanup()
        {
            if (currentSlowEffect != null)
            {
                Destroy(currentSlowEffect);
                currentSlowEffect = null;
            }
        }


        void OnDestroy()
        {
            if (currentSlowEffect != null)
                Destroy(currentSlowEffect);
                
            // Đảm bảo va chạm được BẬT lại khi đối tượng Player bị hủy
            if (bossLayer != -1 && playerLayer != -1)
            {
                Physics2D.IgnoreLayerCollision(playerLayer, bossLayer, false);
            }
        }
    }
}