using UnityEngine;
using System.Collections;

public class SkeletonCombatController : MonoBehaviour
{
    // =============================================================
    // THUỘC TÍNH (BỔ SUNG VÀ GIỮ NGUYÊN)
    // =============================================================

    [Header("Movement & Setup")]
    public float moveSpeed = 1f;
    public LayerMask wallLayer;
    public Collider2D attackHitbox; 

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private float currentMoveSpeed;

    // [BỔ SUNG] THUỘC TÍNH SOUND
    [Header("Audio")]
    public AudioClip attackSound; // Âm thanh khi quái vật tấn công
    private AudioSource audioSource; // Component phát âm thanh
    // [KẾT THÚC BỔ SUNG]

    [Header("AI & Attack")]
    public float sightRange = 5f;
    public float attackRange = 1.2f;
    public float attackCooldown = 2f;
    public float attackSpeed = 1.6f; 
    public LayerMask playerLayer;
    private Transform playerTarget;
    private float lastAttackTime;
    private bool isAttacking = false;
    private bool hasDealtDamage = false; 

    [Header("Health & Invulnerability")]
    [SerializeField] private int health = 3;
    private bool isDead = false;
    private bool isInvulnerable = false;
    public Color hurtColor = Color.red;
    private Color originalColor;
    public float flashDuration = 0.1f;

    [Header("Idle System")]
    public float patrolInterval = 10f;
    public float idleDuration = 3f;
    private float patrolTimer = 0f;
    private bool isIdle = false;

    private bool isInvestigating = false;

