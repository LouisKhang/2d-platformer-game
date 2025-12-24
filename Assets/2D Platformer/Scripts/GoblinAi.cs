using UnityEngine;
using System.Collections;

public class GoblinAI : MonoBehaviour
{
    // CÁC THAM SỐ CÓ THỂ ĐIỀU CHỈNH TRONG UNITY INSPECTOR

    [Header("Health")]
    public int maxHealth = 8;
    private int currentHealth;

    [Header("Movement Stats")]
    public float moveSpeed = 3f;
    [Tooltip("Khoảng cách tối đa Goblin đi ra khỏi vị trí bắt đầu.")]
    public float patrolDistance = 5f; 
    
    // THAM SỐ TẦM NHÌN VÀ PHẠM VI TẤN CÔNG
    [Header("Detection & Combat")]
    public Vector2 sightBoxSize = new Vector2(12f, 1.5f); 
    public float sightBoxOffset = 0.5f; 
    public LayerMask playerLayer; 
    
    public Vector2 attackRangeBoxSize = new Vector2(2f, 3f); // Giảm tầm tấn công cận chiến
    public float attackRangeBoxOffset = 0f; 
    
    public float attack1Cooldown = 1.0f; // Cooldown cho Melee Attack 1 (thường)
    public float attack2Cooldown = 3.0f; // Cooldown cho Melee Attack 2 (mạnh hơn/phụ)
    
    public Collider2D attackHitbox; 
    public int attackDamage = 1;
    public int secondaryAttackDamage = 3; // Sát thương cao hơn cho Attack 2
    
    private bool hasDealtDamage = false; 
    private string currentAttackType = ""; // "MeleeAttack1" hoặc "MeleeAttack2"

    // [BỔ SUNG AUDIO]
    [Header("Audio")]
    public AudioClip attack1Sound; // Âm thanh cho Melee Attack 1
    public AudioClip attack2Sound; // Âm thanh cho Melee Attack 2
    public AudioClip hitSound;     // Âm thanh khi bị đánh
    public AudioClip deathSound;   // Âm thanh khi chết
    private AudioSource audioSource; // Component để phát âm thanh
    // [/BỔ SUNG AUDIO]

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
    private bool isFlipping = false; // Dùng cờ này để tạm dừng di chuyển, KHÔNG chặn Update()

    private bool isReturningToLKP = false; 
    private Vector2 lastKnownPlayerPosition; 

    private float currentTimer = 0f; 
    
    private float lastAttack1Time;
    private float lastAttack2Time;
    private bool isAttacking = false;

