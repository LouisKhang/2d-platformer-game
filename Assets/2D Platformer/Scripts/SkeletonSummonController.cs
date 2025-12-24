using UnityEngine;
using System.Collections;
using Platformer; // Đảm bảo namespace này tồn tại nếu PlayerController nằm trong đó

public class SkeletonSummonController : MonoBehaviour
{
    [Header("Movement & Setup")]
    public float moveSpeed = 3f; // <<< ĐÃ CHỈNH MẶC ĐỊNH LÀ 3f (Sẽ được Boss gán)
    public LayerMask wallLayer;
    [Tooltip("Collider được dùng làm Hitbox tấn công (phải là Is Trigger).")]
    public Collider2D attackHitbox;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private float currentMoveSpeed;

    [Header("AI & Attack")]
    public float sightRange = 5f;
    public float attackRange = 1.2f;
    public int attackDamage = 1; // Sát thương 1 máu
    public float attackCooldown = 2f;
    public float attackSpeed = 1.6f;   
    public LayerMask playerLayer;
    private Transform playerTarget;
    private float lastAttackTime;
    private bool isAttacking = false;

    [Header("Health & Invulnerability")]
    [SerializeField] private int health = 2; // <<< MÁU LÀ 2
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
    private bool isSummoned = false; // Cờ cho quái triệu hồi

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;

        currentMoveSpeed = moveSpeed;

        if (attackHitbox != null)
            attackHitbox.enabled = false;
    }

    // GỌI TỪ WIZARD KHI TRIỆU HỒI
    public void InitializeAI(Transform target)
    {
        isSummoned = true;
        playerTarget = target;
        currentMoveSpeed = moveSpeed;
        
        isIdle = false;
        isInvestigating = false;
        patrolTimer = 0f;
        
        if (target != null && Mathf.Sign(playerTarget.position.x - transform.position.x) != Mathf.Sign(transform.localScale.x))
            Flip();
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

        // Logic Patrol (Chỉ áp dụng nếu KHÔNG phải quái triệu hồi và không có mục tiêu)
        if (!isSummoned && playerTarget == null)
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
        }
        else if (isSummoned) // Nếu là quái triệu hồi và không thấy player, nó dừng lại
        {
            playerTarget = null;
            currentMoveSpeed = 0;
            return;
        }
        
        if (playerTarget != null)
        {
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
            currentMoveSpeed = Mathf.Sign(transform.localScale.x) * moveSpeed;
        }
    }

    private void StartAttack()
    {
        isAttacking = true;
        currentMoveSpeed = 0;
        animator.speed = attackSpeed;
        animator.ResetTrigger("Hit");
        animator.SetTrigger("Attack");
        lastAttackTime = Time.time;
    }

    // ** GỌI TỪ ANIMATION EVENT **
    public void EnableAttackHitbox()
    {
        if (attackHitbox != null)
        {
            attackHitbox.enabled = true;
            // Gọi hàm sát thương ngay khi Hitbox được kích hoạt
            DamagePlayer(); 
        }
    }

    // ** GỌI TỪ ANIMATION EVENT **
    public void DisableAttackHitbox()
    {
        if (attackHitbox != null)
        {
            attackHitbox.enabled = false;
        }
    }

    // HÀM GÂY SÁT THƯƠNG (Sử dụng logic OverlapBoxAll từ mẫu bạn cung cấp)
    public void DamagePlayer()
    {
        if (attackHitbox == null || !attackHitbox.enabled) return;

        Collider2D[] hitPlayers = Physics2D.OverlapBoxAll(
            attackHitbox.bounds.center,
            attackHitbox.bounds.size,
            0f,
            playerLayer
        );

        foreach (Collider2D playerCol in hitPlayers)
        {
            // Kiểm tra PlayerController trong namespace Platformer
            var player = playerCol.GetComponent<Platformer.PlayerController>();
            
            if (player != null)
            {
                player.TakeDamage(attackDamage); // Gây 1 sát thương
                Debug.Log("⚔️ Skeleton Summon HIT Player thành công!");
                return; 
            }
        }
    }

    public void FinishAttack()
    {
        isAttacking = false;
        if (playerTarget != null)
            currentMoveSpeed = Mathf.Sign(playerTarget.position.x - transform.position.x) * moveSpeed;
        else
            currentMoveSpeed = Mathf.Sign(transform.localScale.x) * moveSpeed;

        animator.speed = 1f;
    }
    
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
        if ((direction > 0 && transform.localScale.x < 0) || (direction < 0 && transform.localScale.x > 0))
        {
            Flip();
        }
        
        yield return new WaitForSeconds(0.3f);
        isInvulnerable = false;
    }
    
    // **********************************
    // HÀM DIE ĐƯỢC CHUYỂN THÀNH PUBLIC ĐỂ BOSS CÓ THỂ GỌI
    // **********************************
    public void Die() 
    { 
        if (isDead) return;
        isDead = true; 
        animator.SetTrigger("IsDead"); 
        
        // Dừng di chuyển và tắt vật lý
        if (rb != null) { rb.velocity = Vector2.zero; rb.simulated = false; }
        
        // Tắt tất cả Collider
        Collider2D[] cols = GetComponentsInChildren<Collider2D>();
        foreach (Collider2D col in cols) col.enabled = false;
        
        // Hủy đối tượng sau 2 giây (cho animation chết chạy)
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
        if (playerTarget == null)
        {
            currentMoveSpeed *= -1;
        }
    }

    void OnDrawGizmosSelected() 
    { 
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}