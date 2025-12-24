using UnityEngine;
using System.Collections;

public class SkullwolfAI : MonoBehaviour
{
    // CÁC THAM SỐ CÓ THỂ ĐIỀU CHỈNH TRONG UNITY INSPECTOR
    
    [Header("Health")]
    public int maxHealth = 8;
    private int currentHealth;

    [Header("Movement Stats")]
    public float moveSpeed = 3f;
    
    // *** THAM SỐ TẦM NHÌN HÌNH CHỮ NHẬT TOÀN CẢNH ***
    [Header("Sight Box (Toàn Cảnh)")]
    // Đặt X là tổng chiều dài Box (Ví dụ 12f cho 6m mỗi bên)
    public Vector2 sightBoxSize = new Vector2(12f, 1.5f); 
    public float sightBoxOffset = 0.5f; // Khoảng dịch chuyển lên/xuống của box
    public LayerMask playerLayer; // Layer của Player
    // *******************************************

    [Header("Patrol & Ground Check")]
    public LayerMask groundLayer; 
    public LayerMask wallLayer; 
    public Collider2D groundWallCheckTrigger; 
    public Collider2D bodyCollider; 

    // THỜI GIAN
    private const float MOVE_DURATION = 10f; 
    private const float WAIT_DURATION = 5f;  
    private const float DEATH_ANIM_DURATION = 0.67f; 

    // COMPONENTS VÀ TRẠNG THÁI NỘI BỘ
    private Animator anim;
    private Rigidbody2D rb;
    private Transform player; 

    private int moveDirection = -1; // -1 = trái, 1 = phải (hướng di chuyển)
    
    private bool isPatrolling = true;
    private bool isDead = false;
    private bool isWaiting = false;    
    private bool isGrounded = false; 
    private bool isFlipping = false;    

    private bool isReturningToLKP = false;  
    private Vector2 lastKnownPlayerPosition; 

    private float currentTimer = 0f; 

    void Start()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<Collider2D>(); 
        
