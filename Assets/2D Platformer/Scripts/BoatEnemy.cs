using System.Collections;
using UnityEngine;

namespace Platformer
{
    public class BoatEnemy : MonoBehaviour
    {
        [Header("Boat Settings")]
        public float moveSpeed = 3f;
        [Tooltip("Phạm vi tuần tra tương đối tính từ vị trí bắt đầu.")]
        public float patrolDistance = 5f;

        [Header("Health")]
        public int maxHealth = 10;
        private int currentHealth;

        [Header("Detection / Attack")]
        public Vector2 sightBoxSize = new Vector2(10f, 2f);
        public float sightBoxOffset = 0.5f;
        public LayerMask playerLayer;

        public Transform firePoint;
        public GameObject bombPrefab;
        public float fireCooldown = 5f;
        public Vector2 attackRangeSize = new Vector2(8f, 2f);
        public float attackRangeOffset = 0.5f;
        public float bombSpeed = 10f;

        [Header("Effects")]
        public GameObject hurtPrefab;
        public GameObject explodePrefab;
        public Transform[] hurtPoints;

        [Header("Audio")]
        public AudioClip hurtSound;
        [Range(0f, 2f)] public float hurtVolume = 1f;
        public AudioClip dieSound;
        [Range(0f, 2f)] public float dieVolume = 1f;

        [Header("Boat Area Limit (Tuyệt Đối)")]
        [Tooltip("Giới hạn X bên trái tuyệt đối cho toàn bộ khu vực hoạt động.")]
        public Transform leftBoundary;
        [Tooltip("Giới hạn X bên phải tuyệt đối cho toàn bộ khu vực hoạt động.")]
        public Transform rightBoundary;

        [Header("Rock Sinking")]
        public Animator rockAnimator; 

        private const float LKP_WAIT_DURATION = 2f;

        private Rigidbody2D rb;
        private Transform player;
        private Vector2 startPos;
        private int moveDirection = 1;
        private bool isPatrolling = true;
        private bool isWaiting = false; 
        private bool isFlipping = false;
        private bool isDead = false;
        private bool isReturningToLKP = false; 
        private Vector2 lastKnownPlayerPosition; 
        private float currentTimer = 0f; 
        private bool canFire = true;

        private SpriteRenderer[] spriteRenderers;
        private Color[] originalColors;
        private AudioSource audioSource;

        void Start()
        {
            rb = GetComponent<Rigidbody2D>();
            if (rb != null) rb.gravityScale = 0f;

            currentHealth = maxHealth;
            startPos = transform.position;

            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) player = p.transform;

            spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
            originalColors = new Color[spriteRenderers.Length];
            for (int i = 0; i < spriteRenderers.Length; i++)
                originalColors[i] = spriteRenderers[i].color;

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

            if (moveDirection == -1) FlipImmediate();

            if (rockAnimator == null)
                Debug.LogWarning("⚠️ Rock Animator chưa được gán trên BoatEnemy! Khối đá sẽ không chìm.");
        }

        void Update()
        {
            if (isDead || isFlipping) { rb.velocity = Vector2.zero; return; }

            bool playerVisible = CheckPlayerVisible();
            bool playerInRange = CheckPlayerInAttackRange();

            if (playerVisible && playerInRange && canFire)
                FireBomb();

            if (playerVisible && player != null)
            {
                isPatrolling = isReturningToLKP = isWaiting = false;
                lastKnownPlayerPosition = player.position; 
                
                float direction = (player.position.x > transform.position.x) ? 1f : -1f;
                if (moveDirection != (int)direction)
                {
                    moveDirection = (int)direction;
                    StartCoroutine(SafeFlip()); 
                }
                
                ChasePlayer();
            }
            else
            {
                if (isReturningToLKP)
                    ReturnToLKP(); 
                else 
                    ProcessPatrolAndLKP(); 
            }

            ClampInsideBoatAreaAbsolute();
        }

        void ClampInsideBoatAreaAbsolute()
        {
            if (leftBoundary == null || rightBoundary == null) return;

            float minX = leftBoundary.position.x;
            float maxX = rightBoundary.position.x;

            Vector3 pos = transform.position;
            pos.x = Mathf.Clamp(pos.x, minX, maxX);
            transform.position = pos;

            if ((transform.position.x <= minX + 0.1f && rb.velocity.x < 0) || 
                (transform.position.x >= maxX - 0.1f && rb.velocity.x > 0))
            {
                rb.velocity = Vector2.zero;
                if (!isWaiting) 
                {
                    isPatrolling = false; 
                    isReturningToLKP = false;
                    moveDirection = (transform.position.x <= minX + 0.1f) ? 1 : -1; 
                    StartCoroutine(WaitAndFlip()); 
                }
            }
        }

        #region Movement
        void ProcessPatrolAndLKP() 
        {
            if (isWaiting)
            {
                rb.velocity = Vector2.zero;
                currentTimer += Time.deltaTime;

                if (currentTimer >= LKP_WAIT_DURATION) 
                {
                    isWaiting = false;
                    currentTimer = 0f;
                    isPatrolling = true; 
                    StartCoroutine(SafeFlip()); 
                }
                return;
            }
            
            Patrol();
        }

