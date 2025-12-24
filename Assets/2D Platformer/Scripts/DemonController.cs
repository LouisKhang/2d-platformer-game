using UnityEngine;
using System.Collections;
using Platformer;

public class DemonController : MonoBehaviour
{
    [Header("Stats")]
    public int health = 30;
    public float moveSpeed = 4f;

    [Header("Ranges & Cooldowns")]
    public float attackRange = 3f;
    public float attackCooldown = 1.5f;
    public float investigateTime = 5f;

    [Header("Sight Box (Toàn Cảnh) 👁️")]
    public Vector2 sightBoxSize = new Vector2(15f, 6f);
    public float sightBoxOffset = 0f;
    public LayerMask playerLayer;

    [Header("Attack Setup ⚔️")]
    public Collider2D attackHitbox;
    public int attackDamage = 1;

    [Header("Lunge Attack Settings 🚀")]
    public float lungeSpeed = 12f;
    public float lungeDistance = 5f;
    public float maxLungeDuration = 0.5f;
    public float lungeCooldown = 12f;
    public GameObject lungeImpactEffectPrefab;

    [Header("Rage Logic (New) 🔥")]
    private const float RageTotalDuration = 3f;
    public float rageCooldown = 15f;
    public GameObject firePrefab;
    public Transform[] fireSpawnPoints;
    public float fireLifetime = 8f;

    [Header("FireBall Attack Settings 🔥⚡")]
    public GameObject fireballPrefab;
    public GameObject fireballExplosionPrefab;
    public Transform[] fireballSpawnPoints;
    public float fireballSpeed = 10f;
    public int fireballDamage = 2;
    public float fireballCooldown = 5f;
    public float fireballAttackDistance = 8f;
    public float fireballInterval = 2f;

    [Header("Hurt Effect Settings")]
    public float hurtDuration = 0.5f;
    public float flashInterval = 0.05f;

    [Header("Death Settings")]
    public float deathAnimationDuration = 2.0f;
    [Tooltip("Prefab nổ khi Boss chết")]
    public GameObject deathExplosionPrefab;
    public float explosionDuration = 1.5f;

    [Header("Gold Drop")]
    public GameObject goldPrefab;
    public int goldAmount = 5;
    public float goldSpreadRadius = 1.5f;

    [Header("Portal Settings 🌀")]
    [Tooltip("2 GameObject Portal đã đặt sẵn trên map (đang tắt)")]
    public GameObject[] portalsToActivate;
    [Tooltip("Delay trước khi hiện portal sau khi boss chết")]
    public float portalActivationDelay = 1.0f;

    [Header("Sound Effects 🔊")]
    [Tooltip("Âm thanh khi Boss được kích hoạt")]
    public AudioClip activateSound;
    
    [Tooltip("Âm thanh đánh thường (Cleave)")]
    public AudioClip cleaveAttackSound;
    
    [Tooltip("Âm thanh khi Lunge (lao về phía player)")]
    public AudioClip lungeSound;
    
    [Tooltip("Âm thanh khi Lunge đâm trúng player")]
    public AudioClip lungeImpactSound;
    
    [Tooltip("Âm thanh khi bắn FireBall")]
    public AudioClip fireballShootSound;
    
    [Tooltip("Âm thanh khi FireBall nổ")]
    public AudioClip fireballExplosionSound;
    
    [Tooltip("Âm thanh khi Rage (triệu hồi lửa)")]
    public AudioClip rageSound;
    
    [Tooltip("Âm thanh của lửa cháy khi Rage")]
    public AudioClip fireLoopSound;
    
    [Tooltip("Âm thanh khi bị hit")]
    public AudioClip hurtSound;
    
    [Tooltip("Âm thanh khi chết")]
    public AudioClip deathSound;
    
    [Tooltip("Âm thanh footstep khi chạy (Optional)")]
    public AudioClip[] footstepSounds;
    
    [Tooltip("Âm thanh khi portal xuất hiện")]
    public AudioClip portalAppearSound;
    
