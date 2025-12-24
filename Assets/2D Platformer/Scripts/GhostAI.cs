using UnityEngine;
using System.Collections;

public class GhostAI : MonoBehaviour
{
    // CÁC THAM SỐ CÓ THỂ ĐIỀU CHỈNH TRONG UNITY INSPECTOR
    
    [Header("Health")]
    public int maxHealth = 8;
    private int currentHealth;

    [Header("Movement Stats")]
    public float moveSpeed = 3f;
    [Tooltip("Khoảng cách tối đa Ghost bay ra khỏi vị trí bắt đầu.")]
    public float patrolDistance = 5f; 
    
    // *** THAM SỐ TẦM NHÌN HÌNH CHỮ NHẬT TOÀN CẢNH ***
    [Header("Sight Box (Toàn Cảnh)")]
    public Vector2 sightBoxSize = new Vector2(12f, 1.5f); 
    public float sightBoxOffset = 0.5f; 
    public LayerMask playerLayer; 
    
    [Header("Attack Settings ⚔️")]
    public Vector2 attackRangeBoxSize = new Vector2(6f, 3f); 
    public float attackRangeBoxOffset = 0f; 
    public float attackCooldown = 1.5f; 
    public float secondaryAttackCooldown = 3.0f; 
    public Collider2D attackHitbox; 
    public int attackDamage = 1;
    private bool hasDealtDamage = false; 
    private string currentAttackType = ""; 

    // ⭐ BỔ SUNG: KHAI BÁO AUDIO
    [Header("Audio")]
    public AudioClip meleeSound;
    public AudioClip rangedSound;
    private AudioSource audioSource;
    
    
    // THỜI GIAN NỘI BỘ 
    private const float WAIT_DURATION = 5f; 
    private const float DEATH_ANIM_DURATION = 0.67f; 

    // COMPONENTS VÀ TRẠNG THÁI NỘI BỘ
    private Animator anim;
    private Rigidbody2D rb;
    private Transform player; 

    private Vector2 startPosition; 
    
    private int moveDirection = -1; // 1 = Right, -1 = Left
    
    private bool isPatrolling = true;
    private bool isDead = false;
    private bool isWaiting = false; 
    private bool isFlipping = false; 

    private bool isReturningToLKP = false; 
    private Vector2 lastKnownPlayerPosition; 

    private float currentTimer = 0f; 
    
    private float lastAttackTime;
    private float lastSecondaryAttackTime;
    private bool isAttacking = false;

