using UnityEngine;
using System.Collections;

public class FlyingEyeAI : MonoBehaviour
{
    // CÁC THAM SỐ CÓ THỂ ĐIỀU CHỈNH TRONG UNITY INSPECTOR
    
    [Header("Health")]
    public int maxHealth = 6;
    private int currentHealth;

    [Header("Movement Stats")]
    public float moveSpeed = 4f;
    [Tooltip("Khoảng cách tối đa quái bay ra khỏi vị trí bắt đầu.")]
    public float patrolDistance = 6f; 
    
    [Header("Sight & Attack")]
    public Vector2 sightBoxSize = new Vector2(12f, 3f); 
    public float sightBoxOffset = 0f; 
    public LayerMask playerLayer; 
    
    public Vector2 attackRangeBoxSize = new Vector2(2f, 2f); 
    public float attackRangeBoxOffset = 0f; 
    public float attackCooldown = 1.0f; 
    public Collider2D attackHitbox; // Collider con dùng để gây sát thương
    public int attackDamage = 1; 
    private float lastAttackTime; 
    private bool hasDealtDamage = false; 

    // ⭐ ĐÃ THÊM: CÀI ĐẶT ÂM THANH
    [Header("Audio")]
    public AudioClip attackSoundClip; // Clip âm thanh tấn công (Phát khi gây sát thương)
    public AudioClip hitSoundClip;    // Clip âm thanh khi bị đánh
    public AudioClip dieSoundClip;    // Clip âm thanh khi chết
    private AudioSource audioSource; // Component AudioSource

    // THỜI GIAN NỘI BỘ 
    private const float WAIT_DURATION = 5f; // Thời gian chờ (bay tại chỗ)
    private const float DEATH_ANIM_DURATION = 0.5f; 

    // COMPONENTS VÀ TRẠNG THÁI NỘI BỘ
    private Animator anim;
    private Rigidbody2D rb;
    private Transform player; 

    private Vector2 startPosition; 
    private int moveDirection = -1; // -1 = Left, 1 = Right
    
    private bool isPatrolling = true;
    private bool isDead = false;
    private bool isWaiting = false; // Đang chờ (Flying Idle)
    private bool isReturningToLKP = false; 
    private Vector2 lastKnownPlayerPosition; 

    private float currentTimer = 0f; 
    private bool isAttacking = false;