    [Range(0f, 1f)]
    [Tooltip("Volume cho tất cả sound effects")]
    public float soundVolume = 1f;

    // AudioSource component
    private AudioSource audioSource;

    public event System.Action OnBossDeath;

    private float minXBoundary;
    private float maxXBoundary;
    private Animator animator;
    private Transform player;
    private Vector3 spawnPosition;
    private SpriteRenderer spriteRenderer;
    private Collider2D mainCollider;
    private Rigidbody2D rb;

    private float lastAttackTime;
    private float lastLungeTime;
    private float lastFireBallTime;
    private float investigateTimer;
    private bool isActivated = false;

    private bool isPerformingSpecialAction = false;
    private bool firstAttack = true;
    [HideInInspector] public bool isHurt = false;
    private bool isDead = false;

    private bool isLungeActive = false;
    private bool hasHitPlayer = false;

    private float nextRageTime = 0f;
    private int playerLayerID;
    private int bossLayerID;
    private bool isInvulnerable = false;

    private int currentFireBallIndex = 0;
    public void HideBoss()
    {
        if (spriteRenderer != null)
            spriteRenderer.enabled = false;

        if (mainCollider != null)
            mainCollider.enabled = false;

        if (rb != null)
            rb.simulated = false;

        isActivated = false;
    }

    public void ShowBossInstant()
    {
        if (spriteRenderer != null)
            spriteRenderer.enabled = true;

        if (mainCollider != null)
            mainCollider.enabled = true;

        if (rb != null)
            rb.simulated = true;
    }

    void Start()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        mainCollider = GetComponent<Collider2D>();
        rb = GetComponent<Rigidbody2D>();
        spawnPosition = transform.position;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0.5f;

        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        bossLayerID = gameObject.layer;
        playerLayerID = LayerMask.NameToLayer("Player");

        lastAttackTime = -attackCooldown;
        lastLungeTime = Time.time;
        lastFireBallTime = Time.time;
        nextRageTime = Time.time + rageCooldown;

        if (attackHitbox != null)
            attackHitbox.enabled = false;

        if (minXBoundary == 0f && maxXBoundary == 0f)
        {
            minXBoundary = spawnPosition.x - 100f;
            maxXBoundary = spawnPosition.x + 100f;
        }

        // 🔴 ẨN BOSS LÚC ĐẦU (RẤT QUAN TRỌNG)
        if (spriteRenderer != null)
            spriteRenderer.enabled = false;

        if (mainCollider != null)
            mainCollider.enabled = false;

        if (rb != null)
            rb.simulated = false;

        isActivated = false;
        isDead = false;
        isHurt = false;
        isLungeActive = false;
        isPerformingSpecialAction = false;

        // Tắt portal nếu có
        if (portalsToActivate != null)
        {
            foreach (GameObject portal in portalsToActivate)
            {
                if (portal != null)
                    portal.SetActive(false);
            }
        }