    // =============================================================
    // START & UPDATE (CÓ BỔ SUNG)
    // =============================================================

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // [BỔ SUNG] LẤY COMPONENT AUDIOSOURCE
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            // Nếu chưa có, thêm AudioSource component
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false; // Đảm bảo âm thanh không tự phát
        }
        // [KẾT THÚC BỔ SUNG]

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;

        currentMoveSpeed = moveSpeed;

        if (attackHitbox != null)
            attackHitbox.enabled = false;
    }

    void Update()
    {
        if (isDead) return;
        if (isAttacking)
        {
            rb.velocity = Vector2.zero;
            animator.SetBool("IsWalking", false);
            return;
        }

        if (isInvestigating)
            return;

        if (playerTarget == null)
        {
            patrolTimer += Time.deltaTime;
            if (!isIdle && patrolTimer >= patrolInterval)
            {
                StartCoroutine(IdleRoutine());
                patrolTimer = 0f;
            }
        }

        DetectPlayerAndAttackLogic();

        if (!isIdle && !isAttacking)
        {
            rb.velocity = new Vector2(currentMoveSpeed, rb.velocity.y);
            animator.SetBool("IsWalking", Mathf.Abs(currentMoveSpeed) > 0.01f);
        }
        else
        {
            rb.velocity = Vector2.zero;
            animator.SetBool("IsWalking", false);
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (((1 << collision.gameObject.layer) & wallLayer) != 0)
        {
            if (!isAttacking && !isDead)
                Flip();
        }
    }

    private void DetectPlayerAndAttackLogic()
    {
        if (isDead) return;

        Collider2D playerCheck = Physics2D.OverlapCircle(transform.position, sightRange, playerLayer);

        if (playerCheck != null)
        {
            playerTarget = playerCheck.transform;
            float distanceToPlayer = Vector2.Distance(transform.position, playerTarget.position);

            if (Mathf.Sign(playerTarget.position.x - transform.position.x) != Mathf.Sign(transform.localScale.x))
                Flip();

            if (distanceToPlayer <= attackRange && Time.time >= lastAttackTime + attackCooldown)
            {
                StartAttack();
            }
            else if (distanceToPlayer <= attackRange)
            {
                currentMoveSpeed = 0;
            }
            else
            {
                currentMoveSpeed = Mathf.Sign(playerTarget.position.x - transform.position.x) * moveSpeed;
            }
        }
        else
        {
            playerTarget = null;
            currentMoveSpeed = Mathf.Sign(transform.localScale.x) * moveSpeed;
        }
    }

    private void StartAttack()
    {
        isAttacking = true;
        currentMoveSpeed = 0;
        hasDealtDamage = false; 
        
        animator.speed = attackSpeed;

        animator.ResetTrigger("Hit");
        animator.SetTrigger("Attack");
        lastAttackTime = Time.time;
    }

    // =============================================================
    // HÀM GÂY SÁT THƯƠNG ĐƠN GIẢN (ĐƯỢC GỌI BẰNG ANIMATION EVENT)
    // =============================================================

    public void EnableAttackHitbox()
    {
        // Hàm này được gọi từ Animation Event tại thời điểm cú đánh
        if (attackHitbox == null || isDead || hasDealtDamage) return;

        // [BỔ SUNG] PHÁT ÂM THANH TẤN CÔNG
        PlayAttackSound();
        // [KẾT THÚC BỔ SUNG]

        // Bật Hitbox chỉ trong 1 frame kiểm tra va chạm
        attackHitbox.enabled = true;
        
        // Thực hiện logic gây sát thương ngay lập tức
        DamagePlayer();
        
        // Tắt Hitbox ngay lập tức sau khi kiểm tra để đảm bảo chỉ hit 1 lần
        attackHitbox.enabled = false;
    }

    // XÓA HÀM DisableAttackHitbox() CŨ VÌ NÓ KHÔNG CẦN THIẾT NỮA

    public void FinishAttack()
    {
        // Hàm này được gọi từ Animation Event ở cuối Animation Attack
        isAttacking = false;
        
        // Khôi phục tốc độ di chuyển
        currentMoveSpeed = Mathf.Sign(transform.localScale.x) * moveSpeed;

        // reset speed animation về bình thường
        animator.speed = 1f;
    }

    public void DamagePlayer()
    {
        // Kiểm tra lại lần nữa:
        if (attackHitbox == null || !attackHitbox.enabled || hasDealtDamage) return;

        // TÌM PLAYER TRONG PHẠM VI HITBOX
        Collider2D[] hitPlayers = Physics2D.OverlapBoxAll(
            attackHitbox.bounds.center,
            attackHitbox.bounds.size,
            0f,
            playerLayer
        );

        foreach (Collider2D playerCol in hitPlayers)
        {
            // Cần đảm bảo PlayerController của bạn nằm trong namespace Platformer
            Platformer.PlayerController player = playerCol.GetComponent<Platformer.PlayerController>();
            if (player != null)
            {
                player.TakeDamage(1);
                Debug.Log("⚔️ Skeleton HIT Player thành công! (Simple Damage)");
                hasDealtDamage = true; // Đánh dấu đã gây sát thương thành công
                return; // Chỉ cần tấn công một Player
            }
        }
    }

    // [BỔ SUNG] HÀM PHÁT ÂM THANH
    private void PlayAttackSound()
    {
        if (audioSource != null && attackSound != null)
        {
            audioSource.PlayOneShot(attackSound);
        }
    }
    // [KẾT THÚC BỔ SUNG]
    
    // =============================================================
    // CÁC HÀM CÒN LẠI (GIỮ NGUYÊN)
    // =============================================================

    public void TakeDamage(int damage, Vector3 attackerPosition)
    {
        if (isDead || isInvulnerable) return;

        health -= damage;

        if (health <= 0)
        {
            Die();
            return;
        }

        animator.SetTrigger("Hit");
        StartCoroutine(ReactToAttack(attackerPosition));
    }

    private IEnumerator ReactToAttack(Vector3 attackerPosition)
    {
        isInvulnerable = true;
        isAttacking = false;
        isIdle = false;

        if (spriteRenderer != null)
        {
            spriteRenderer.color = hurtColor;
            yield return new WaitForSeconds(flashDuration);
            spriteRenderer.color = originalColor;
        }

        float direction = Mathf.Sign(attackerPosition.x - transform.position.x);
        if ((direction > 0 && transform.localScale.x < 0) ||
            (direction < 0 && transform.localScale.x > 0))
        {
            Flip();
        }

        if (playerTarget == null)
        {
            StartCoroutine(InvestigateAttackSource(attackerPosition));
        }

        yield return new WaitForSeconds(0.3f);
        isInvulnerable = false;
    }

    private IEnumerator InvestigateAttackSource(Vector3 attackerPosition)
    {
        isInvestigating = true;
        animator.SetBool("IsWalking", true);

        float direction = Mathf.Sign(attackerPosition.x - transform.position.x);
        float investigateTime = 3f;
        float timer = 0f;

        while (timer < investigateTime && !isDead)
        {
            rb.velocity = new Vector2(direction * moveSpeed * 1.2f, rb.velocity.y);

            Collider2D playerCheck = Physics2D.OverlapCircle(transform.position, sightRange, playerLayer);
            if (playerCheck != null)
            {
                playerTarget = playerCheck.transform;
                isInvestigating = false;
                yield break;
            }

            timer += Time.deltaTime;
            yield return null;
        }

        rb.velocity = Vector2.zero;
        animator.SetBool("IsWalking", false);
        isInvestigating = false;
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        animator.SetTrigger("IsDead");

        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.simulated = false;
        }

        Collider2D[] cols = GetComponentsInChildren<Collider2D>();
        foreach (Collider2D col in cols)
            col.enabled = false;

        Destroy(gameObject, 2f);
    }

    private IEnumerator IdleRoutine()
    {
        isIdle = true;
        animator.SetBool("IsWalking", false);
        rb.velocity = Vector2.zero;
        yield return new WaitForSeconds(idleDuration);
        isIdle = false;
    }

    private void Flip()
    {
        transform.localScale = new Vector2(-transform.localScale.x, transform.localScale.y);
        currentMoveSpeed *= -1;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}