        void Patrol()
        {
            float limitLeft = startPos.x - patrolDistance;
            float limitRight = startPos.x + patrolDistance;
            
            float currentX = transform.position.x;
            bool shouldTurn = false;

            if (moveDirection == 1 && currentX >= limitRight)
            {
                shouldTurn = true;
                moveDirection = -1;
            }
            else if (moveDirection == -1 && currentX <= limitLeft)
            {
                shouldTurn = true;
                moveDirection = 1;
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

            float limitLeft = startPos.x - patrolDistance;
            float limitRight = startPos.x + patrolDistance;
            
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
            
            float direction = (clampedLKP.x > transform.position.x) ? 1f : -1f;
            if (moveDirection != (int)direction)
            {
                moveDirection = (int)direction;
                StartCoroutine(SafeFlip());
                return;
            }

            Vector2 targetVelocity = new Vector2(direction * moveSpeed, 0f); 
            rb.velocity = targetVelocity;
        }
        
        void ChasePlayer()
        {
            if (isFlipping) return;

            float direction = (player.position.x > transform.position.x) ? 1f : -1f;

            if (moveDirection != (int)direction)
            {
                moveDirection = (int)direction;
                StartCoroutine(SafeFlip());
            }

            Vector2 targetVelocity = new Vector2(direction * moveSpeed, 0f); 
            rb.velocity = targetVelocity;
        }

        IEnumerator WaitAndFlip()
        {
            if (isWaiting) yield break;
            isWaiting = true;
            rb.velocity = Vector2.zero;
            currentTimer = 0f;

            yield return new WaitForSeconds(LKP_WAIT_DURATION); 

            isWaiting = false;
            isPatrolling = true; 
            currentTimer = 0f;

            yield return StartCoroutine(SafeFlip());
        }

        private void FlipImmediate()
        {
             Vector3 scale = transform.localScale;
             scale.x = Mathf.Abs(scale.x) * moveDirection; 
             transform.localScale = scale;
        }

        IEnumerator SafeFlip()
        {
            isFlipping = true; 
            rb.velocity = Vector2.zero; 
            FlipImmediate();
            yield return new WaitForSeconds(0.15f);
            isFlipping = false;
        }
        #endregion

        #region Detection & Combat
        bool CheckPlayerVisible()
        {
            if (player == null) return false;
            Vector2 center = new Vector2(transform.position.x, transform.position.y + sightBoxOffset);
            Collider2D[] hits = Physics2D.OverlapBoxAll(center, sightBoxSize, 0f, playerLayer);
            return hits.Length > 0;
        }

        bool CheckPlayerInAttackRange()
        {
            if (player == null) return false;
            float distanceX = Mathf.Abs(player.position.x - transform.position.x);
            float distanceY = Mathf.Abs(player.position.y - transform.position.y);
            return distanceX <= attackRangeSize.x / 2f && distanceY <= attackRangeSize.y / 2f;
        }

        void FireBomb()
        {
            if (bombPrefab == null || firePoint == null || player == null) return;
            GameObject bomb = Instantiate(bombPrefab, firePoint.position, firePoint.rotation);
            Rigidbody2D rbBomb = bomb.GetComponent<Rigidbody2D>();
            if (rbBomb != null)
            {
                rbBomb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                Vector2 direction = (player.position - firePoint.position).normalized;
                rbBomb.velocity = direction * bombSpeed;
            }
            canFire = false;
            StartCoroutine(ResetFireCooldown());
        }

        IEnumerator ResetFireCooldown()
        {
            yield return new WaitForSeconds(fireCooldown);
            canFire = true;
        }

        public void TakeDamage(int dmg)
        {
            if (isDead) return;
            currentHealth -= dmg;

            // Phát âm thanh hurt
            if (hurtSound != null && audioSource != null)
                audioSource.PlayOneShot(hurtSound, hurtVolume);

            if (hurtPrefab != null && hurtPoints != null && hurtPoints.Length > 0)
            {
                foreach (var p in hurtPoints)
                    Instantiate(hurtPrefab, p.position, Quaternion.identity);
            }

            if (currentHealth <= 0) Die();
        }

        void Die()
        {
            if (isDead) return;
            isDead = true;

            // Phát âm thanh die
            if (dieSound != null)
                AudioSource.PlayClipAtPoint(dieSound, transform.position, dieVolume);

            rb.velocity = Vector2.zero;
            if (rockAnimator != null) rockAnimator.SetTrigger("Sink"); 
            if (explodePrefab != null) Instantiate(explodePrefab, transform.position, Quaternion.identity);

            Collider2D[] cols = GetComponentsInChildren<Collider2D>();
            foreach (var c in cols) c.enabled = false;
            Destroy(gameObject, 0.5f);
        }
        #endregion

        #region Gizmos
        private void OnDrawGizmosSelected()
        {
            Vector3 currentStartPosition = (Application.isPlaying) ? (Vector3)startPos : transform.position;

            Gizmos.color = Color.cyan;
            Vector3 leftLimit = new Vector3(currentStartPosition.x - patrolDistance, currentStartPosition.y, currentStartPosition.z);
            Vector3 rightLimit = new Vector3(currentStartPosition.x + patrolDistance, currentStartPosition.y, currentStartPosition.z);
            Gizmos.DrawLine(leftLimit + Vector3.up * 1f, leftLimit + Vector3.down * 1f);
            Gizmos.DrawLine(rightLimit + Vector3.up * 1f, rightLimit + Vector3.down * 1f);
            
            Vector3 sightBoxCenter = new Vector3(transform.position.x, transform.position.y + sightBoxOffset, transform.position.z);
            Gizmos.color = Color.yellow; 
            Gizmos.DrawWireCube(sightBoxCenter, sightBoxSize);
            
            Vector3 attackBoxCenter = new Vector3(transform.position.x, transform.position.y + attackRangeOffset, transform.position.z);
            Gizmos.color = Color.red; 
            Gizmos.DrawWireCube(attackBoxCenter, attackRangeSize); 

            if (leftBoundary != null && rightBoundary != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(leftBoundary.position + Vector3.up * 5, leftBoundary.position + Vector3.down * 5);
                Gizmos.DrawLine(rightBoundary.position + Vector3.up * 5, rightBoundary.position + Vector3.down * 5);
            }
        }
        #endregion
    }
}
