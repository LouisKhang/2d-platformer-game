using System.Collections;
using System.Collections.Generic; // Cần thêm namespace này cho List/Array
using UnityEngine;

namespace Platformer
{
    public class BreakableBlock : MonoBehaviour
    {
        [Header("Settings")]
        public int hitsToBreak = 3; 
        public Color flashColor = Color.red; 
        public float flashDuration = 0.1f; 
        public string explosionTrigger = "Explode"; 
        
        // 🔥 THUỘC TÍNH MỚI: DÙNG MẢNG ĐỂ CHỨA NHIỀU PREFAB VẬT PHẨM KHÁC NHAU
        [Header("Drop Items")]
        [Tooltip("Các Prefab vật phẩm có thể rơi ra (Coin, BombItem, Heart, v.v.)")]
        public GameObject[] dropPrefabs;
        
        [Tooltip("Có thả vật phẩm khi khối bị phá hủy hoàn toàn không?")]
        public bool dropOnBreak = true;

        // Biến private
        private int currentHits = 0;
        private Animator anim;
        private SpriteRenderer spriteRenderer;
        private Color originalColor;
        private bool isBroken = false;

        void Start()
        {
            anim = GetComponent<Animator>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            
            if (spriteRenderer != null)
            {
                originalColor = spriteRenderer.color;
            }
        }

        /// <summary>
        /// Hàm này được gọi khi khối bị trúng đạn (playerRb=null) hoặc bị HeadCheck.
        /// </summary>
        public void TakeHit(Rigidbody2D playerRb = null)
        {
            if (isBroken) return; 

            currentHits++;
            StartCoroutine(FlashBlock()); 

            if (currentHits >= hitsToBreak)
            {
                Break();
            }
            
            // Xử lý phản hồi vật lý khi Player đập đầu (chuyển từ PlayerController sang đây)
            if (playerRb != null && playerRb.velocity.y > 0.1f)
            {
                // Giảm vận tốc lên của Player về 0
                playerRb.velocity = new Vector2(playerRb.velocity.x, 0); 
            }
        }

        // Xử lý nổ và tạo vật phẩm
        private void Break()
        {
            isBroken = true;
            
            // 1. TẠO VẬT PHẨM (Nếu được cho phép)
            if (dropOnBreak)
            {
                DropRandomItem();
            }

            // 2. Kích hoạt Animation nổ
            if (anim != null)
            {
                anim.SetTrigger(explosionTrigger); 
            }
            
            // 3. Tắt tất cả Colliders và Sprite Renderer
            Collider2D[] colliders = GetComponents<Collider2D>();
            foreach(Collider2D col in colliders)
            {
                col.enabled = false;
            }
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = false; 
            }
            
            // 4. Xóa khối sau 0.5s
            Destroy(gameObject, 0.5f); 
        }

        /// <summary>
        /// Hàm tạo vật phẩm (có thể tùy chỉnh logic thả ngẫu nhiên ở đây)
        /// </summary>
        private void DropRandomItem()
        {
            if (dropPrefabs == null || dropPrefabs.Length == 0)
            {
                Debug.LogWarning("Không có Prefab vật phẩm nào được gán cho BreakableBlock này!");
                return;
            }

            // Chọn một Prefab ngẫu nhiên từ danh sách
            int randomIndex = Random.Range(0, dropPrefabs.Length);
            GameObject itemToDrop = dropPrefabs[randomIndex];

            // Tạo vật phẩm tại vị trí của khối
            if (itemToDrop != null)
            {
                // Thay thế Vector3.up bằng Quaternion.identity cho góc quay mặc định
                GameObject droppedItem = Instantiate(itemToDrop, transform.position, Quaternion.identity);
                
                // Thêm một lực đẩy nhẹ lên trên nếu vật phẩm có Rigidbody2D
                Rigidbody2D itemRb = droppedItem.GetComponent<Rigidbody2D>();
                if (itemRb != null)
                {
                    // Lực đẩy nhẹ để vật phẩm bay lên khi xuất hiện
                    itemRb.AddForce(Vector2.up * 3f, ForceMode2D.Impulse); 
                }
            }
        }
        
        // Coroutine để nháy màu đỏ
        private IEnumerator FlashBlock()
        {
            if (spriteRenderer == null) yield break;

            spriteRenderer.color = flashColor;
            yield return new WaitForSeconds(flashDuration);
            
            if (!isBroken)
            {
                spriteRenderer.color = originalColor;
            }
        }

        // Xử lý va chạm Trigger từ HeadCheck của Player
        private void OnTriggerEnter2D(Collider2D other)
        {
            // Kiểm tra HeadCheck của Player
            if (other.name == "HeadCheck" && !isBroken)
            {
                // Lấy Rigidbody của Player (nằm trên parent của HeadCheck)
                Rigidbody2D playerRb = other.GetComponentInParent<Rigidbody2D>();
                
                // Kiểm tra Player có đang đi lên không
                // Đã giảm ngưỡng xuống 0.01f để tăng độ nhạy va chạm với HeadCheck
                if (playerRb != null && playerRb.velocity.y > 0.01f)
                {
                    // Truyền Rigidbody để xử lý phản hồi vật lý
                    TakeHit(playerRb);
                }
            }
        }
    }
}