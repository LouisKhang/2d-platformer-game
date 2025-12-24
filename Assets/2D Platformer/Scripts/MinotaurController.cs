using UnityEngine;
using System.Collections;
using Platformer;

public class MinotaurController : MonoBehaviour
{
    // --- 1. THAM SỐ CÀI ĐẶT ---
    [Header("Stats")]
    public int health = 30;
    public float moveSpeed = 4f;

    [Header("Ranges & Cooldowns")]
    public float attackRange = 3f; 
    public float attackCooldown = 1.0f;
    public float stompCooldown = 20f;
    public float spinSlashCooldown = 15f;
    public float investigateTime = 5f;

    [Header("Sight Box (Toàn Cảnh) 👁️")]
    public Vector2 sightBoxSize = new Vector2(15f, 6f);
    public float sightBoxOffset = 0f;
    public LayerMask playerLayer;

    [Header("Attack Setup ⚔️")]
    public Collider2D attackHitbox;
    public int attackDamage = 1;

    [Header("Stomp Follow-up")]
    public GameObject followUpHitboxObject;
    
    // --- Stomp Attack (Ranged Damage & Slow) ---
    [Header("Stomp Attack Settings (Ranged Damage & Slow)")]
    public float stompRadius = 5f; // Phạm vi sát thương (Vùng rộng)
    public float slowRadius = 3f;  // Phạm vi Slow (Vùng hẹp)
    public int stompDamage = 2; 
    public float slowDuration = 2f; 
    public float slowFactor = 0.5f; 
    public GameObject stompEffectPrefab;

    // --- LOGIC LUNGE ĐỘC LẬP & COOLDOWN 12s ---
    [Header("Lunge Attack Settings (Independent) 🚀")]
    public float lungeSpeed = 12f;
    public float lungeDistance = 5f; 
    public float maxLungeDuration = 0.5f;
    public float lungeCooldown = 12f; 
    public GameObject lungeImpactEffectPrefab;

    [Header("Angry Logic")]
    private const float AngryTotalDuration = 3f;
    public int angryLoopCount = 3;
    public float angryCooldown = 15f;

    [Header("Hurt Effect Settings")]
    public float hurtDuration = 0.5f;
    public float flashInterval = 0.05f;

    [Header("Death Settings")]
    public float deathAnimationDuration = 2.0f;

    [Header("Gold Drop")]
    [Tooltip("Prefab vàng mà Boss sẽ rơi khi chết.")]
    public GameObject goldPrefab;

    [Tooltip("Số lượng vàng Boss rơi khi chết.")]
    public int goldAmount = 5;

    [Tooltip("Khoảng cách rải vàng khi rơi.")]
    public float goldSpreadRadius = 1.5f;

    [Header("Sound Effects")]
    public AudioClip slashAttackSound;      // Âm thanh đánh thường
    public AudioClip stompAttackSound;      // Âm thanh Stomp
    public AudioClip spinSlashSound;        // Âm thanh Spin Slash
    public AudioClip lungeAttackSound;      // Âm thanh Lunge
    public AudioClip angrySound;            // Âm thanh Angry
    public AudioClip hurtSound;             // Âm thanh bị đánh
    public AudioClip deathSound;            // Âm thanh chết
    public AudioClip activateSound;         // Âm thanh kích hoạt
    [Range(0f, 1f)]
    public float soundVolume = 0.7f;

    private AudioSource audioSource;

    // --- 2. BIẾN NỘI BỘ ---
    private float minXBoundary;
    private float maxXBoundary;
    private Animator animator;
    private Transform player;
    private Vector3 spawnPosition;
    private SpriteRenderer spriteRenderer;
    private Collider2D mainCollider;
    private Rigidbody2D rb;

    private Collider2D followUpCollider;

    private float lastAttackTime;
    private float lastStompTime;
    private float lastSpinSlashTime;
    private float lastLungeTime; 
    private float investigateTimer;
    private bool isActivated = false;