        GameObject playerObject = GameObject.FindWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
        }

        currentHealth = maxHealth;
        Flip(); 
    }

    void Update()
    {
        isGrounded = bodyCollider.IsTouchingLayers(groundLayer);

        // *** FIX LỖI: Ngăn animation chết bị ghi đè ***
        if (isDead) 
        {
            rb.velocity = Vector2.zero;
            return; 
        }

        if (isFlipping)
        {
            rb.velocity = Vector2.zero;
            return;
        }
        
        // --- Logic Quyết định Hành Động ---
        bool playerVisible = false;

        if (player != null)
        {
            // *** 1. LOGIC TẦM NHÌN HÌNH CHỮ NHẬT TOÀN CẢNH (Đã sửa lỗi) ***
            
            // Box Center ở chính giữa quái vật (Không phụ thuộc hướng quay mặt)
            Vector2 boxCenter = new Vector2(
                transform.position.x, 
                transform.position.y + sightBoxOffset
            );
            
            // Thực hiện Box Check
            Collider2D[] hits = Physics2D.OverlapBoxAll(
                boxCenter, 
                sightBoxSize, 
                0f, 
                playerLayer
            );

            playerVisible = (hits.Length > 0);
            
            // Nếu thấy Player trong tầm nhìn, buộc quái vật quay mặt về Player
            if (playerVisible)
            {
                float direction = (player.position.x > transform.position.x) ? 1f : -1f;
                if (moveDirection != (int)direction)
                {
                    moveDirection = (int)direction;
                    Flip(); 
                }
            }
        }

        // 2. Quyết định trạng thái
        if (playerVisible)
        {
            // THẤY PLAYER: CHUYỂN SANG TRUY ĐUỔI NGAY LẬP TỨC
            isPatrolling = false;
            isReturningToLKP = false;
            isWaiting = false; // <-- Đảm bảo thoát khỏi trạng thái chờ 5s
            
            lastKnownPlayerPosition = player.position; 
            ChasePlayer(); 
        }
        else
        {
            // MẤT DẤU HOẶC PLAYER Ở NGOÀI TẦM NHÌN
            if (isReturningToLKP)
            {
                ReturnToLKP();
            }
            else if (!isPatrolling) 
            {
                // Vừa truy đuổi xong và mất dấu -> Chờ 5s tại vị trí cuối cùng 
                isReturningToLKP = true;
                isWaiting = true; 
                currentTimer = 0f;
                rb.velocity = Vector2.zero;
                
                ReturnToLKP(); 
            }
            else
            {
                // ĐANG TUẦN TRA
                isPatrolling = true;
                Patrol(); 
            }
        }
        
        // Cập nhật animation di chuyển 
        anim.SetFloat("Speed", Mathf.Abs(rb.velocity.x));
    }

    void FixedUpdate()
    {
        if (!isDead && !isWaiting && isGrounded && !isFlipping)
        {
            CheckGroundAndWall();
        }
    }

    // --- LOGIC TUẦN TRA & NGHỈ NGƠI ---

    void Patrol()
    {
        if (isWaiting)
        {
            rb.velocity = Vector2.zero;
            currentTimer += Time.deltaTime;
            
            if (currentTimer >= WAIT_DURATION) 
            {
                isWaiting = false;
                isPatrolling = true;
                currentTimer = 0f;
                
                moveDirection *= -1; 
                StartCoroutine(SafeFlip()); 
            }
            return;
        }

        currentTimer += Time.deltaTime;
        if (currentTimer >= MOVE_DURATION)
        {
            isWaiting = true; 
            currentTimer = 0f; 
            rb.velocity = Vector2.zero; 
            return;
        }

        Vector2 targetVelocity = new Vector2(moveDirection * moveSpeed, rb.velocity.y);
        rb.velocity = targetVelocity;
    }
    
    // --- LOGIC QUAY LẠI VỊ TRÍ CUỐI CÙNG ---

    void ReturnToLKP()
    {
        if (isWaiting) 
        {
            Patrol(); 
            return;
        }
        
        float distanceX = Mathf.Abs(transform.position.x - lastKnownPlayerPosition.x);

        if (distanceX <= 0.2f) 
        {
            isReturningToLKP = false; 
            isWaiting = true;         
            currentTimer = 0f;
            rb.velocity = Vector2.zero;
            
            int direction = (lastKnownPlayerPosition.x > transform.position.x) ? 1 : -1;
            if (moveDirection != direction)
            {
                moveDirection = direction;
                Flip();
            }
            Patrol(); 
        }
        else
        {
            float direction = (lastKnownPlayerPosition.x > transform.position.x) ? 1f : -1f;
            
            if (moveDirection != (int)direction)
            {
                moveDirection = (int)direction;
                Flip();
            }

            Vector2 targetVelocity = new Vector2(direction * moveSpeed, rb.velocity.y);
            rb.velocity = targetVelocity;
        }
    }

    // --- LOGIC KIỂM TRA MẶT ĐẤT/TƯỜNG ---

    void CheckGroundAndWall()
    {
        if (groundWallCheckTrigger == null) return;
        
        bool touchingWall = groundWallCheckTrigger.IsTouchingLayers(wallLayer);
        bool onGround = groundWallCheckTrigger.IsTouchingLayers(groundLayer);

        if (touchingWall || !onGround)
        {
            if (!isWaiting)
            {
                isReturningToLKP = false; 
                isPatrolling = true; 
                isWaiting = true;       
                currentTimer = 0f;
                rb.velocity = Vector2.zero;
            }
            
            return; 
        }
    }

    // --- LOGIC TRUY ĐUỔI ---

    void ChasePlayer()
    {
        if (isWaiting)
        {
            Patrol(); 
            return;
        }
        
        float direction = (player.position.x > transform.position.x) ? 1f : -1f;
        
        if (moveDirection != (int)direction)
        {
            moveDirection = (int)direction;
            Flip();
        }

        Vector2 targetVelocity = new Vector2(direction * moveSpeed, rb.velocity.y);
        rb.velocity = targetVelocity;
    }
    
    // --- LOGIC MÁU & SÁT THƯƠNG ---

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        Debug.Log($"Skullwolf received {damage} damage. Current HP: {currentHealth}");
        
        anim.SetTrigger("Hit"); 

        if (player != null)
        {
            // Buộc chuyển sang chế độ truy đuổi ngay lập tức khi bị tấn công
            isPatrolling = false;
            isReturningToLKP = false;
            isWaiting = false; 
            lastKnownPlayerPosition = player.position;

            float direction = (player.position.x > transform.position.x) ? 1f : -1f;
            if (moveDirection != (int)direction)
            {
                moveDirection = (int)direction;
                Flip();
            }
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // --- LOGIC CHẾT ---
    public void Die()
    {
        if (isDead) return;
        isDead = true;
        
        anim.SetTrigger("Die"); 
        
        rb.velocity = Vector2.zero;
        if (rb != null) rb.simulated = false; 

        Collider2D[] cols = GetComponentsInChildren<Collider2D>();
        foreach (Collider2D col in cols) col.enabled = false;
        
        Destroy(gameObject, DEATH_ANIM_DURATION + 0.13f); 
    }

    // --- CÁC HÀM PHỤ TRỢ ---
    
    private void Flip()
    {
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * -moveDirection; 
        transform.localScale = scale;
    }
    
    IEnumerator SafeFlip()
    {
        isFlipping = true; 
        rb.velocity = Vector2.zero; 
        Flip();
        yield return new WaitForSeconds(0.3f); 
        isFlipping = false;

        Vector2 targetVelocity = new Vector2(moveDirection * moveSpeed, rb.velocity.y);
        rb.velocity = targetVelocity;
    }
    
    // Hàm này giúp hiển thị tầm nhìn Box Check toàn cảnh trong Scene View
    private void OnDrawGizmosSelected()
    {
        // Vị trí trung tâm của Box
        Vector2 boxCenter = new Vector2(
            transform.position.x, 
            transform.position.y + sightBoxOffset
        );

        Gizmos.color = Color.yellow;
        // Vẽ Box Check tầm nhìn
        Gizmos.DrawWireCube(boxCenter, sightBoxSize);
    }
}