        Debug.Log("DemonController Start: Boss hidden & waiting for activation.");
    }


    private void PlaySound(AudioClip clip, float volumeScale = 1f)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip, soundVolume * volumeScale);
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
            if (distanceToPlayer <= attackRange * 1.1f)
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
        if (!isActivated || isDead || player == null || isHurt || isLungeActive) return;

        StopAllCoroutines();
        if (rb != null) rb.velocity = Vector2.zero;

        float direction = Mathf.Sign(player.position.x - transform.position.x);
        FlipSprite(direction);

        PlaySound(lungeSound);

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

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isLungeActive && !hasHitPlayer && collision.gameObject.layer == playerLayerID)
        {
            if (player != null && collision.gameObject == player.gameObject)
            {
                hasHitPlayer = true;

                PlaySound(lungeImpactSound);

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

    void TryToAttack()
    {
        float currentTime = Time.time;
        
        if (currentTime >= lastFireBallTime + fireballCooldown)
        {
            float distanceToPlayer = Mathf.Abs(player.position.x - transform.position.x);
            Debug.Log($"TryToAttack: FireBall Cooldown OK ({currentTime} >= {lastFireBallTime} + {fireballCooldown}). Distance: {distanceToPlayer}. Initiating FireBall Attack.");
            
            lastFireBallTime = currentTime;
            StartCoroutine(PerformFireBallAttack());
            return;
        }

        if (currentTime >= lastLungeTime + lungeCooldown)
        {
            lastLungeTime = currentTime;
            ForceImmediateLunge();
            return;
        }

        if (currentTime >= lastAttackTime + attackCooldown)
        {
            InitiateAttack("TriggerCleave");
        }
    }

    private IEnumerator PerformFireBallAttack()
    {
        if (isDead || isHurt || isPerformingSpecialAction) yield break;

        isPerformingSpecialAction = true;
        isInvulnerable = true;
        animator.SetBool("IsRunning", false);
        
        if (rb != null) rb.velocity = Vector2.zero;
        
        Debug.Log("FireBall Attack: START sequence. Boss stands still and fires immediately.");

        currentFireBallIndex = 0;

        for (int i = 0; i < 3; i++)
        {
            if (isDead || isHurt) break;

            if (player != null)
            {
                FlipSprite(player.position.x - transform.position.x);
            }
            
            Debug.Log($"FireBall Attack: Triggering shot #{i+1}");

            animator.SetTrigger("TriggerFireBall");

            yield return new WaitForSeconds(fireballInterval);

            currentFireBallIndex++;
        }

        Debug.Log("FireBall Attack: Sequence FINISHED.");
        isPerformingSpecialAction = false;
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

    private IEnumerator MoveToFireBallPosition()
    {
        yield break; 
    }

    public void ShootFireBall()
    {
        Debug.Log($"*** ANIMATION EVENT: ShootFireBall Called! Shooting 3 fireballs at once! ***");
        
        PlaySound(fireballShootSound);
        
        if (fireballPrefab == null || fireballSpawnPoints == null || fireballSpawnPoints.Length == 0)
        {
            Debug.LogError("FireBall prefab hoặc spawn points chưa được setup! Check Inspector.");
            return;
        }

        int spawnIndex = currentFireBallIndex % fireballSpawnPoints.Length;
        Transform spawnPoint = fireballSpawnPoints[spawnIndex];

        if (spawnPoint == null)
        {
            Debug.LogError($"FireBall spawn point index {spawnIndex} is null!");
            return;
        }

        float[] angleOffsets = { 0f, -15f, 15f };
        
        for (int i = 0; i < 3; i++)
        {
            GameObject fireball = Instantiate(fireballPrefab, spawnPoint.position, Quaternion.identity);
            Debug.Log($"FireBall {i+1}/3 Instantiated at: {spawnPoint.position}");

            DemonFireBall fireballScript = fireball.GetComponent<DemonFireBall>();
            if (fireballScript != null)
            {
                Debug.Log($"FireBall {i+1} Script Found. Setting up properties.");
                fireballScript.damage = fireballDamage;
                fireballScript.speed = fireballSpeed;
                fireballScript.explosionPrefab = fireballExplosionPrefab;
                fireballScript.explosionSound = fireballExplosionSound;

                Vector2 baseDirection;
                if (player != null)
                {
                    baseDirection = (player.position - spawnPoint.position).normalized;
                }
                else
                {
                    float facing = transform.localScale.x < 0 ? 1f : -1f;
                    baseDirection = new Vector2(facing, 0f);
                }

                float angleInRadians = angleOffsets[i] * Mathf.Deg2Rad;
                Vector2 rotatedDirection = new Vector2(
                    baseDirection.x * Mathf.Cos(angleInRadians) - baseDirection.y * Mathf.Sin(angleInRadians),
                    baseDirection.x * Mathf.Sin(angleInRadians) + baseDirection.y * Mathf.Cos(angleInRadians)
                );

                fireballScript.direction = rotatedDirection.normalized;
                Debug.Log($"FireBall {i+1} Direction: {rotatedDirection.normalized} (Angle: {angleOffsets[i]}°)");
            }
            else
            {
                Debug.LogError($"ERROR: DemonFireBall script NOT FOUND on FireBall {i+1}!");
            }
        }
    }

    void InitiateAttack(string attackTriggerName)
    {
        animator.SetBool("IsAttacking", true);
        isInvulnerable = (attackTriggerName == "TriggerRage");
        animator.SetTrigger(attackTriggerName);
    }

    public void FinishAttack()
    {
        FinishAttackLogic();
    }

    private void FinishAttackLogic()
    {
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

    public void DealDamageToPlayer()
    {
        if (attackHitbox != null)
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

    IEnumerator PerformRageAction()
    {
        if (isDead || isHurt || isPerformingSpecialAction) yield break;

        isPerformingSpecialAction = true;
        isInvulnerable = true;
        animator.SetBool("IsRunning", false);
        if (rb != null) rb.velocity = Vector2.zero;

        FlipSprite(player.position.x - transform.position.x);

        animator.SetTrigger("TriggerRage");
        
        PlaySound(rageSound);

        yield return new WaitForSeconds(0.2f);

        if (firePrefab != null && fireSpawnPoints != null)
        {
            foreach (Transform spawnPoint in fireSpawnPoints)
            {
                if (spawnPoint != null)
                {
                    GameObject spawnedFire = Instantiate(firePrefab, spawnPoint.position, Quaternion.identity);
                    
                    if (fireLoopSound != null)
                    {
                        AudioSource fireAudio = spawnedFire.GetComponent<AudioSource>();
                        if (fireAudio == null)
                        {
                            fireAudio = spawnedFire.AddComponent<AudioSource>();
                        }
                        fireAudio.clip = fireLoopSound;
                        fireAudio.loop = true;
                        fireAudio.volume = soundVolume * 0.7f;
                        fireAudio.spatialBlend = 0.8f;
                        fireAudio.Play();
                    }
                    
                    Destroy(spawnedFire, fireLifetime);
                }
            }
        }

        yield return new WaitForSeconds(RageTotalDuration - 0.2f);

        nextRageTime = Time.time + rageCooldown;
        isPerformingSpecialAction = false;
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

    void HandleChaseLogic(bool forceMove = false)
    {
        if (player == null) return;

        if (!forceMove && Time.time >= nextRageTime)
        {
            StartCoroutine(PerformRageAction());
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
            if (isAtBoundary) FlipSprite(directionX);
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

    public void TakeDamage(int damage)
    {
        if (isInvulnerable) return;

        health -= damage;

        PlaySound(hurtSound);

        if (health <= 0)
        {
            StartCoroutine(DieRoutine());
        }
        else
        {
            animator.SetBool("IsAttacking", false);
            isPerformingSpecialAction = false;
            
            StopCoroutine(nameof(PerformFireBallAttack));
            StopCoroutine(nameof(PerformRageAction));
            
            animator.SetTrigger("TriggerHit");
            StartCoroutine(HurtFlashRoutine());
            
            StartCoroutine(RecoverFromHurt());
        }
    }

    private IEnumerator RecoverFromHurt()
    {
        isHurt = true;
        yield return new WaitForSeconds(hurtDuration);
        isHurt = false;
        
        if (player != null && CheckPlayerInSight())
        {
            float distanceToPlayer = Mathf.Abs(player.position.x - transform.position.x);
            if (distanceToPlayer <= attackRange * 1.1f)
            {
                animator.SetBool("IsRunning", false);
            }
            else
            {
                animator.SetBool("IsRunning", true);
                HandleChaseLogic(true);
            }
        }
        else
        {
            animator.SetBool("IsRunning", false);
            if (rb != null) rb.velocity = new Vector2(0f, rb.velocity.y);
        }
    }

    private IEnumerator HurtFlashRoutine()
    {
        if (spriteRenderer == null) yield break;

        float timer = 0f;
        Color originalColor = spriteRenderer.color;

        if (rb != null) rb.velocity = new Vector2(0f, rb.velocity.y);

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
            Vector3 spawnPos = transform.position + new Vector3(
                Random.Range(-goldSpreadRadius, goldSpreadRadius),
                Random.Range(0.5f, goldSpreadRadius),
                0
            );
            Instantiate(goldPrefab, spawnPos, Quaternion.identity);
        }
    }

    // *** CẬP NHẬT: Hàm kích hoạt portal ***
    private void ActivatePortals()
    {
        if (portalsToActivate == null || portalsToActivate.Length == 0)
        {
            Debug.LogWarning("⚠️ Không có portal nào được gán trong Inspector!");
            return;
        }

        Debug.Log($"🌀 Bắt đầu kích hoạt {portalsToActivate.Length} portal(s)...");

        foreach (GameObject portal in portalsToActivate)
        {
            if (portal != null)
            {
                // Kiểm tra trạng thái trước khi active
                bool wasActive = portal.activeSelf;
                Debug.Log($"📍 Portal {portal.name} - Vị trí: {portal.transform.position} - Trạng thái trước: {(wasActive ? "BẬT" : "TẮT")}");
                
                portal.SetActive(true);
                
                // Verify sau khi active
                Debug.Log($"✅ Portal {portal.name} đã được kích hoạt! Trạng thái sau: {(portal.activeSelf ? "BẬT" : "TẮT")}");
                
                // Bật tất cả child objects của portal
                foreach (Transform child in portal.transform)
                {
                    child.gameObject.SetActive(true);
                    Debug.Log($"  └─ Child: {child.name} đã được bật");
                }
            }
            else
            {
                Debug.LogError("❌ Portal trong array là NULL!");
            }
        }

        // Phát âm thanh portal xuất hiện
        if (portalAppearSound != null)
        {
            AudioSource.PlayClipAtPoint(portalAppearSound, transform.position, soundVolume);
        }
    }

    private IEnumerator DieRoutine()
    {
        isDead = true;
        Debug.Log("💀 Boss DieRoutine bắt đầu!");
        
        // ❌ KHÔNG DÙNG StopAllCoroutines()
        
        // Reset trạng thái boss
        isPerformingSpecialAction = false;
        isLungeActive = false;
        isHurt = false;

        if (deathSound != null)
        {
            AudioSource.PlayClipAtPoint(deathSound, transform.position, soundVolume);
        }

        animator.SetTrigger("TriggerDeath");
        if (mainCollider != null) mainCollider.enabled = false;
        if (rb != null) rb.velocity = Vector2.zero;

        OnBossDeath?.Invoke();
        Debug.Log("📢 OnBossDeath event invoked!");

        if (deathExplosionPrefab != null)
        {
            GameObject explosion = Instantiate(deathExplosionPrefab, transform.position, Quaternion.identity);
            yield return new WaitForSeconds(explosionDuration);
        }

        

        // ✅ PORTAL CHẮC CHẮN ĐƯỢC GỌI
        yield return new WaitForSeconds(portalActivationDelay);
        ActivatePortals();

        yield return new WaitForSeconds(deathAnimationDuration);
        Destroy(gameObject);
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
            }
            else
            {
                animator.SetBool("IsRunning", false);
                if (rb != null) rb.velocity = new Vector2(0f, rb.velocity.y);
            }
        }

        investigateTimer += Time.deltaTime;
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

    public void SetMovementBoundaries(float minX, float maxX)
    {
        minXBoundary = minX;
        maxXBoundary = maxX;
    }

    public void ActivateBoss()
    {
        isActivated = true;
        animator.SetBool("IsRunning", true);
        
        PlaySound(activateSound);
    }

    public void PlayFootstepSound()
    {
        if (footstepSounds != null && footstepSounds.Length > 0)
        {
            AudioClip randomFootstep = footstepSounds[Random.Range(0, footstepSounds.Length)];
            PlaySound(randomFootstep, 0.5f);
        }
    }

    public void PlayCleaveSound()
    {
        PlaySound(cleaveAttackSound);
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
        Gizmos.DrawWireSphere(transform.position, attackRange * 1.1f);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, fireballAttackDistance);
    }
} 