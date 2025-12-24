using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EvilWizardBoss : MonoBehaviour
{
    [Header("Boss Stats")]
    public float maxHealth = 50f;
    private float currentHealth;
    public float moveSpeed = 3f;
    public float attackRange = 15f;
    public float stopDistance = 7f;
    public float attackCooldown = 2f;
    private float lastAttackTime;
    public float disappearTime = 2f;

    [Header("Boundary & Return Logic")]
    public Collider2D boundaryCollider;
    public float returnSpeed = 5f;
    public float losePlayerDelay = 5f;
    private Vector3 spawnPosition;
    private bool isReturningToSpawn = false;
    private float playerOutOfBoundsTime = 0f;
    private bool hasReachedSpawn = false;

    [Header("References")]
    private Animator anim;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Transform player;

    public GameObject magicBoltPrefab;
    public Transform firePoint;

    [Header("Health & Hit Effect")]
    private bool isDead = false;
    private bool isInvulnerable = false;
    public float invulnerabilityDuration = 0.3f;
    public Color hurtColor = Color.red;
    public float flashDuration = 0.1f;
    private Color originalColor;
    private bool isActivated = false;

    [Header("Death Effect")]
    public GameObject deadEffectPrefab;
    public float deadEffectDuration = 4f;

    [Header("Summoning Logic")]
    public GameObject skeletonPrefab;
    public GameObject summonEffectPrefab;
    public Transform summonPoint;
    public float initialSummonDelay = 10f;
    public float summonCooldown = 10f;
    private float nextSummonTime;
    private bool isSummoning = false;

    [Header("Bat Summon Logic")]
    public GameObject batsCloudPrefab;
    public float batTravelSpeed = 8f;
    public float batSummonCooldown = 20f;
    private float lastBatSummonTime;
    private bool isCastingBatAttack = false;

    [Header("Gold Drop")]
    public GameObject goldPrefab;
    public int goldAmount = 5;
    public float goldSpreadRadius = 1.5f;

    [Header("Sound Effects")]
    public AudioClip attackSound;           // Âm thanh khi tấn công (Magic Bolt)
    public AudioClip summonSound;           // Âm thanh khi triệu hồi Skeleton
    public AudioClip batAttackSound;        // Âm thanh khi triệu hồi Bat
    public AudioClip hurtSound;             // Âm thanh khi bị đánh
    public AudioClip deathSound;            // Âm thanh khi chết
    public AudioClip activateSound;         // Âm thanh khi được kích hoạt
    [Range(0f, 1f)]
    public float soundVolume = 0.7f;
    
    private AudioSource audioSource;

    private List<GameObject> summonedSkeletons = new List<GameObject>();

    void Start()
    {
        currentHealth = maxHealth;
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Khởi tạo AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        audioSource.volume = soundVolume;

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;

        spawnPosition = transform.position;

        enabled = false;
        if (rb != null) rb.velocity = Vector2.zero;
        SetAnimationState(false, false);

        // 🔹 Fix bat cooldown lần đầu
        lastBatSummonTime = Time.time;

        if(firePoint != null && !firePoint.CompareTag("FirePoint"))
        {
             Debug.LogError("LỖI: Vui lòng tạo Tag 'FirePoint' trong Unity và gán cho FirePoint GameObject.");
        }

        if (boundaryCollider == null)
        {
            Debug.LogWarning("⚠️ Boundary Collider chưa được gán! Boss sẽ không có giới hạn vùng di chuyển.");
        }
    }

    public void ActivateBoss()
    {
        if (isDead || isActivated) return;

        isActivated = true;
        enabled = true;
        hasReachedSpawn = false;

        nextSummonTime = Time.time + initialSummonDelay;

        PlaySound(activateSound);

        Debug.Log("🔥 Evil Wizard Boss Activated!");
    }

    private void DeactivateBoss()
    {
        if (!isActivated) return;

        isActivated = false;
        enabled = false;

        isReturningToSpawn = false;
        hasReachedSpawn = false;
        playerOutOfBoundsTime = 0f;
        isSummoning = false;
        isCastingBatAttack = false;
        isInvulnerable = false;

        anim.SetBool("IsMoving", false);
        anim.SetBool("IsAttacking", false);

        if (rb != null) rb.velocity = Vector2.zero;

        KillAllSummonedSkeletons();

        currentHealth = maxHealth;

        Debug.Log("💤 Boss đã reset về trạng thái chưa kích hoạt. Chờ Player kích hoạt lại...");
    }

    void Update()
    {
        if (isDead || !isActivated || player == null) return;

        bool playerInBounds = IsPlayerInBounds();

        if (!playerInBounds)
        {
            playerOutOfBoundsTime += Time.deltaTime;

            if (playerOutOfBoundsTime >= losePlayerDelay && !isReturningToSpawn)
            {
                StartReturnToSpawn();
            }
        }
        else
        {
            playerOutOfBoundsTime = 0f;

            if (isReturningToSpawn && !hasReachedSpawn)
            {
                CancelReturnToSpawn();
            }
        }

        if (isReturningToSpawn)
        {
            HandleReturnToSpawn();
            return;
        }

        if (!IsBossInBounds())
        {
            MoveBackIntoBounds();
            return;
        }

        if (!isSummoning && Time.time >= nextSummonTime)
            StartSummon();

        if (!isCastingBatAttack && Time.time >= lastBatSummonTime + batSummonCooldown)
            StartBatAttack();

        // Khi summon hoặc cast bat, chỉ dừng movement, boss vẫn hit được
        if (isSummoning || isCastingBatAttack)
        {
            rb.velocity = Vector2.zero;
            anim.SetBool("IsMoving", false);
            anim.SetBool("IsAttacking", false);
            Flip();
        }
        else
        {
            float distanceToPlayer = Vector2.Distance(transform.position, player.position);

            if (distanceToPlayer <= attackRange && playerInBounds)
                HandleAttackLogic();
            else
            {
                rb.velocity = Vector2.zero;
                SetAnimationState(false, false);
            }

            Flip();
        }
    }

    private bool IsPlayerInBounds()
    {
        if (boundaryCollider == null || player == null) return true;
        return boundaryCollider.bounds.Contains(player.position);
    }

    private bool IsBossInBounds()
    {
        if (boundaryCollider == null) return true;
        return boundaryCollider.bounds.Contains(transform.position);
    }

    private void MoveBackIntoBounds()
    {
        if (boundaryCollider == null) return;

        Vector3 closestPoint = boundaryCollider.bounds.ClosestPoint(transform.position);
        Vector2 direction = (closestPoint - transform.position).normalized;
        rb.velocity = new Vector2(direction.x * moveSpeed, rb.velocity.y);

        anim.SetBool("IsMoving", true);
        anim.SetBool("IsAttacking", false);
        Flip();
    }

    private void StartReturnToSpawn()
    {
        if (isReturningToSpawn) return;

        isReturningToSpawn = true;
        isSummoning = false;
        isCastingBatAttack = false;

        anim.SetBool("IsAttacking", false);
        anim.SetBool("IsMoving", true);

        Debug.Log("🏠 Boss đang quay về điểm Spawn vì mất Player!");
    }

    private void CancelReturnToSpawn()
    {
        if (hasReachedSpawn) return;
        isReturningToSpawn = false;
        Debug.Log("⚔️ Boss hủy quay về Spawn vì Player đã quay lại!");
    }

    private void HandleReturnToSpawn()
    {
        float distanceToSpawn = Vector2.Distance(transform.position, spawnPosition);

        if (distanceToSpawn < 0.5f)
        {
            rb.velocity = Vector2.zero;
            transform.position = spawnPosition;

            anim.SetBool("IsMoving", false);
            anim.SetBool("IsAttacking", false);

            hasReachedSpawn = true;
            isReturningToSpawn = false;

            Debug.Log("✅ Boss đã về đến điểm Spawn.");
            DeactivateBoss();
        }
        else
        {
            Vector2 direction = (spawnPosition - transform.position).normalized;
            rb.velocity = new Vector2(direction.x * returnSpeed, rb.velocity.y);
            anim.SetBool("IsMoving", true);
            Flip();
        }
    }

    void StartBatAttack()
    {
        if (isCastingBatAttack || batsCloudPrefab == null) return;

        isCastingBatAttack = true;
        rb.velocity = Vector2.zero;
        SetAnimationState(false, false);

        anim.SetTrigger("BatAttack");
        PlaySound(batAttackSound);
        Debug.Log("Wizard bắt đầu Triệu hồi Dơi!");
    }

    public void FireBatsCloud()
    {
        if (!isActivated || isDead || batsCloudPrefab == null) return;

        float directionX = Mathf.Sign(transform.localScale.x);
        Vector2 travelDirection = new Vector2(directionX, 0);

        GameObject batGO = Instantiate(batsCloudPrefab, transform.position, Quaternion.identity);

        Vector3 localScale = batGO.transform.localScale;
        localScale.x = Mathf.Abs(localScale.x) * directionX;
        batGO.transform.localScale = localScale;

        Rigidbody2D batRb = batGO.GetComponent<Rigidbody2D>();
        if (batRb != null)
            batRb.velocity = travelDirection * batTravelSpeed;
        else
            Debug.LogError("BatCloudPrefab thiếu Rigidbody2D!");

        Debug.Log("Đã tạo Đám Dơi gây sát thương và bắn ngang!");
    }

    public void EndBatAttack()
    {
        isCastingBatAttack = false;
        lastBatSummonTime = Time.time;
        SetAnimationState(false, false);
        Debug.Log("Animation Triệu hồi Dơi kết thúc!");
    }

    void StartSummon()
    {
        if (isSummoning || skeletonPrefab == null) return;

        isSummoning = true;
        rb.velocity = Vector2.zero;
        SetAnimationState(false, false);

        anim.SetTrigger("Summon");
        PlaySound(summonSound);
        Debug.Log("Wizard bắt đầu Triệu hồi!");
    }

    public void SummonSkeleton()
    {
        if (!isActivated || isDead || skeletonPrefab == null) return;

        Transform spawnPos = summonPoint != null ? summonPoint : transform;

        for (int i = 0; i < 1; i++)
        {
            float offsetValue = (i == 0) ? -0.5f : 0.5f;
            Vector3 spawnPosition = spawnPos.position + new Vector3(offsetValue, 0, 0);

            if (summonEffectPrefab != null)
            {
                GameObject effect = Instantiate(summonEffectPrefab, spawnPosition, Quaternion.identity);
                Destroy(effect, 2f);
            }

            GameObject skeletonGO = Instantiate(skeletonPrefab, spawnPosition, Quaternion.identity);
            SkeletonSummonController skeletonAI = skeletonGO.GetComponent<SkeletonSummonController>();
            if (skeletonAI != null)
            {
                skeletonAI.InitializeAI(player);
                skeletonAI.moveSpeed = this.moveSpeed;
            }

            summonedSkeletons.Add(skeletonGO);
        }
        Debug.Log("Đã triệu hồi Quái Xương!");
    }

    public void EndSummon()
    {
        isSummoning = false;
        nextSummonTime = Time.time + summonCooldown;
        Debug.Log("Animation Triệu hồi kết thúc!");
    }

    void HandleAttackLogic()
    {
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (Time.time > lastAttackTime + attackCooldown)
        {
            rb.velocity = Vector2.zero;
            SetAnimationState(false, false);
            anim.SetBool("IsAttacking", true);
            lastAttackTime = Time.time;
        }
        else
        {
            if (distanceToPlayer <= stopDistance)
            {
                rb.velocity = Vector2.zero;
                SetAnimationState(false, false);
            }
            else
            {
                HandleMovement(player.position);
            }
        }
    }

    void HandleMovement(Vector3 target)
    {
        Vector2 direction = (target - transform.position).normalized;
        rb.velocity = new Vector2(direction.x * moveSpeed, rb.velocity.y);
        anim.SetBool("IsMoving", true);
        anim.SetBool("IsAttacking", false);
    }

    public void FireProjectile()
    {
        if (magicBoltPrefab == null || firePoint == null || player == null) return;

        PlaySound(attackSound);

        Vector2 direction = (player.position - firePoint.position).normalized;
        GameObject boltGO = Instantiate(magicBoltPrefab, firePoint.position, Quaternion.identity);

        MagicBolt bolt = boltGO.GetComponent<MagicBolt>();
        if (bolt != null)
        {
            boltGO.transform.localScale = new Vector3(Mathf.Sign(transform.localScale.x) * Mathf.Abs(boltGO.transform.localScale.x), boltGO.transform.localScale.y, boltGO.transform.localScale.z);
            bolt.Launch(direction);
        }
        else
        {
            Debug.LogError("MagicBolt script không tồn tại trên Prefab đạn phép!");
        }
    }

    public void TakeDamage(int damageAmount, string attackType, Vector3 attackerPosition)
    {
        if (isDead || !isActivated) return;

        // Nếu đang cast skill thì KHÔNG cho phép bị đánh gián đoạn
        if (isSummoning || isCastingBatAttack)
        {
            // Vẫn trừ máu và flash màu nhưng KHÔNG reset animation
            if (isInvulnerable) return;
            
            currentHealth -= damageAmount;
            Debug.Log($"Boss nhận {damageAmount} sát thương từ {attackType} (đang cast skill). Máu còn lại: {currentHealth}");
            
            PlaySound(hurtSound);
            StartCoroutine(FlashHurtColor());
            
            if (currentHealth <= 0) Die();
            return;
        }

        // Logic bình thường khi KHÔNG đang cast skill
        if (isInvulnerable) return;

        currentHealth -= damageAmount;
        Debug.Log($"Boss nhận {damageAmount} sát thương từ {attackType}. Máu còn lại: {currentHealth}");

        PlaySound(hurtSound);

        anim.SetTrigger("IsHit");
        StartCoroutine(ReactToAttack());

        if (currentHealth <= 0) Die();
    }

    private IEnumerator ReactToAttack()
    {
        isInvulnerable = true;

        if (spriteRenderer != null)
        {
            Color original = spriteRenderer.color;
            spriteRenderer.color = hurtColor;
            yield return new WaitForSeconds(flashDuration);
            spriteRenderer.color = original;
        }

        yield return new WaitForSeconds(invulnerabilityDuration - flashDuration);
        isInvulnerable = false;
    }

    // Coroutine riêng cho flash màu khi đang cast skill
    private IEnumerator FlashHurtColor()
    {
        isInvulnerable = true;

        if (spriteRenderer != null)
        {
            Color original = spriteRenderer.color;
            spriteRenderer.color = hurtColor;
            yield return new WaitForSeconds(flashDuration);
            spriteRenderer.color = original;
        }

        yield return new WaitForSeconds(invulnerabilityDuration - flashDuration);
        isInvulnerable = false;
    }

    private void KillAllSummonedSkeletons()
    {
        for (int i = summonedSkeletons.Count - 1; i >= 0; i--)
        {
            GameObject skeleton = summonedSkeletons[i];

            if (skeleton != null)
            {
                SkeletonSummonController skelCtrl = skeleton.GetComponent<SkeletonSummonController>();
                if (skelCtrl != null) skelCtrl.Die();
                else Destroy(skeleton);
            }
            summonedSkeletons.RemoveAt(i);
        }
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

        Debug.Log("💰 Boss rơi vàng!");
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        PlaySound(deathSound);

        KillAllSummonedSkeletons();

        anim.SetBool("IsMoving", false);
        anim.SetBool("IsAttacking", false);
        anim.SetTrigger("DieTrigger");

        DropGold();

        if (deadEffectPrefab != null)
        {
            GameObject emptyParent = new GameObject("TempEffectParent");
            emptyParent.transform.position = transform.position;
            emptyParent.transform.rotation = Quaternion.identity;
            emptyParent.transform.localScale = Vector3.one;

            GameObject effect = Instantiate(deadEffectPrefab, emptyParent.transform);
            effect.transform.localPosition = Vector3.zero;

            Destroy(emptyParent, deadEffectDuration);
        }

        if (rb != null) { rb.velocity = Vector2.zero; rb.simulated = false; }
        Collider2D[] cols = GetComponentsInChildren<Collider2D>();
        foreach (Collider2D col in cols) col.enabled = false;

        enabled = false;

        StartCoroutine(WaitForDeathAnimationAndDestroy());
    }

    private IEnumerator WaitForDeathAnimationAndDestroy()
    {
        float deathAnimationLength = 0.85f;
        float remainingDestroyTime = deadEffectDuration - deathAnimationLength;

        yield return new WaitForSeconds(deathAnimationLength);

        if (remainingDestroyTime > 0)
            yield return new WaitForSeconds(remainingDestroyTime);

        Destroy(gameObject);
    }

    void SetAnimationState(bool isMoving, bool isAttacking)
    {
        anim.SetBool("IsMoving", isMoving);
        anim.SetBool("IsAttacking", isAttacking);
    }

    private void Flip()
    {
        if (player == null) return;

        if (isReturningToSpawn)
        {
            bool spawnIsRight = spawnPosition.x > transform.position.x;
            float scaleX = Mathf.Abs(transform.localScale.x);

            transform.localScale = spawnIsRight
                ? new Vector3(scaleX, transform.localScale.y, transform.localScale.z)
                : new Vector3(-scaleX, transform.localScale.y, transform.localScale.z);
            return;
        }

        bool playerIsRight = player.position.x > transform.position.x;
        float scaleXPlayer = Mathf.Abs(transform.localScale.x);

        transform.localScale = playerIsRight
            ? new Vector3(scaleXPlayer, transform.localScale.y, transform.localScale.z)
            : new Vector3(-scaleXPlayer, transform.localScale.y, transform.localScale.z);
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip, soundVolume);
        }
    }
}