    private bool isPerformingSpecialAction = false;
    private bool firstAttack = true;
    [HideInInspector]
    public bool isHurt = false;
    private bool isDead = false;

    // --- CỜ QUAN TRỌNG ---
    private bool isStompFollowUp = false; 
    private bool isChainingAttack = false;
    private bool isLungeActive = false;
    private bool hasHitPlayer = false; 

    private float nextAngryTime = 0f;

    private int playerLayerID;
    private int bossLayerID;

    private bool isInvulnerable = false;

    void Start()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        mainCollider = GetComponent<Collider2D>();
        rb = GetComponent<Rigidbody2D>();
        spawnPosition = transform.position;

        // Khởi tạo AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        audioSource.volume = soundVolume;

        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        bossLayerID = gameObject.layer;
        playerLayerID = LayerMask.NameToLayer("Player");

        lastAttackTime = -attackCooldown;
        lastStompTime = Time.time;
        lastSpinSlashTime = Time.time;
        lastLungeTime = Time.time - lungeCooldown; 

        nextAngryTime = Time.time + angryCooldown;

        if (attackHitbox != null)
            attackHitbox.enabled = false;

        if (followUpHitboxObject != null)
        {
            followUpCollider = followUpHitboxObject.GetComponent<Collider2D>();
            followUpHitboxObject.SetActive(false);
        }

        if (rb == null)
            Debug.LogError("Rigidbody2D not found! Minotaur requires a Rigidbody2D.");
            