    void Start()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        
        // ⭐ KHỞI TẠO AUDIOSOURCE
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        GameObject playerObject = GameObject.FindWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
        }

        currentHealth = maxHealth;
        startPosition = transform.position; 
        Flip(); 
        
        lastAttackTime = Time.time - attackCooldown;
        
        if (rb != null)
        {
            rb.gravityScale = 0f; // Quái bay
            rb.drag = 5f; 
        }
        
        if (attackHitbox != null)
            attackHitbox.enabled = false;
    }

    void Update()
    {
        if (isDead || isAttacking) 
        {
            if (rb != null) rb.velocity = Vector2.zero;
            return; 
        }
        
        bool playerVisible = CheckPlayerVisibility();

        // LOGIC CHUYỂN TRẠNG THÁI CHÍNH
        if (playerVisible)
        {
            // THẤY PLAYER -> CHASE/ATTACK
            FacePlayer();
            lastKnownPlayerPosition = player.position; 
            isPatrolling = isReturningToLKP = isWaiting = false;

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
            // MẤT DẤU PLAYER -> RETURN/WAIT/PATROL
            if (isReturningToLKP)
            {
                ReturnToLKP(); 
            }
            else if (isWaiting) 
            {
                // Xử lý chờ (Bay tại chỗ 5s) tại LKP hoặc giới hạn Patrol
                WaitAndResumePatrol(); 
            }
            else if (isPatrolling) 
            {
                Patrol(); 
            }
            else // Vừa mất dấu Player, chưa quay về LKP
            {
                isReturningToLKP = true;
                ReturnToLKP(); 
            }
        }
    }
    
    // --- LOGIC DI CHUYỂN & TUẦN TRA ---
    
    void WaitAndResumePatrol()
    {
         if (isWaiting)
         {
             rb.velocity = Vector2.zero; // <== BAY TẠI CHỖ
             currentTimer += Time.deltaTime;
             
             if (currentTimer >= WAIT_DURATION) 
             {
                 isWaiting = false;
                 currentTimer = 0f;
                 isReturningToLKP = false; 

                 moveDirection *= -1; 
                 Flip(); 
                 
                 isPatrolling = true; 
                 Patrol();
             }
             return;
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
            // Đạt đến giới hạn, chuyển sang trạng thái chờ (Flying Idle)
            isWaiting = true; 
            currentTimer = 0f; 
            rb.velocity = Vector2.zero; // Bay tại chỗ
            return;
        }
        
        Vector2 targetVelocity = new Vector2(moveDirection * moveSpeed, 0f); 
        rb.velocity = targetVelocity; 
    }
    
    void ReturnToLKP()
    {
        if (isWaiting) return; 

        float limitLeft = startPosition.x - patrolDistance;
        float limitRight = startPosition.x + patrolDistance;
        
        Vector2 clampedLKP = new Vector2(
            Mathf.Clamp(lastKnownPlayerPosition.x, limitLeft, limitRight),
            transform.position.y 
        );

        float distanceX = Mathf.Abs(transform.position.x - clampedLKP.x);

        // Đã đến LKP (hoặc điểm Clamp gần nhất)
        if (distanceX <= 0.2f) 
        {
            isReturningToLKP = false; 
            isWaiting = true;        
            currentTimer = 0f;
            rb.velocity = Vector2.zero; // Bay tại chỗ
            return; 
        }
        else
        {
            float direction = (clampedLKP.x > transform.position.x) ? 1f : -1f;
            
            if (moveDirection != (int)direction)
            {
                moveDirection = (int)direction;
                Flip();
            }

            Vector2 targetVelocity = new Vector2(direction * moveSpeed, 0f); 
            rb.velocity = targetVelocity;
        }
    }

    void ChasePlayer()
    {
        if (isWaiting)
        {
             isWaiting = false;
             currentTimer = 0f;
        }
        
        float direction = (player.position.x > transform.position.x) ? 1f : -1f;
        
        if (moveDirection != (int)direction)
        {
            moveDirection = (int)direction;
            Flip();
        }

        Vector2 targetVelocity = new Vector2(direction * moveSpeed, 0f); 
        rb.velocity = targetVelocity;
        
        // Ràng buộc để quái không bay ra khỏi vùng Patrol khi đuổi
        float limitLeft = startPosition.x - patrolDistance;
        float limitRight = startPosition.x + patrolDistance;
        
        float clampedX = Mathf.Clamp(transform.position.x, limitLeft, limitRight);

        if (clampedX != transform.position.x)
        {
            // Nếu quái bị Clamp (sắp bay ra ngoài)
            transform.position = new Vector3(clampedX, transform.position.y, transform.position.z);
            rb.velocity = Vector2.zero;
            
            // Dừng Chase, chuyển sang trạng thái chờ/Patrol
            isPatrolling = true; 
            isReturningToLKP = false;
            isWaiting = true; 
            currentTimer = 0f;
        }
    }
    
    // --- LOGIC TẤN CÔNG & HP ---
    
    void TryAttack()
    {
        float currentTime = Time.time;
        if (currentTime >= lastAttackTime + attackCooldown)
        {
            InitiateAttack("attack"); 
            lastAttackTime = currentTime; 
            
            isPatrolling = false;
            isReturningToLKP = false;
            isWaiting = false;
        }
        else
        {
            rb.velocity = Vector2.zero;
        }
    }
    
    void InitiateAttack(string attackTriggerName)
    {
        isAttacking = true; 
        rb.velocity = Vector2.zero; 
        hasDealtDamage = false; 
        anim.SetTrigger(attackTriggerName);
        FacePlayer(); 
    }
    
    // GỌI BẰNG ANIMATION EVENT
    public void DealDamage()
    {
        if (attackHitbox == null || isDead || hasDealtDamage) return;
        
        // ⭐ PHÁT ÂM THANH TẤN CÔNG
        if (audioSource != null && attackSoundClip != null)
        {
            audioSource.PlayOneShot(attackSoundClip);
        }

        if (attackHitbox != null) attackHitbox.enabled = true; 
        
        Collider2D[] hitPlayers = Physics2D.OverlapBoxAll(
            attackHitbox.bounds.center, 
            attackHitbox.bounds.size, 
            0f, 
            playerLayer
        );
        
        foreach (Collider2D playerCol in hitPlayers)
        {
            
             Platformer.PlayerController playerScript = playerCol.GetComponent<Platformer.PlayerController>();
             if (playerScript != null)
             {
                 playerScript.TakeDamage(attackDamage); 
                 hasDealtDamage = true; 
                 if (attackHitbox != null) attackHitbox.enabled = false; 
                 break; 
             }
        }
        StartCoroutine(DisableHitboxAfterDelay());
    }

    // GỌI BẰNG ANIMATION EVENT
    public void FinishAttack()
    {
        isAttacking = false;
        
        if (CheckPlayerVisibility()) 
        {
            if (!CheckAttackRange()) 
            {
                ChasePlayer();
            }
            else
            {
                rb.velocity = Vector2.zero;
            }
        }
        else
        {
            isReturningToLKP = true;
            ReturnToLKP();
        }
    }
    
    IEnumerator DisableHitboxAfterDelay()
    {
        yield return null; 
        if (attackHitbox != null) attackHitbox.enabled = false;
    }
    
    // --- HÀM PHỤ TRỢ ---
    
    public void TakeDamage(int damage)
    {
        if (isDead) return;
        currentHealth -= damage;
        anim.SetTrigger("hit");
        
        // ⭐ PHÁT ÂM THANH BỊ ĐÁNH
        if (audioSource != null && hitSoundClip != null)
        {
            audioSource.PlayOneShot(hitSoundClip);
        }
        
        if (player != null)
        {
            isPatrolling = false;
            isReturningToLKP = false;
            isWaiting = false; 
            lastKnownPlayerPosition = player.position;
            FacePlayer();
        }

        if (currentHealth <= 0) Die();
    }
    
    public void Die()
    {
        if (isDead) return;
        isDead = true;
        anim.SetTrigger("dead");
        rb.velocity = Vector2.zero;
        if (rb != null) rb.simulated = false; 
        
        // ⭐ PHÁT ÂM THANH CHẾT
        if (audioSource != null && dieSoundClip != null)
        {
            // Chơi âm thanh chết (có thể đặt âm thanh này là 1 Shot để đảm bảo nó phát hết)
            audioSource.PlayOneShot(dieSoundClip);
        }
        
        Collider2D[] cols = GetComponentsInChildren<Collider2D>();
        foreach (Collider2D col in cols) col.enabled = false;
        Destroy(gameObject, DEATH_ANIM_DURATION + 0.1f); 
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
    
    private void Flip()
    {
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * -moveDirection; 
        transform.localScale = scale;
    }
    
    private void FacePlayer()
    {
          if (player != null)
          {
              float direction = (player.position.x > transform.position.x) ? 1f : -1f;
              if (moveDirection != (int)direction)
              {
                  moveDirection = (int)direction;
                  Flip();
              }
          }
    }
    
    // --- GIZMOS ---
    
    void OnDrawGizmosSelected()
    {
        Vector3 currentStartPosition = (Application.isPlaying) ? (Vector3)startPosition : transform.position;

        // TẦM NHÌN (SIGHT BOX)
        Vector3 sightBoxCenter = new Vector3(transform.position.x, transform.position.y + sightBoxOffset, transform.position.z);
        Gizmos.color = Color.yellow; 
        Gizmos.DrawWireCube(sightBoxCenter, sightBoxSize);
        
        // TẦM TẤN CÔNG (ATTACK RANGE BOX)
        Vector3 attackBoxCenter = new Vector3(transform.position.x, transform.position.y + attackRangeBoxOffset, transform.position.z);
        Gizmos.color = Color.red; 
        Gizmos.DrawWireCube(attackBoxCenter, attackRangeBoxSize); 
        
        // GIỚI HẠN TUẦN TRA (PATROL LIMITS)
        Gizmos.color = Color.cyan;
        Vector3 leftLimit = new Vector3(currentStartPosition.x - patrolDistance, currentStartPosition.y, currentStartPosition.z);
        Gizmos.DrawLine(leftLimit + Vector3.up * 1f, leftLimit + Vector3.down * 1f);
        Vector3 rightLimit = new Vector3(currentStartPosition.x + patrolDistance, currentStartPosition.y, currentStartPosition.z);
        Gizmos.DrawLine(rightLimit + Vector3.up * 1f, rightLimit + Vector3.down * 1f);
        
        // KHUNG HITBOX TẤN CÔNG
        if (attackHitbox != null)
        {
            Gizmos.color = new Color(0.9f, 0.4f, 0.2f, 0.7f); 
            Gizmos.DrawWireCube(attackHitbox.bounds.center, attackHitbox.bounds.size);
        }
    }
}