    void Start()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        // ⭐ BỔ SUNG: LẤY AUDIO SOURCE
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            // Tùy chọn: Thêm AudioSource nếu thiếu
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        GameObject playerObject = GameObject.FindWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
        }

        currentHealth = maxHealth;
        startPosition = transform.position; 
        if (moveDirection == -1) Flip(); // Khởi tạo hướng
        
        lastAttackTime = Time.time - attackCooldown;
        lastSecondaryAttackTime = Time.time - secondaryAttackCooldown;
        
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.drag = 5f; 
        }
        
        if (attackHitbox != null)
            attackHitbox.enabled = false;
    }

    void Update()
    {
        // ⭐ Xử lý trạng thái tĩnh (Die, Flip, Attack).
        if (isDead || isFlipping || isAttacking) 
        {
            if (rb != null) rb.velocity = Vector2.zero;
            return; 
        }

        bool playerVisible = CheckPlayerVisibility();

        // 2. Quyết định trạng thái
        if (playerVisible && player != null)
        {
            // THẤY PLAYER -> CHASE/ATTACK (Reset các cờ phi chiến đấu)
            isPatrolling = isReturningToLKP = isWaiting = false;
            
            lastKnownPlayerPosition = player.position; 

            float direction = (player.position.x > transform.position.x) ? 1f : -1f;
            if (moveDirection != (int)direction)
            {
                moveDirection = (int)direction;
                Flip(); 
            }
            
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
            // Mất dấu Player
            if (isReturningToLKP)
            {
                ReturnToLKP(); 
            }
            else if (isWaiting || isPatrolling) 
            {
                ReturnToPatrolState(); 
            }
            else // Vừa mất dấu Player, chuyển sang tìm kiếm
            {
                isReturningToLKP = true;
                isWaiting = false; 
                ReturnToLKP(); 
            }
        }
        
        anim.SetFloat("Speed", Mathf.Abs(rb.velocity.magnitude)); 
    }
    
    // --- HÀM GÂY SÁT THƯƠNG ---
    public void DealDamage()
    {
        if (attackHitbox == null || isDead || hasDealtDamage) return;
        
        attackHitbox.enabled = true; 
        
        Collider2D[] hitPlayers = Physics2D.OverlapBoxAll(
            attackHitbox.bounds.center, 
            attackHitbox.bounds.size, 
            0f, 
            playerLayer
        );
        
        foreach (Collider2D playerCol in hitPlayers)
        {
            // Thay thế bằng script PlayerController thực tế của bạn nếu khác
            Platformer.PlayerController playerScript = playerCol.GetComponent<Platformer.PlayerController>();
            
            if (playerScript != null)
            {
                playerScript.TakeDamage(attackDamage); 
                Debug.Log($"⚔️ Ghost dealt {currentAttackType} damage! Player took {attackDamage} damage.");
                hasDealtDamage = true; 
                break; 
            }
        }
        
        attackHitbox.enabled = false;
    }

    // --- LOGIC TẤN CÔNG ---
    void TryAttack()
    {
        float currentTime = Time.time;
        bool canMelee = (currentTime >= lastAttackTime + attackCooldown);
        bool canRanged = (currentTime >= lastSecondaryAttackTime + secondaryAttackCooldown);

        if (canMelee || canRanged)
        {
            string attackToUse = "";
            
            // Ưu tiên Ranged nếu có thể và xác suất cao
            if (canRanged && (canMelee == false || Random.Range(0f, 1f) < 0.6f))
            {
                attackToUse = "RangedAttack";
                lastAttackTime = currentTime; 
                lastSecondaryAttackTime = currentTime; 
            }
            else if (canMelee)
            {
                attackToUse = "MeleeAttack"; 
                lastAttackTime = currentTime; 
            }

            if (!string.IsNullOrEmpty(attackToUse))
            {
                InitiateAttack(attackToUse); 
                
                isPatrolling = false;
                isReturningToLKP = false;
                isWaiting = false;
            }
        }
        else
        {
            rb.velocity = Vector2.zero; 
        }
    }
    
    void InitiateAttack(string attackTriggerName)
    {
        isAttacking = true;
        currentAttackType = attackTriggerName; 
        rb.velocity = Vector2.zero; 
        hasDealtDamage = false; 
        anim.SetTrigger(attackTriggerName);
        
        // ⭐ BỔ SUNG: Logic phát âm thanh
        if (audioSource != null)
        {
            if (attackTriggerName == "MeleeAttack" && meleeSound != null)
            {
                audioSource.PlayOneShot(meleeSound);
            }
            else if (attackTriggerName == "RangedAttack" && rangedSound != null)
            {
                audioSource.PlayOneShot(rangedSound);
            }
        }
        
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
    
    // GỌI TỪ ANIMATION EVENT CỦA CLIP ATTACK
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
    
    // GỌI TỪ ANIMATION EVENT CỦA CLIP HIT
    public void FinishHit()
    {
        if (isDead) return;
        
        if (CheckPlayerVisibility())
        {
            ChasePlayer();
        }
        else
        {
            isReturningToLKP = true;
            ReturnToLKP();
        }
    }


    // -----------------------------------------------------
    // --- LOGIC DI CHUYỂN & TUẦN TRA (Đã Bổ Sung Logic Clamp) ---
    // -----------------------------------------------------
    
    void ReturnToPatrolState()
    {
        if (isWaiting)
        {
            rb.velocity = Vector2.zero;
            currentTimer += Time.deltaTime;
            
            if (currentTimer >= WAIT_DURATION) 
            {
                isWaiting = false;
                currentTimer = 0f;
                isPatrolling = true; 
                isReturningToLKP = false; 
                
                // Đảo hướng sau khi chờ
                moveDirection *= -1; 
                StartCoroutine(SafeFlip()); 
            }
            return;
        }
        
        Patrol();
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
            isPatrolling = false; 
            isWaiting = true; 
            currentTimer = 0f; 
            rb.velocity = Vector2.zero; 
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
        
        // ⭐ QUAN TRỌNG: Clamp LKP để đảm bảo Ghost không bay ra khỏi vùng Patrol.
        Vector2 clampedLKP = new Vector2(
            Mathf.Clamp(lastKnownPlayerPosition.x, limitLeft, limitRight),
            transform.position.y // Giữ nguyên trục Y (vì Ghost bay trên cùng một độ cao)
        );

        float distanceX = Mathf.Abs(transform.position.x - clampedLKP.x);

        if (distanceX <= 0.2f) 
        {
            isReturningToLKP = false; 
            isWaiting = true;       
            currentTimer = 0f;
            rb.velocity = Vector2.zero;
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

        float currentX = transform.position.x;
        float limitLeft = startPosition.x - patrolDistance;
        float limitRight = startPosition.x + patrolDistance;

        // ⭐ QUAN TRỌNG: Ràng buộc vị trí X của Ghost trong khu vực Patrol
        float clampedX = Mathf.Clamp(currentX, limitLeft, limitRight);

        if (clampedX != currentX)
        {
            // Bị CLAMP: Ghost đang cố gắng bay ra khỏi vùng Patrol
            transform.position = new Vector3(clampedX, transform.position.y, transform.position.z);
            rb.velocity = Vector2.zero;
            
            // Dừng Chase, chuyển sang trạng thái chờ/Patrol
            isPatrolling = true; 
            isReturningToLKP = false;
            isWaiting = true; // Bắt đầu chờ 5s tại giới hạn Patrol
            currentTimer = 0f;
            
            // Đặt lại moveDirection để WaitAndResumePatrol có thể đảo hướng đúng
            moveDirection = (currentX >= limitRight) ? -1 : 1; 
            
            return;
        }

        // Nếu không bị Clamp, tiếp tục Chase như bình thường
        if (moveDirection != (int)direction)
        {
            moveDirection = (int)direction;
            Flip();
        }

        Vector2 targetVelocity = new Vector2(direction * moveSpeed, 0f); 
        rb.velocity = targetVelocity;
    }
    
    // --- CÁC HÀM KIỂM TRA & SỨC KHỎE ---
    
    private bool CheckPlayerVisibility()
    {
        Vector2 sightBoxCenter = new Vector2(
            transform.position.x, 
            transform.position.y + sightBoxOffset
        );
        
        Collider2D[] hits = Physics2D.OverlapBoxAll(
            sightBoxCenter, 
            sightBoxSize, 
            0f, 
            playerLayer
        );

        return (hits.Length > 0);
    }
    
    private bool CheckAttackRange()
    {
        Vector2 attackBoxCenter = new Vector2(
            transform.position.x,
            transform.position.y + attackRangeBoxOffset
        );
        
        Collider2D playerInAttackRange = Physics2D.OverlapBox(
            attackBoxCenter,
            attackRangeBoxSize,
            0f,
            playerLayer
        );
        
        return (playerInAttackRange != null);
    }
    
    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        
        isAttacking = false; 
        rb.velocity = Vector2.zero; 
        
        anim.SetTrigger("Hit"); 

        if (player != null)
        {
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

        if (isPatrolling)
        {
            Vector2 targetVelocity = new Vector2(moveDirection * moveSpeed, 0f); 
            rb.velocity = targetVelocity;
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        Vector3 currentStartPosition = (Application.isPlaying) ? (Vector3)startPosition : transform.position;

        Vector3 sightBoxCenter = new Vector3(
            transform.position.x, 
            transform.position.y + sightBoxOffset,
            transform.position.z
        );

        Gizmos.color = Color.yellow; 
        Gizmos.DrawWireCube(sightBoxCenter, sightBoxSize);
        
        Vector3 attackBoxCenter = new Vector3(
            transform.position.x,
            transform.position.y + attackRangeBoxOffset, 
            transform.position.z
        );
        
        Gizmos.color = Color.red; 
        Gizmos.DrawWireCube(attackBoxCenter, attackRangeBoxSize); 
        
        Gizmos.color = Color.cyan;
        
        Vector3 leftLimit = new Vector3(currentStartPosition.x - patrolDistance, currentStartPosition.y, currentStartPosition.z);
        Gizmos.DrawLine(leftLimit + Vector3.up * 1f, leftLimit + Vector3.down * 1f);
        
        Vector3 rightLimit = new Vector3(currentStartPosition.x + patrolDistance, currentStartPosition.y, currentStartPosition.z);
        Gizmos.DrawLine(rightLimit + Vector3.up * 1f, rightLimit + Vector3.down * 1f);
        
        if (attackHitbox != null)
        {
            Gizmos.color = new Color(0.9f, 0.4f, 0.2f, 0.5f); 
            Gizmos.DrawWireCube(attackHitbox.bounds.center, attackHitbox.bounds.size);
        }
    }
}