    void Start()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        
        // [BỔ SUNG AUDIO]
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) 
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false; 
        // [/BỔ SUNG AUDIO]
        
        GameObject playerObject = GameObject.FindWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
        }

        currentHealth = maxHealth;
        startPosition = transform.position; 
        if (moveDirection == -1) Flip(); 
        
        lastAttack1Time = Time.time - attack1Cooldown;
        lastAttack2Time = Time.time - attack2Cooldown;
        
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
        // ⭐ SỬA: CHỈ CHẶN UPDATE NẾU CHẾT HOẶC ĐANG TẤN CÔNG
        if (isDead || isAttacking) 
        {
            if (rb != null) rb.velocity = Vector2.zero;
            return; 
        }
        
        // ⭐ BỔ SUNG: DỪNG TẤT CẢ LOGIC NẾU ĐANG XOAY NGƯỜI (TRÁNH LỖI NGẮT)
        if (isFlipping)
        {
            if (rb != null) rb.velocity = Vector2.zero;
            return; 
        }
        // *Lưu ý: Mặc dù isFlipping chặn Update, nhưng nó chỉ chạy trong 0.15s trong Coroutine,
        // không đủ dài để cắt animation chém. isAttacking mới là cờ dài hạn.

        bool playerVisible = CheckPlayerVisibility();

        // 2. Quyết định trạng thái
        if (playerVisible && player != null)
        {
            isPatrolling = isReturningToLKP = isWaiting = false;
            
            lastKnownPlayerPosition = player.position; 

            float direction = (player.position.x > transform.position.x) ? 1f : -1f;
            if (moveDirection != (int)direction)
            {
                moveDirection = (int)direction;
                // ⭐ SỬA: Dùng SafeFlip() khi Chase để có thời gian chuyển hướng
                StartCoroutine(SafeFlip()); 
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
        else 
        {
            if (isReturningToLKP)
            {
                ReturnToLKP(); 
            }
            else if (isWaiting || isPatrolling) 
            {
                ReturnToPatrolState(); 
            }
            else 
            {
                isReturningToLKP = true;
                isWaiting = false; 
                ReturnToLKP(); 
            }
        }
        
        // Cập nhật Animator Speed.
        if (rb != null)
        {
            rb.velocity = new Vector2(rb.velocity.x, 0f); 
            anim.SetFloat("Speed", Mathf.Abs(rb.velocity.x)); 
        }
    }
    
    // --- HÀM GÂY SÁT THƯƠNG (Điều chỉnh damage theo loại attack) ---
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
        
        int damageToDeal = (currentAttackType == "MeleeAttack2") ? secondaryAttackDamage : attackDamage;

        foreach (Collider2D playerCol in hitPlayers)
        {
            Platformer.PlayerController playerScript = playerCol.GetComponent<Platformer.PlayerController>();
            
            if (playerScript != null)
            {
                playerScript.TakeDamage(damageToDeal); 
                Debug.Log($"⚔️ Goblin dealt {currentAttackType} damage! Player took {damageToDeal} damage.");
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
        bool canAttack1 = (currentTime >= lastAttack1Time + attack1Cooldown);
        bool canAttack2 = (currentTime >= lastAttack2Time + attack2Cooldown);

        if (canAttack1 || canAttack2)
        {
            string attackToUse = "";
            
            if (canAttack2 && (canAttack1 == false || Random.Range(0f, 1f) < 0.6f))
            {
                attackToUse = "MeleeAttack2";
                lastAttack1Time = currentTime; 
                lastAttack2Time = currentTime; 
            }
            else if (canAttack1)
            {
                attackToUse = "MeleeAttack1"; 
                lastAttack1Time = currentTime; 
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
        
        if (audioSource != null)
        {
            if (attackTriggerName == "MeleeAttack1" && attack1Sound != null)
            {
                audioSource.PlayOneShot(attack1Sound);
            }
            else if (attackTriggerName == "MeleeAttack2" && attack2Sound != null)
            {
                audioSource.PlayOneShot(attack2Sound);
            }
        }

        // Đảm bảo Goblin quay mặt về phía Player ngay trước khi tấn công
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
        // ⭐ QUAN TRỌNG: Đặt isAttacking = false sau khi hiệu ứng hoàn tất.
        isAttacking = false; 
        
        // Logic tiếp tục: Nếu vẫn thấy Player, Chase hoặc Attack lại, nếu không thì ReturnToLKP
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
    // --- LOGIC DI CHUYỂN & TUẦN TRA ---
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
        
        Vector2 clampedLKP = new Vector2(
            Mathf.Clamp(lastKnownPlayerPosition.x, limitLeft, limitRight),
            transform.position.y 
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
                // ⭐ SỬA: Dùng SafeFlip() khi ReturnToLKP để có thời gian chuyển hướng
                StartCoroutine(SafeFlip());
            }
            else if (!isFlipping) // ⭐ Bổ sung: Chỉ di chuyển nếu không đang xoay
            {
                Vector2 targetVelocity = new Vector2(direction * moveSpeed, 0f); 
                rb.velocity = targetVelocity;
            }
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

        float clampedX = Mathf.Clamp(currentX, limitLeft, limitRight);

        if (clampedX != currentX)
        {
            transform.position = new Vector3(clampedX, transform.position.y, transform.position.z);
            rb.velocity = Vector2.zero;
            
            isPatrolling = true; 
            isReturningToLKP = false;
            isWaiting = true; 
            currentTimer = 0f;
            
            moveDirection = (currentX >= limitRight) ? -1 : 1; 
            
            return;
        }

        if (moveDirection != (int)direction)
        {
            moveDirection = (int)direction;
            StartCoroutine(SafeFlip()); 
        }
        else if (!isFlipping) // ⭐ Bổ sung: Chỉ di chuyển nếu không đang xoay
        {
            Vector2 targetVelocity = new Vector2(direction * moveSpeed, 0f); 
            rb.velocity = targetVelocity;
        }
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
        
        if (audioSource != null && hitSound != null)
        {
            audioSource.PlayOneShot(hitSound);
        }

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
        
        if (audioSource != null && deathSound != null)
        {
            audioSource.PlayOneShot(deathSound);
        }
        
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
    
    // ⭐ Đã sửa SafeFlip để chỉ dừng di chuyển trong thời gian xoay,
    // nhưng không chặn toàn bộ logic Update() (vì isFlipping không còn trong điều kiện chặn đầu Update)
    IEnumerator SafeFlip()
    {
        if (isFlipping) yield break; // Tránh chạy lặp
        
        isFlipping = true; 
        rb.velocity = Vector2.zero; 
        Flip();
        
        yield return new WaitForSeconds(0.15f); 
        isFlipping = false;

        // Tiếp tục di chuyển sau khi Flip
        if (isPatrolling || isReturningToLKP)
        {
            Vector2 targetVelocity = new Vector2(moveDirection * moveSpeed, 0f); 
            rb.velocity = targetVelocity;
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        // Giữ nguyên Gizmos
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