using UnityEngine;
using System.Collections;

namespace Platformer
{
    public class EnemyAI : MonoBehaviour
    {
        public float moveSpeed = 1f;
        public LayerMask ground;
        public LayerMask wall;
        public Collider2D triggerCollider; 

        private Rigidbody2D rb;
        private Animator animator;
        private SpriteRenderer spriteRenderer;

        [SerializeField] private int health = 3; 
        private bool isDead = false;
        
        // <--- ĐÃ THÊM: Cờ bất tử tạm thời để ngăn chặn va chạm ba lần (3 Collider)
        private bool isInvulnerable = false; 

        [Header("Flash Settings")]
        public Color hurtColor = Color.red;
        private Color originalColor;
        public float flashDuration = 0.1f;

        void Start()
        {
            rb = GetComponent<Rigidbody2D>();
            animator = GetComponent<Animator>();
            spriteRenderer = GetComponent<SpriteRenderer>();

            if (spriteRenderer != null)
                originalColor = spriteRenderer.color;
        }

        void Update()
        {
            if (!isDead && rb != null)
                rb.velocity = new Vector2(moveSpeed, rb.velocity.y);
        }

        void FixedUpdate()
        {
            if (!isDead && triggerCollider != null)
            {
                if (!triggerCollider.IsTouchingLayers(ground) || triggerCollider.IsTouchingLayers(wall))
                    Flip();
            }
        }

        private void Flip()
        {
            transform.localScale = new Vector2(-transform.localScale.x, transform.localScale.y);
            moveSpeed *= -1;
        }

        public void TakeDamage(int damage)
        {
            // BỎ QUA sát thương nếu đã chết hoặc đang bất tử (do bị hit 3 lần)
            if (isDead || isInvulnerable) return; 

            isInvulnerable = true; // Kích hoạt bất tử ngay khi nhận hit

            health -= damage;
            Debug.Log($"EnemyAI {name} trúng đòn! -{damage} máu | Còn lại: {health}");
            
            StartCoroutine(FlashRedAndResetInvulnerability());

            if (health <= 0)
                Die();
            else if (animator != null)
                animator.SetTrigger("Hurt");
        }

        // HÀM MỚI: Coroutine xử lý flash và reset bất tử
        private IEnumerator FlashRedAndResetInvulnerability()
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = hurtColor;
                yield return new WaitForSeconds(flashDuration);
                spriteRenderer.color = originalColor;
            }
            isInvulnerable = false; // Tắt cờ bất tử sau khi hết thời gian flash
        }

        public void Die()
        {
            // SỬA: Đảm bảo isDead được đặt ở đầu
            if (isDead) return;
            isDead = true;

            if (animator != null)
                animator.SetTrigger("IsDead");

            if (rb != null)
            {
                rb.velocity = Vector2.zero;
                rb.simulated = false;
            }

            Collider2D[] cols = GetComponentsInChildren<Collider2D>();
            foreach (Collider2D col in cols)
                col.enabled = false;

            Destroy(gameObject, 0.5f);
        }

        // SỬA: Xóa logic xử lý chém khỏi EnemyAI.cs để tránh xung đột
        private void OnTriggerEnter2D(Collider2D collision)
        {
            // Logic xử lý chém ĐÃ ĐƯỢC CHUYỂN HOÀN TOÀN sang SlashHitbox.cs.
            // Nếu bạn có va chạm khác cần xử lý, hãy thêm vào đây và áp dụng logic isInvulnerable.
        }
    }
}