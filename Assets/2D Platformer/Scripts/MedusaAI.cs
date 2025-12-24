using UnityEngine;
using System.Collections;

public class MedusaAI : MonoBehaviour
{
    // CÁC THAM SỐ CÓ THỂ ĐIỀU CHỈNH
    
    [Header("Health")]
    public int maxHealth = 8;
    private int currentHealth;

    [Header("Movement Stats")]
    public float moveSpeed = 3f;
    public float patrolDistance = 5f; 
    
    [Header("Sight Box & Layer")]
    public Vector2 sightBoxSize = new Vector2(12f, 1.5f); 
    public float sightBoxOffset = 0.5f; 
    public LayerMask playerLayer; 
    
    [Header("Attack Settings ⚔️")]
    public Vector2 attackRangeBoxSize = new Vector2(6f, 3f); 
    public float attackRangeBoxOffset = 0f; 
    public float attackCooldown = 1.5f; 
    public int attackDamage = 1; 

    [Header("Stone Effect 🗿")]
    public GameObject stoneEffectPrefab; 
    public float stoneDuration = 1.5f; 
    private bool hasAppliedStoneEffect = false; 
    
    [Header("Visual Feedback")] 
    public Color hitColor = Color.red;
    public float flashDuration = 0.15f; 

    // THỜI GIAN HẰNG SỐ
    private const float WAIT_DURATION = 5f; 
    private const float DEATH_ANIM_DURATION = 0.67f; 
    private const float DESTROY_DELAY_AFTER_DEATH = 2f; 
    
    // TRẠNG THÁI VÀ COMPONENTS NỘI BỘ
    private Animator anim;
    private Rigidbody2D rb;
    private Transform player; 
    private SpriteRenderer sr; 
    private Color originalColor; 

    private Vector2 startPosition; 
    private int moveDirection = -1; 
    private bool isPatrolling = true;
    private bool isDead = false;
    private bool isWaiting = false;   
    private bool isFlipping = false;    
    private bool isReturningToLKP = false;  
    private bool isReturningToStart = false; 
    private Vector2 lastKnownPlayerPosition; 
    private float currentTimer = 0f; 
    private float lastAttackTime;
    private bool isAttacking = false;
    private bool isBeingHit = false; 

    void Start()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>(); 
        