        if (minXBoundary == 0f && maxXBoundary == 0f) 
        {
            minXBoundary = spawnPosition.x - 100f; 
            maxXBoundary = spawnPosition.x + 100f;
        }
    }

    void FixedUpdate()
    {
        if (!isLungeActive && rb != null && !animator.GetBool("IsRunning") && !animator.GetBool("IsAttacking"))
        {
            rb.velocity = new Vector2(0f, rb.velocity.y);
        }
        
        ClampPosition(); 
    }
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isLungeActive && !hasHitPlayer && collision.gameObject.layer == playerLayerID)
        {
            if (player != null && collision.gameObject == player.gameObject)
            {
                hasHitPlayer = true; 

                if (lungeImpactEffectPrefab != null)
                {
                    Vector3 impactPos = collision.contacts.Length > 0 ? (Vector3)collision.contacts[0].point : transform.position;
                    Instantiate(lungeImpactEffectPrefab, impactPos, Quaternion.identity);
                }
                
                Platformer.PlayerController playerScript = player.GetComponent<Platformer.PlayerController>();
                if (playerScript != null)
                {
                    playerScript.TakeDamage(attackDamage); 
                }
            }
        }
    }

    void Update()
    {
        if (!isActivated || health <= 0 || isDead || isLungeActive || isHurt || isPerformingSpecialAction)
        {
            if (rb != null && !isLungeActive) rb.velocity = new Vector2(0f, rb.velocity.y);
            return;
        }

        if (player == null)
        {
            HandleLostPlayerLogic(true);
            return;
        }

        bool playerVisible = CheckPlayerInSight();
        float distanceToPlayer = Mathf.Abs(player.position.x - transform.position.x);

        if (!animator.GetBool("IsAttacking"))
        {
            if (distanceToPlayer <= attackRange * 1.1f && Time.time >= lastAttackTime + attackCooldown)
            {
                animator.SetBool("IsRunning", false);
                if (rb != null) rb.velocity = new Vector2(0f, rb.velocity.y);
                TryToAttack();
            }
            else if (playerVisible)
            {
                HandleChaseLogic();
            }
            else
            {
                HandleLostPlayerLogic();
            }
        }
        else 
        {
            animator.SetBool("IsRunning", false);
        }
    }

    public void ForceImmediateLunge()
    {
        if (!isActivated || isDead || player == null || isHurt || isLungeActive) 
        {
            return; 
        }

        StopCoroutine(nameof(LungeToPlayerRoutine));
        if (rb != null) rb.velocity = Vector2.zero;

        isStompFollowUp = false; 
        isChainingAttack = false;
        
        float direction = Mathf.Sign(player.position.x - transform.position.x);
        FlipSprite(direction);

        // Phát âm thanh Lunge
        PlaySound(lungeAttackSound);

        StartCoroutine(LungeToPlayerRoutine(direction));
        
        animator.SetBool("IsAttacking", true);
    }

    private IEnumerator LungeToPlayerRoutine(float directionX)
    {
        if (player == null || rb == null) yield break;

        isInvulnerable = true;
        isLungeActive = true;
        hasHitPlayer = false; 

        Vector3 startPosition = transform.position;
        
        float targetX = startPosition.x + directionX * lungeDistance;
        targetX = Mathf.Clamp(targetX, minXBoundary, maxXBoundary);
        
        float actualDistance = Mathf.Abs(targetX - startPosition.x);
        if (actualDistance <= 0.1f) 
        {
            FinishLungeLogicAfterLungeRoutine();
            yield break;
        }
        
        float lungeVelocityX = directionX * lungeSpeed;
        float lungeTimer = 0f;
        
        if (playerLayerID != -1 && bossLayerID != -1)
        {
            Physics2D.IgnoreLayerCollision(bossLayerID, playerLayerID, false);
        }
        
        while (lungeTimer < maxLungeDuration)
        {
            if (isDead || isHurt) break; 
            
            rb.velocity = new Vector2(lungeVelocityX, rb.velocity.y);
            
            if (directionX > 0 && transform.position.x >= targetX) break;
            if (directionX < 0 && transform.position.x <= targetX) break;

            lungeTimer += Time.deltaTime;
            yield return null;
        }

        if (rb != null)
        {
            rb.velocity = new Vector2(0f, rb.velocity.y);
        }

        FinishLungeLogicAfterLungeRoutine();
    }
    
    private void FinishLungeLogicAfterLungeRoutine()
    {
        if (!isLungeActive) return;

        isLungeActive = false;
        isInvulnerable = false;
        hasHitPlayer = false; 
        
        animator.SetBool("IsAttacking", false); 

        if (playerLayerID != -1 && bossLayerID != -1)
        {
            Physics2D.IgnoreLayerCollision(bossLayerID, playerLayerID, true);
        }

        if (player != null && CheckPlayerInSight())
        {
            animator.SetBool("IsRunning", true);
            HandleChaseLogic(true);
        }
        else
        {
            if (rb != null) rb.velocity = new Vector2(0f, rb.velocity.y);
        }
    }

    void TryToAttack()
    {
        string triggerToUse = "TriggerSlash";
        float currentTime = Time.time;

        if (firstAttack)
        {
            firstAttack = false;
            InitiateAttack(triggerToUse);
            return;
        }
        
        if (currentTime >= lastLungeTime + lungeCooldown)
        {
            lastLungeTime = currentTime; 
            ForceImmediateLunge(); 
            return;
        }

        if (currentTime >= lastStompTime + stompCooldown)
        {
            triggerToUse = "TriggerStomp";
            lastStompTime = currentTime;
        }
        else if (currentTime >= lastSpinSlashTime + spinSlashCooldown)
        {
            triggerToUse = "TriggerSpinSlash";
            lastSpinSlashTime = currentTime;
        }

        InitiateAttack(triggerToUse);
    }

    void InitiateAttack(string attackTriggerName)
    {
        animator.SetBool("IsAttacking", true);

        if (attackTriggerName == "TriggerStomp" || attackTriggerName == "TriggerSpinSlash")
        {
            isInvulnerable = true;
        }

        // Phát âm thanh tương ứng với loại tấn công
        if (attackTriggerName == "TriggerStomp")
        {
            PlaySound(stompAttackSound);
        }
        else if (attackTriggerName == "TriggerSpinSlash")
        {
            PlaySound(spinSlashSound);
        }
        else if (attackTriggerName == "TriggerSlash")
        {
            PlaySound(slashAttackSound);
        }

        animator.SetTrigger(attackTriggerName);
    }
    
    // --- STOMP: LOGIC SỬ DỤNG HÀM REFRESH SLOW MỚI ---
    public void PerformStompAttack()
    {
        // 1. Kích hoạt hiệu ứng Stomp
        if (stompEffectPrefab != null)
            Instantiate(stompEffectPrefab, transform.position, Quaternion.identity);

        // 2. Tìm kiếm Player
        Collider2D[] hitObjects = Physics2D.OverlapCircleAll(transform.position, stompRadius, playerLayer);

        foreach (Collider2D hit in hitObjects)
        {
            Platformer.PlayerController playerScript = hit.GetComponent<Platformer.PlayerController>();
            
            // Gây sát thương
            if (playerScript != null)
            {
                playerScript.TakeDamage(stompDamage); 
            }
            
            // --- LOGIC PHÂN VÙNG SLOW ---
            float distanceToPlayer = Vector2.Distance(transform.position, hit.transform.position);

            // CHỈ ÁP DỤNG SLOW KHI PLAYER ĐỨNG TRONG PHẠM VI slowRadius
            if (distanceToPlayer <= slowRadius) 
            {
                Platformer.PlayerDebuffController debuffScript = hit.GetComponent<Platformer.PlayerDebuffController>();
                if (debuffScript != null)
                {
                    // >>> GỌI HÀM ForceApplySlow ĐỂ LUÔN GHI ĐÈ/REFRESH SLOW <<<
                    debuffScript.ForceApplySlow(slowDuration, slowFactor, this); 
                }
            }
            // -----------------------------
        }
    }
    
    private void FinishAttackLogic()
    {
        if (isStompFollowUp)
        {
             if (followUpHitboxObject != null)
             {
                 followUpHitboxObject.SetActive(false);
             }
             isStompFollowUp = false;
             isChainingAttack = false;
        }

        animator.SetBool("IsAttacking", false);
        lastAttackTime = Time.time;

        isInvulnerable = false;

        if (player != null && CheckPlayerInSight())
        {
             animator.SetBool("IsRunning", true);
             HandleChaseLogic(true);
        }
        else
        {
             animator.SetBool("IsRunning", false);
             if (rb != null) rb.velocity = new Vector2(0f, rb.velocity.y);
        }
    }

    public void FinishAttack()
    {
        FinishAttackLogic();
    }

    public void TakeDamage(int damage)
    {
        // Nếu đang invulnerable (trong skill đặc biệt), chỉ flash màu và âm thanh
        if (isInvulnerable)
        {
            PlaySound(hurtSound);
            StartCoroutine(HurtFlashRoutine());
            return;
        }

        health -= damage;
        PlaySound(hurtSound);

        if (health <= 0)
        {
            StartCoroutine(DieRoutine());
        }
        else
        {
            // Chỉ chạy hiệu ứng hurt mà không set isHurt
            StartCoroutine(HurtFlashRoutine());
        }
    }

    private IEnumerator HurtFlashRoutine()
    {
        if (spriteRenderer == null) yield break;

        float timer = 0f;
        Color originalColor = spriteRenderer.color;

        while (timer < hurtDuration)
        {
            spriteRenderer.color = (timer % (2 * flashInterval) < flashInterval) ? Color.red : originalColor;
            yield return new WaitForSeconds(flashInterval);
            timer += flashInterval;
        }

        spriteRenderer.color = originalColor;
    }

    private void DropGold()
    {
        if (goldPrefab == null) return;

        for (int i = 0; i < goldAmount; i++)
        {
            // Tạo vị trí rơi ngẫu nhiên xung quanh Boss
            Vector3 spawnPos = transform.position + new Vector3(
                Random.Range(-goldSpreadRadius, goldSpreadRadius),
                Random.Range(0.5f, goldSpreadRadius),  // rơi từ trên xuống một chút
                0
            );

            Instantiate(goldPrefab, spawnPos, Quaternion.identity);
        }

        Debug.Log("💰 Boss rơi vàng!");
    }

    private IEnumerator DieRoutine()
    {
        isDead = true;
        StopAllCoroutines();
        
        PlaySound(deathSound);
        
        animator.SetTrigger("TriggerDeath");
        if (mainCollider != null) mainCollider.enabled = false;
        if (rb != null) rb.velocity = Vector2.zero;
        DropGold();
        yield return new WaitForSeconds(deathAnimationDuration);
        Destroy(gameObject);
    }

    private IEnumerator HurtRoutine()
    {
        if (isInvulnerable) yield break;

        isHurt = true;
        animator.SetTrigger("TriggerHit");
        if (rb != null) rb.velocity = Vector2.zero;
        if (spriteRenderer != null) {
            float timer = 0f;
            Color originalColor = spriteRenderer.color;
            while (timer < hurtDuration) {
                spriteRenderer.color = (timer % (2 * flashInterval) < flashInterval) ? Color.red : originalColor;
                yield return new WaitForSeconds(flashInterval);
                timer += flashInterval;
            }
            spriteRenderer.color = originalColor;
        }
        isHurt = false;
    }

    public void DealDamageToPlayer()
    {
        if (isStompFollowUp && followUpHitboxObject != null)
        {
            followUpHitboxObject.SetActive(true);
            HandleHitboxDamage(followUpCollider);
        }

        if (!isStompFollowUp && attackHitbox != null)
        {
            attackHitbox.enabled = true;
            HandleHitboxDamage(attackHitbox);
            attackHitbox.enabled = false; 
        }
    }

    void HandleHitboxDamage(Collider2D hitbox)
    {
        if (hitbox == null) return;
        Collider2D[] hitPlayers = Physics2D.OverlapBoxAll(hitbox.bounds.center, hitbox.bounds.size, 0f, playerLayer);
        foreach (Collider2D playerCol in hitPlayers)
        {
            Platformer.PlayerController playerScript = playerCol.GetComponent<Platformer.PlayerController>();
            if (playerScript != null)
            {
                playerScript.TakeDamage(attackDamage);
                break;
            }
        }
    }

    bool CheckPlayerInSight()
    {
        Vector2 boxCenter = new Vector2(transform.position.x, transform.position.y + sightBoxOffset);
        bool playerFound = Physics2D.OverlapBox(boxCenter, sightBoxSize, 0f, playerLayer);

        if (playerFound && player != null)
        {
            float direction = (player.position.x > transform.position.x) ? 1f : -1f;
            FlipSprite(direction);
        }
        return playerFound;
    }
    
    IEnumerator PerformAngryAction()
    {
        if (angryLoopCount <= 0) angryLoopCount = 1;
        isPerformingSpecialAction = true;
        animator.SetBool("IsRunning", false);
        if (rb != null) rb.velocity = Vector2.zero;
        float waitTimePerLoop = AngryTotalDuration / angryLoopCount;

        // Phát âm thanh Angry
        PlaySound(angrySound);

        for (int i = 0; i < angryLoopCount; i++)
        {
            if (player != null)
            {
                float directionX = player.position.x - transform.position.x;
                FlipSprite(directionX);
            }

            animator.SetTrigger("TriggerAngry");
            yield return new WaitForSeconds(waitTimePerLoop);
        }

        nextAngryTime = Time.time + angryCooldown;
        isPerformingSpecialAction = false;

        if (player != null && CheckPlayerInSight())
        {
             animator.SetBool("IsRunning", true);
             HandleChaseLogic(true);
        }
        else
        {
             animator.SetBool("IsRunning", false);
             if (rb != null) rb.velocity = new Vector2(0f, rb.velocity.y);
        }
    }

    void HandleChaseLogic(bool forceMove = false)
    {
        if (player == null) return;
        
        if (!forceMove && Time.time >= nextAngryTime)
        {
            StartCoroutine(PerformAngryAction());
            return;
        }

        float distanceToPlayerX = Mathf.Abs(player.position.x - transform.position.x);
        float directionX = player.position.x - transform.position.x;
        float direction = Mathf.Sign(directionX);
        
        bool isAtBoundary = (direction > 0 && transform.position.x >= maxXBoundary) || 
                             (direction < 0 && transform.position.x <= minXBoundary);
        
        if ((!forceMove && distanceToPlayerX <= attackRange * 0.78f) || isAtBoundary) 
        {
            animator.SetBool("IsRunning", false);
            if (rb != null) rb.velocity = new Vector2(0f, rb.velocity.y);
            
            if(isAtBoundary) FlipSprite(directionX); 
            
            return;
        }

        animator.SetBool("IsRunning", true);
        investigateTimer = 0f; 
        
        if (rb != null)
        {
            rb.velocity = new Vector2(direction * moveSpeed, rb.velocity.y);
        }
        
        FlipSprite(directionX);
    }

    private void FlipSprite(float directionX)
    {
        Vector3 localScale = transform.localScale;
        if (directionX > 0 && localScale.x > 0) localScale.x *= -1f;
        else if (directionX < 0 && localScale.x < 0) localScale.x *= -1f;
        transform.localScale = localScale;
    }

    void ClampPosition()
    {
        Vector3 currentPos = transform.position;
        currentPos.x = Mathf.Clamp(currentPos.x, minXBoundary, maxXBoundary);
        transform.position = currentPos;
    }

    void HandleLostPlayerLogic(bool forceReturn = false)
    {
        if (forceReturn) investigateTimer = investigateTime;

        if (investigateTimer < investigateTime && !forceReturn)
        {
            animator.SetBool("IsRunning", false);
            if (rb != null) rb.velocity = new Vector2(0f, rb.velocity.y);
        }
        else
        {
            float distToSpawn = Vector3.Distance(transform.position, spawnPosition);
            if (distToSpawn > 0.1f)
            {
                animator.SetBool("IsRunning", true);

                if (rb != null)
                {
                    float directionX = spawnPosition.x - transform.position.x;
                    float direction = Mathf.Sign(directionX);

                    bool canMoveTowardsSpawn = (direction > 0 && transform.position.x < spawnPosition.x) || 
                                               (direction < 0 && transform.position.x > spawnPosition.x);

                    if (canMoveTowardsSpawn)
                    {
                        rb.velocity = new Vector2(direction * moveSpeed, rb.velocity.y);
                        FlipSprite(directionX);
                    }
                    else
                    {
                        rb.velocity = new Vector2(0f, rb.velocity.y);
                    }
                }
                else
                {
                    transform.position = Vector3.MoveTowards(transform.position, spawnPosition, moveSpeed * Time.deltaTime);
                }
            }
            else
            {
                animator.SetBool("IsRunning", false);
                if (rb != null) rb.velocity = new Vector2(0f, rb.velocity.y);
            }
        }

        investigateTimer += Time.deltaTime;
    }

    public void SetMovementBoundaries(float minX, float maxX)
    {
        minXBoundary = minX;
        maxXBoundary = maxX;
    }

    public void ActivateBoss()
    {
        if (!isActivated)
        {
            isActivated = true;
            PlaySound(activateSound);
        }
    }
    
    // Hàm phát âm thanh
    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip, soundVolume);
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan; 
        Vector2 boxCenter = new Vector2(transform.position.x, transform.position.y + sightBoxOffset);
        Gizmos.DrawWireCube(boxCenter, sightBoxSize);

        Gizmos.color = Color.red;
        Gizmos.DrawLine(new Vector2(minXBoundary, transform.position.y - 1f), new Vector2(minXBoundary, transform.position.y + 1f));
        Gizmos.DrawLine(new Vector2(maxXBoundary, transform.position.y - 1f), new Vector2(maxXBoundary, transform.position.y + 1f));
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, stompRadius);
        
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, slowRadius);
    }
}