        GameObject playerObject = GameObject.FindWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
        }
        
        currentHealth = maxHealth;
        startPosition = transform.position; 
        
        if (rb != null)
        {
            rb.gravityScale = 1f;
            rb.drag = 0f; 
        }

        if (sr != null) 
        {
            originalColor = sr.color;
        }

        if (moveDirection == -1) Flip(); 
        lastAttackTime = Time.time - attackCooldown; 
    }

    void Update()
    {
        // 1. NGĂN CHẶN TOÀN BỘ LOGIC KHI CHẾT, ĐANG LẬT MẶT HOẶC ĐANG BỊ TẤN CÔNG/HIT
        if (isDead || isFlipping || isAttacking || isBeingHit) 
        {
            if (rb != null) rb.velocity = new Vector2(0f, rb.velocity.y); 
            return; 
        }

        bool playerVisible = CheckPlayerVisibility();

        // --- LOGIC CHUYỂN TRẠNG THÁI CHÍNH ---
        if (playerVisible && player != null)
        {
            // THẤY PLAYER -> CHASE/ATTACK
            isPatrolling = isReturningToLKP = isWaiting = isReturningToStart = false; 
            lastKnownPlayerPosition = player.position; 
            
            float direction = (player.position.x > transform.position.x) ? 1f : -1f;
            FaceDirection(direction); 
            
            if (CheckAttackRange())
            {
                TryAttack(); 
            }
            else
            {
                ChasePlayer(); 
            }
        }
        else // Không thấy Player
        {
            // MẤT DẤU PLAYER -> RETURN/WAIT/PATROL
            if (isReturningToStart)
            {
                ReturnToStart();
            }
            else if (isReturningToLKP)
            {
                ReturnToLKP();
            }
            else if (isWaiting) 
            {
                WaitAndResumePatrol(); 
            }
            else if (isPatrolling) 
            {
                Patrol(); 
            }
            else 
            {
                // Nếu vừa mất dấu, sẽ chuyển về LKP
                isReturningToLKP = true;
                ReturnToLKP(); 
            }
        }
        
        // Cập nhật Animator
        anim.SetFloat("Speed", Mathf.Abs(rb.velocity.x)); 
    }
    
    // ------------------------------------
    // --- LOGIC DI CHUYỂN & TUẦN TRA ---
    // ------------------------------------
    
    void WaitAndResumePatrol()
    {
        // Xử lý đếm ngược thời gian chờ (Idle)
        rb.velocity = new Vector2(0f, rb.velocity.y);
        currentTimer += Time.deltaTime;
        
        if (currentTimer >= WAIT_DURATION) 
        {
            isWaiting = false;
            currentTimer = 0f;
            isReturningToLKP = false;

            if (Mathf.Abs(transform.position.x - startPosition.x) > patrolDistance + 0.5f)
            {
                // Nếu quá xa vị trí ban đầu (do bị đẩy lùi/lỗi) -> Bắt buộc Return to Start
                isReturningToStart = true;
                ReturnToStart();
                return;
            }
            else
            {
                isPatrolling = true; 
                
                float limitLeft = startPosition.x - patrolDistance;
                float limitRight = startPosition.x + patrolDistance;
                
                // Nếu Medusa đang đứng tại giới hạn Patrol (sau khi chờ) thì đảo hướng
                if (Mathf.Abs(transform.position.x - limitRight) < 0.2f || Mathf.Abs(transform.position.x - limitLeft) < 0.2f)
                {
                    moveDirection *= -1;
                } 
                
                StartCoroutine(SafeFlip()); 
            }
        }
    }

    void Patrol()
    {
        float limitLeft = startPosition.x - patrolDistance;
        float limitRight = startPosition.x + patrolDistance;
        float currentX = transform.position.x;
        bool shouldTurn = false;
        
        if (moveDirection == 1 && currentX >= limitRight)
        {
            shouldTurn = true;
        }
        else if (moveDirection == -1 && currentX <= limitLeft)
        {
            shouldTurn = true;
        }
        
        if (shouldTurn)
        {
            // Đạt đến giới hạn, chuyển sang trạng thái chờ (Wait)
            isPatrolling = false; 
            isWaiting = true; 
            currentTimer = 0f; 
            rb.velocity = new Vector2(0f, rb.velocity.y); 
            return;
        }
        
        Vector2 targetVelocity = new Vector2(moveDirection * moveSpeed, rb.velocity.y); 
        rb.velocity = targetVelocity;
    }
    
    /**
     * Cập nhật: Clamp LKP để Medusa không ra khỏi vùng Patrol.
     */
    void ReturnToLKP()
    {
        if (isWaiting) 
        {
            WaitAndResumePatrol(); 
            return;
        }

        float limitLeft = startPosition.x - patrolDistance;
        float limitRight = startPosition.x + patrolDistance;
        
        // ⭐ QUAN TRỌNG: Clamp LKP để đảm bảo Medusa không đi ra khỏi vùng Patrol.
        Vector2 clampedLKP = new Vector2(
            Mathf.Clamp(lastKnownPlayerPosition.x, limitLeft, limitRight),
            lastKnownPlayerPosition.y // Giữ nguyên trục Y của LKP
        );
        
        float distanceX = Mathf.Abs(transform.position.x - clampedLKP.x);

        if (distanceX <= 0.2f) 
        {
            isReturningToLKP = false; 
            isWaiting = true;        
            currentTimer = 0f;
            rb.velocity = new Vector2(0f, rb.velocity.y);
            
            // Chuyển sang Wait
            WaitAndResumePatrol(); 
            return;
        }
        else
        {
            float direction = (clampedLKP.x > transform.position.x) ? 1f : -1f;
            FaceDirection(direction); 
            
            Vector2 targetVelocity = new Vector2(direction * moveSpeed, rb.velocity.y); 
            rb.velocity = targetVelocity;
        }
    }
    
    void ReturnToStart()
    {
        float distanceX = Mathf.Abs(transform.position.x - startPosition.x);

        if (distanceX <= 0.5f) 
        {
            isReturningToStart = false;
            isPatrolling = true;
            rb.velocity = new Vector2(0f, rb.velocity.y);
            
            // Quay mặt về hướng tuần tra ban đầu, sau đó tiếp tục Patrolling
            float direction = (startPosition.x > transform.position.x) ? 1f : -1f;
            moveDirection = (int)direction;
            StartCoroutine(SafeFlip());
            return;
        }

        float directionToStart = (startPosition.x > transform.position.x) ? 1f : -1f;
        FaceDirection(directionToStart);
        
        Vector2 targetVelocity = new Vector2(directionToStart * moveSpeed, rb.velocity.y); 
        rb.velocity = targetVelocity;
    }

    /**
     * Cập nhật: Sử dụng Clamp để ngăn Medusa rời khỏi vùng Patrol khi Chase.
     */
    void ChasePlayer()
    {
        float direction = (player.position.x > transform.position.x) ? 1f : -1f;
        
        float currentX = transform.position.x;
        float limitLeft = startPosition.x - patrolDistance;
        float limitRight = startPosition.x + patrolDistance;

        // ⭐ Ràng buộc vị trí X của Medusa trong khu vực Patrol
        float clampedX = Mathf.Clamp(currentX, limitLeft, limitRight);

        if (clampedX != currentX)
        {
            // Bị CLAMP: Medusa đang cố gắng đi ra khỏi vùng Patrol
            transform.position = new Vector3(clampedX, transform.position.y, transform.position.z);
            rb.velocity = new Vector2(0f, rb.velocity.y);

            // Chuyển sang trạng thái chờ/Patrol ngay lập tức
            isPatrolling = true; 
            isReturningToLKP = false;
            isWaiting = true; // Bắt đầu chờ 5s tại giới hạn Patrol
            currentTimer = 0f;
            
            // Đặt lại moveDirection để WaitAndResumePatrol có thể đảo hướng đúng
            moveDirection = (currentX >= limitRight) ? -1 : 1; 
            
            return;
        }

        // Nếu không bị Clamp, tiếp tục Chase như bình thường
        FaceDirection(direction);

        Vector2 targetVelocity = new Vector2(direction * moveSpeed, rb.velocity.y); 
        rb.velocity = targetVelocity;
    }
    
    // -----------------------------------------------------
    // --- LOGIC TẤN CÔNG & HP ---
    // -----------------------------------------------------
    
    void TryAttack()
    {
        float currentTime = Time.time;
        
        if (currentTime >= lastAttackTime + attackCooldown)
        {
            InitiateAttack("MeleeAttack"); 
            lastAttackTime = currentTime; 
            
            isPatrolling = false;
            isReturningToLKP = false;
            isWaiting = false;
            isReturningToStart = false;
        }
        else
        {
            rb.velocity = new Vector2(0f, rb.velocity.y); 
        }
    }
    
    void InitiateAttack(string attackTriggerName)
    {
        isAttacking = true;
        hasAppliedStoneEffect = false;
        rb.velocity = new Vector2(0f, rb.velocity.y); 
        anim.SetTrigger(attackTriggerName);
        
        if (player != null)
        {
            float direction = (player.position.x > transform.position.x) ? 1f : -1f;
            FaceDirection(direction); 
        }
    }

    // GỌI TỪ ANIMATION EVENT
    public void ApplyStoneEffect() 
    {
        if (isDead || hasAppliedStoneEffect || stoneEffectPrefab == null || player == null) return;

        if (CheckPlayerVisibility() && CheckAttackRange())
        {
            GameObject activeStoneEffect = Instantiate(stoneEffectPrefab, player.position, Quaternion.identity);
            activeStoneEffect.transform.SetParent(player);
            
            // Giả định có script StoneEffect.cs đính kèm
            if (activeStoneEffect.TryGetComponent<StoneEffect>(out StoneEffect stoneScript))
            {
                stoneScript.Initialize(stoneDuration, attackDamage); 
            }

            hasAppliedStoneEffect = true; 
        }
    }
    
    // GỌI TỪ ANIMATION EVENT CỦA CLIP ATTACK
    public void FinishAttack()
    {
        isAttacking = false;
        hasAppliedStoneEffect = false; 

        if (CheckPlayerVisibility() && player != null) 
        {
            if (!CheckAttackRange()) 
            {
                ChasePlayer();
            }
            else
            {
                rb.velocity = new Vector2(0f, rb.velocity.y);
            }
        }
        else
        {
            isReturningToLKP = true;
            ReturnToLKP();
        }
    }
    
    // GỌI TỪ ANIMATION EVENT CỦA CLIP HIT (Thoát trạng thái bị đánh)
    public void FinishHit()
    {
        isBeingHit = false; // Thoát cờ chặn logic

        if (isDead) return;
        
        // Sau khi bị đánh, quyết định hành động tiếp theo
        if (CheckPlayerVisibility())
        {
            if (CheckAttackRange())
            {
                TryAttack();
            }
            else
            {
                ChasePlayer();
            }
        }
        else
        {
            isReturningToLKP = true;
            ReturnToLKP();
        }
    }

    private bool CheckPlayerVisibility()
    {
        Vector2 sightBoxCenter = new Vector2(transform.position.x, transform.position.y + sightBoxOffset);
        Collider2D[] hits = Physics2D.OverlapBoxAll(sightBoxCenter, sightBoxSize, 0f, playerLayer);
        return (hits.Length > 0);
    }
    
    private bool CheckAttackRange()
    {
        Vector2 attackBoxCenter = new Vector2(transform.position.x, transform.position.y + attackRangeBoxOffset);
        Collider2D playerInAttackRange = Physics2D.OverlapBox(attackBoxCenter, attackRangeBoxSize, 0f, playerLayer);
        return (playerInAttackRange != null);
    }
    
    public void TakeDamage(int damage)
    {
        if (isDead) return;
        
        currentHealth -= damage;
        
        // NGẮT CHIÊU VÀ CHẶN LOGIC CHÍNH
        isAttacking = false; 
        isBeingHit = true; 
        rb.velocity = new Vector2(0f, rb.velocity.y);
        
        anim.SetTrigger("Hit"); 
        StartCoroutine(FlashColor()); 

        if (currentHealth <= 0)
        {
            Die();
            return;
        }
        
        // Cập nhật LKP và quay mặt về phía kẻ tấn công
        if (player != null)
        {
            isPatrolling = false;
            isWaiting = false; 
            isReturningToStart = false; 
            
            lastKnownPlayerPosition = player.position;

            float direction = (player.position.x > transform.position.x) ? 1f : -1f;
            FaceDirection(direction); 
        }
    }
    
    public void Die()
    {
        if (isDead) return;
        isDead = true;
        anim.SetTrigger("Die"); 
        rb.velocity = Vector2.zero;
        if (rb != null) rb.simulated = false; 
        
        Collider2D[] cols = GetComponentsInChildren<Collider2D>();
        foreach (Collider2D col in cols) col.enabled = false;
        
        Destroy(gameObject, DESTROY_DELAY_AFTER_DEATH); 
    }

    // COROUTINE NHÁY MÀU
    IEnumerator FlashColor()
    {
        if (sr != null)
        {
            sr.color = hitColor; 
            yield return new WaitForSeconds(flashDuration);
            if (!isDead)
            {
                sr.color = originalColor; 
            }
        }
        yield break;
    }

    private void FaceDirection(float direction)
    {
        // Chỉ lật khi không đang bị hit, tấn công hoặc lật
        if (moveDirection != (int)direction && !isFlipping && !isAttacking && !isBeingHit)
        {
            moveDirection = (int)direction;
            Flip(); 
        }
    }
    
    private void Flip()
    {
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * -moveDirection; 
        transform.localScale = scale;
    }
    
    IEnumerator SafeFlip()
    {
        isFlipping = true; 
        rb.velocity = new Vector2(0f, rb.velocity.y); 
        Flip();
        yield return new WaitForSeconds(0.3f); 
        isFlipping = false;
        
        if (isPatrolling)
        {
            Vector2 targetVelocity = new Vector2(moveDirection * moveSpeed, rb.velocity.y); 
            rb.velocity = targetVelocity;
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        Vector3 currentStartPosition = (Application.isPlaying) ? (Vector3)startPosition : transform.position;

        // VẼ TẦM NHÌN
        Vector3 sightBoxCenter = new Vector3(transform.position.x, transform.position.y + sightBoxOffset, transform.position.z);
        Gizmos.color = Color.yellow; 
        Gizmos.DrawWireCube(sightBoxCenter, sightBoxSize);
        
        // VẼ TẦM TẤN CÔNG
        Vector3 attackBoxCenter = new Vector3(transform.position.x, transform.position.y + attackRangeBoxOffset, transform.position.z);
        Gizmos.color = Color.red; 
        Gizmos.DrawWireCube(attackBoxCenter, attackRangeBoxSize); 
        
        // VẼ GIỚI HẠN TUẦN TRA
        Gizmos.color = Color.cyan;
        Vector3 leftLimit = new Vector3(currentStartPosition.x - patrolDistance, currentStartPosition.y, currentStartPosition.z);
        Vector3 rightLimit = new Vector3(currentStartPosition.x + patrolDistance, currentStartPosition.y, currentStartPosition.z);
        Gizmos.DrawLine(leftLimit + Vector3.up * 1f, leftLimit + Vector3.down * 1f);
        Gizmos.DrawLine(rightLimit + Vector3.up * 1f, rightLimit + Vector3.down * 1f);
    }
}