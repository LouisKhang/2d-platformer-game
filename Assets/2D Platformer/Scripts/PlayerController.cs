using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Platformer
{
    public class PlayerController : MonoBehaviour
    {
        // =============================================================
        // CÁC THUỘC TÍNH (PROPERTIES)
        // =============================================================
        
        [Header("Ground Check")]
        public Transform groundCheck;
        public float checkRadius = 0.2f; 
        public LayerMask whatIsGround; 
        private bool isGrounded;
        
        [Header("Head Check")]
        public Transform headCheck; 
        public LayerMask whatIsHeadBlock; 
        private bool isHeadBlocked;
        
        // === THUỘC TÍNH CHO SÁT THƯƠNG VÀ MIỄN NHIỄM ===
        [Header("Damage & Invulnerability")]
        public float invulnerabilityDuration = 1.0f;
        // Thời gian miễn nhiễm sau khi bị tấn công
        private bool isInvulnerable = false;
        // Cờ miễn nhiễm
        // ==================================================
        
        // === 🛡️ THUỘC TÍNH CHO SHIELD SKILL ===
        [Header("Shield Skill")]
        public GameObject shieldPrefab;
        // Prefab của khiên (hiệu ứng hình ảnh)
        public float shieldDuration = 7.0f;
        // Thời gian khiên hoạt động (7 giây)
        public float shieldCooldown = 30.0f;
        // Thời gian hồi chiêu (30 giây)
        private bool isShieldActive = false;
        // Cờ theo dõi khiên có đang bật không
        private float nextShieldTime = 0f;
        // Thời điểm có thể dùng lại skill
        private GameObject currentShieldInstance;
        // Instance của khiên đang hoạt động
        // ============================================
        
        // === 💥 THUỘC TÍNH CHO LUNGE ===
        [Header("Lunge / Dash")]
        public float lungeForce = 15f;
        public float lungeDuration = 0.2f; 
        private bool isLunging = false; 
        private bool canAirDash = true;
        // ===================================

        [Header("Movement")]
        public float movingSpeed = 5f;
        public float jumpForce = 6f; 
        
        // ⭐ THUỘC TÍNH MỚI: Hệ số làm chậm (Slow Factor)
        private float currentSlowFactor = 1.0f;
        // 1.0 = Tốc độ bình thường
        
        private float moveInput;
        private float verticalInput;
        
        private Collider2D currentPlatformCollider;
        private bool isDroppingThrough = false;
        
        [Header("Climb")] 
        public float climbSpeed = 7f; 
        public float slideSpeed = 6f;
        private bool canClimb = false; 
        private bool isClimbing = false;
        
        [HideInInspector] 
        public float originalGravityScale;

        private Vector2 alignPosition; 
        private bool isAligningX = false;
        private bool isClimbingChain = false; 
        private int contactCount = 0;
        
        [Header("Chain Pathing")]
        private EdgeCollider2D currentChainPath; 
        private Vector2[] pathPointsWorld; 
        private float[] pathDistances;
        private float pathLength; 
        private float currentPathPosition; 
        private float playerColliderHeightOffset = 0.5f;
        
        [Header("Bomb System")]
        public GameObject bombPrefab; 
        public Transform bombSpawnPoint; 
        public float bombThrowForce = 12f;
        
        [HideInInspector] // ⭐ ẨN ĐI VÌ ĐƯỢC QUẢN LÍ BỞI GAMEMANAGER
        public int currentBombs = 0;
        private bool facingRight = false;
        
        [HideInInspector]
        public bool deathState = false;
        [HideInInspector]
        public bool isInputLocked = false;
        [HideInInspector]
        public float maxJumpVelocity = 0f;
        [HideInInspector]
        public bool hasKey = false;

        private Rigidbody2D rigidbody;
        private Animator animator;
        private GameManager gameManager;
        private SpriteRenderer spriteRenderer; 

        [HideInInspector]
        public BoatController nearBoat;
        
        [Header("Audio Settings")]
        public AudioSource audioSource;

        public AudioClip jumpSFX;
        public AudioClip hurtSFX;
        public AudioClip dashSFX;
        public AudioClip dieSFX;
        public AudioClip climbSFX;
        public AudioSource climbAudioSource;
        
        // ===== MOBILE INPUT =====
        private bool useMobileInput = false;
        private float mobileHorizontal;
        private float mobileVertical;
        private bool mobileJump;
        private bool mobileDash;


        
        // =============================================================
        // START & HÀM HỖ TRỢ
        // =============================================================

        void Start()
        {
            rigidbody = GetComponent<Rigidbody2D>();
            animator = GetComponent<Animator>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            
            // ⭐ KHẮC PHỤC LỖI INPUT: Mặc định tắt Mobile Input khi khởi động ⭐
            useMobileInput = false; 
            
            originalGravityScale = rigidbody.gravityScale;
            rigidbody.freezeRotation = true; 
            
            Collider2D playerCol = GetComponent<Collider2D>();
            if (playerCol != null)
            {
                if (playerCol is CapsuleCollider2D capsule)
                {
                    playerColliderHeightOffset = capsule.size.y / 2f * transform.localScale.y;
                }
                else if (playerCol is BoxCollider2D box)
                {
                    playerColliderHeightOffset = box.size.y / 2f * transform.localScale.y;
                }
            }

            gameManager = FindObjectOfType<GameManager>();
            if (gameManager == null)
            {
                Debug.LogError("🚨 Không tìm thấy GameManager trong Scene! Cần Game Manager để quản lý game state.");
            }

            Invoke("MeasureInitialJump", 0.5f);
        }

        // ===== MOBILE API =====
        public void SetMobileMove(float h, float v)
        {
            useMobileInput = true;
            mobileHorizontal = h;
            mobileVertical = v;
        }

        public void SetMobileJump()
        {
            useMobileInput = true;
            mobileJump = true;
        }

        public void SetMobileDash()
        {
            useMobileInput = true;
            mobileDash = true;
        }
        
        /// <summary>
        /// Khắc phục lỗi input: Tắt chế độ Mobile Input.
        /// </summary>
        public void SetMobileInputInactive()
        {
            useMobileInput = false;
        }

        private void PlaySFX(AudioClip clip, float volume = 1f)
        {
            if (audioSource != null && clip != null)
                audioSource.PlayOneShot(clip, volume);
        }

        private void MeasureInitialJump()
        {
            if (rigidbody.mass > 0)
            {
                maxJumpVelocity = jumpForce / rigidbody.mass;
            }
        }

        private void FixedUpdate()
        {
            CheckGround();
            CheckHead();
            
            // Reset Air Dash khi chạm đất
            if (isGrounded && !isLunging)
            {
                canAirDash = true;
            }
            
            if (isInputLocked && !deathState && !isLunging)
            {
                // Cho phép Unity xử lý trọng lực và va chạm tự nhiên.
            }
        }

        private IEnumerator DisablePlatformCollision()
        {
            Collider2D playerCollider = GetComponent<Collider2D>();
            Collider2D platformToIgnore = currentPlatformCollider;
            
            isDroppingThrough = true;
            currentPlatformCollider = null;
            if (playerCollider != null && platformToIgnore != null)
            {
                Physics2D.IgnoreCollision(playerCollider, platformToIgnore, true);
            }
            
            yield return new WaitForSeconds(0.5f);
            if (platformToIgnore != null)
            {
                Physics2D.IgnoreCollision(playerCollider, platformToIgnore, false);
            }
            isDroppingThrough = false;
        }

        // ===================================================
        // HÀM NÉM BOMB - TÍCH HỢP VỚI GAMEMANAGER 
        // ===================================================
        private void ThrowBomb()
        {
            if (bombPrefab == null || bombSpawnPoint == null) 
            {
              
                Debug.LogError("Bomb Prefab or Spawn Point is missing! Throw canceled.");
                return;
            }
            
            // KIỂM TRA QUA GAMEMANAGER THAY VÌ currentBombs
            if (gameManager == null || gameManager.bombCounter <= 0)
            {
                Debug.Log("💣 Không còn Bom để ném!");
                return;
            }
            
            // TRỪ BOMB QUA GAMEMANAGER
            gameManager.UseBomb();
            GameObject bomb = Instantiate(bombPrefab, bombSpawnPoint.position, Quaternion.identity);
            
            Rigidbody2D bombRb = bomb.GetComponent<Rigidbody2D>();
            if (bombRb != null)
            {
                float throwDir = facingRight ?
                1f : -1f; 
                Vector2 throwForce = new Vector2(throwDir * bombThrowForce, 0.5f * bombThrowForce);
                
                bombRb.AddForce(throwForce, ForceMode2D.Impulse);
            }
        }

        private void InitializeChainPath(EdgeCollider2D chainCollider)
        {
            currentChainPath = chainCollider;
            Vector2[] localPoints = currentChainPath.points;
            pathPointsWorld = new Vector2[localPoints.Length];
            
            for (int i = 0; i < localPoints.Length; i++)
            {
                pathPointsWorld[i] = currentChainPath.transform.TransformPoint(localPoints[i]);
            }

            pathDistances = new float[pathPointsWorld.Length];
            pathDistances[0] = 0f;
            pathLength = 0f;

            for (int i = 1; i < pathPointsWorld.Length; i++)
            {
                float segmentLength = Vector2.Distance(pathPointsWorld[i - 1], pathPointsWorld[i]);
                pathLength += segmentLength;
                pathDistances[i] = pathLength;
            }

            currentPathPosition = GetClosestPathPosition(transform.position);
        }

        private float GetClosestPathPosition(Vector2 worldPosition)
        {
            float minDistance = float.MaxValue;
            float closestPosition = 0f;
            
            for (int i = 0; i < pathPointsWorld.Length - 1; i++)
            {
                Vector2 p1 = pathPointsWorld[i];
                Vector2 p2 = pathPointsWorld[i + 1];

                float lengthSq = (p2 - p1).sqrMagnitude;
                if (lengthSq == 0.0) continue;
                
                Vector2 vectorP1P2 = p2 - p1;
                float t = Mathf.Clamp01(Vector2.Dot(worldPosition - p1, vectorP1P2) / lengthSq);
                Vector2 closestPoint = p1 + t * vectorP1P2;

                float distance = Vector2.Distance(worldPosition, closestPoint);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestPosition = pathDistances[i] + t * Vector2.Distance(p1, p2);
                }
            }
            return closestPosition;
        }

        private Vector2 GetPathPoint(float distance)
        {
            distance = Mathf.Clamp(distance, 0f, pathLength);
            for (int i = 0; i < pathPointsWorld.Length - 1; i++)
            {
                if (distance >= pathDistances[i] && distance <= pathDistances[i + 1])
                {
                    float segmentStartDist = pathDistances[i];
                    float segmentLength = pathDistances[i + 1] - pathDistances[i];
                    
                    float t = (distance - segmentStartDist) / segmentLength;
                    return Vector2.Lerp(pathPointsWorld[i], pathPointsWorld[i + 1], t);
                }
            }
            return pathPointsWorld[pathPointsWorld.Length - 1];
        }
        
        // =============================================================
        // HÀM HỖ TRỢ LÊN/XUỐNG THUYỀN 
        // =============================================================

        public void EnterBoat(BoatController boat)
        {
            nearBoat = boat;
            nearBoat.EnterBoat(this); 
        }

        public void ExitBoat() 
        {
            if (nearBoat != null && nearBoat.playerOnBoat)
            {
                nearBoat.RequestExitBoat();
            }
        }
        
        // =============================================================
        // HÀM SHIELD SKILL (KỸ NĂNG KHIÊN)
        // =============================================================
        
        /// <summary>
        /// Kích hoạt Shield Skill.
        /// Gọi hàm này khi người chơi nhấn phím Shield (ví dụ: phím I).
        /// </summary>
        public void TryActivateShield()
        {
            // Kiểm tra xem Shield đã đang hoạt động chưa
            if (isShieldActive)
            {
                Debug.Log("🛡️ Shield đang hoạt động!");
                return;
            }

            // Kiểm tra Cooldown
            if (Time.time >= nextShieldTime)
            {
                StartCoroutine(ShieldRoutine());
            }
            else
            {
                float remainingCooldown = nextShieldTime - Time.time;
                Debug.Log($"⏳ Shield đang hồi chiêu. Còn {remainingCooldown:F1}s.");
            }
        }

        /// <summary>
        /// Coroutine xử lý logic Shield Skill
        /// </summary>
        private IEnumerator ShieldRoutine()
        {
            // Bật Shield
            isShieldActive = true;
            isInvulnerable = true; // Bật miễn nhiễm
            
            // Tạo hiệu ứng hình ảnh Shield (nếu có prefab)
            if (shieldPrefab != null)
            {
                currentShieldInstance = Instantiate(shieldPrefab, transform.position, Quaternion.identity, transform);
                Debug.Log("🛡️ Shield Kích hoạt! Miễn nhiễm trong 7 giây.");
            }
            else
            {
                Debug.LogWarning("⚠️ Shield Prefab chưa được gán! Shield vẫn hoạt động nhưng không có hiệu ứng hình ảnh.");
            }

            // Thời gian duy trì Shield (7 giây)
            yield return new WaitForSeconds(shieldDuration);
            
            // Tắt Shield
            isShieldActive = false;
            isInvulnerable = false; // Tắt miễn nhiễm

            // Hủy hiệu ứng hình ảnh
            if (currentShieldInstance != null)
            {
                Destroy(currentShieldInstance);
                currentShieldInstance = null;
            }

            // Bắt đầu Cooldown
            nextShieldTime = Time.time + shieldCooldown;
            Debug.Log($"🛡️ Shield kết thúc. Bắt đầu hồi chiêu {shieldCooldown}s.");
        }

        /// <summary>
        /// Kiểm tra xem Shield có đang hoạt động không
        /// </summary>
        public bool IsShieldActive()
        {
            return isShieldActive;
        }

        /// <summary>
        /// Lấy thời gian cooldown còn lại của Shield
        /// </summary>
        public float GetShieldCooldownRemaining()
        {
            if (Time.time >= nextShieldTime)
                return 0f;
            return nextShieldTime - Time.time;
        }
        
        // =============================================================
        // HÀM GÂY SÁT THƯƠNG (API) & MIỄN NHIỄM
        // =============================================================
        
        public void TakeDamage(int damage)
        {
   
            // Shield đang bật hoặc đang miễn nhiễm thì bỏ qua
            if (isShieldActive || isInvulnerable || isInputLocked || deathState)
            {
                if (isShieldActive)
                    Debug.Log("🛡️ Shield chặn sát thương!");
                return;
            }
            PlaySFX(hurtSFX);
            
            // Gửi thông báo lên GameManager để trừ tim
            GameManager gm = FindObjectOfType<GameManager>();
            if (gm != null)
                gm.PlayerHit(damage);
                
            // Nhấp nháy bất tử tạm thời
            StartCoroutine(InvulnerabilityRoutine());
        }


        private IEnumerator InvulnerabilityRoutine()
        {
            isInvulnerable = true;
            float flashInterval = 0.1f;
            float timer = 0f;
            
            // Hiệu ứng flash (nhấp nháy)
            while (timer < invulnerabilityDuration)
            {
                if (spriteRenderer != null)
                {
                    spriteRenderer.enabled = !spriteRenderer.enabled;
                }
                yield return new WaitForSeconds(flashInterval);
                timer += flashInterval;
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = true;
                // Đảm bảo hiển thị lại
            }
            
            isInvulnerable = false;
        }
        
        // =============================================================
        // HÀM KIỂM SOÁT HIỆU ỨNG SLOW (LÀM CHẬM)
        // =============================================================

        /// <summary>
        /// Áp dụng hệ số làm chậm lên tốc độ di chuyển của nhân vật.
        /// </summary>
        /// <param name="factor">Hệ số làm chậm (0.0f đến 1.0f).
        /// Ví dụ: 0.5f là giảm còn 50% tốc độ.</param>
        public void ApplySlowFactor(float factor)
        {
            currentSlowFactor = Mathf.Clamp(factor, 0.0f, 1.0f);
            Debug.Log($"Player bị Slow: Tốc độ x {currentSlowFactor}");
        }

        /// <summary>
        /// Xóa bỏ hiệu ứng làm chậm, khôi phục tốc độ di chuyển bình thường.
        /// </summary>
        public void RemoveSlowFactor()
        {
            currentSlowFactor = 1.0f;
            Debug.Log("Player Slow kết thúc: Tốc độ khôi phục.");
        }
        
        // =============================================================
        // HÀM KIỂM SOÁT MOVEMENT 
        // =============================================================

        public void DisableMovement()
        {
            isInputLocked = true;
            animator.SetInteger("playerState", 0); 

            if (isClimbing)
            {
                isClimbing = false;
                canClimb = false;
                isClimbingChain = false;
                isAligningX = false;
                rigidbody.gravityScale = originalGravityScale;
            }
        }

        public void EnableMovement()
        {
            isInputLocked = false;
        }
        
        // =============================================================
        // LOGIC LUNGE/DASH
        // =============================================================
        private void HandleLungeInput()
        {
            if (Input.GetKeyDown(KeyCode.L) && !isLunging && !isClimbing && !isInputLocked) 
            {
              
                if (isGrounded || canAirDash)
                {
                    StartCoroutine(LungeRoutine());
                }
            }
        }
        public void PlayDieSFX()
        {
            PlaySFX(dieSFX);
        }

        private IEnumerator LungeRoutine()
        {
            if (!isGrounded)
            {
                canAirDash = false;
            }
            
            isLunging = true;
            float originalGravity = rigidbody.gravityScale;
            
            rigidbody.gravityScale = 0f;
            DisableMovement();

            float lungeDirection = facingRight ? 1f : -1f;
            rigidbody.velocity = new Vector2(lungeDirection * lungeForce, 0f);
            PlaySFX(dashSFX);

            yield return new WaitForSeconds(lungeDuration);

            isLunging = false;
            EnableMovement();
            rigidbody.gravityScale = originalGravity;
            rigidbody.velocity = new Vector2(rigidbody.velocity.x * 0.2f, rigidbody.velocity.y);
        }
        
        // =============================================================
        // UPDATE (LOGIC CHÍNH)
        // =============================================================
        
        void Update()
    {
        // ================== SHIELD ==================
        if (Input.GetKeyDown(KeyCode.I))
        {
       
             TryActivateShield();
        }

        if (isShieldActive && currentShieldInstance != null)
        {
            currentShieldInstance.transform.position = transform.position;
        }

        // ================== BOAT ==================
        if (nearBoat != null && Input.GetKeyDown(KeyCode.Y))
        {
            if (!nearBoat.playerOnBoat)
                EnterBoat(nearBoat);
            else
                nearBoat.RequestExitBoat();

            return;
        }

        if (deathState) return;
        
        // ================== DASH ==================
        HandleLungeInput();
        if (isLunging) return;
        
        // ================== INPUT (PC + MOBILE) ==================
        if (useMobileInput)
        {
            moveInput = mobileHorizontal;
            verticalInput = mobileVertical;
        }
        else
        {
            moveInput = Input.GetAxisRaw("Horizontal");
            verticalInput = Input.GetAxis("Vertical");
        }

        if (isInputLocked)
        {
            rigidbody.velocity = new Vector2(0f, rigidbody.velocity.y);
            return;
        }

        // ================== DROP PLATFORM ==================
        if (currentPlatformCollider != null && isGrounded && verticalInput < -0.01f && !isDroppingThrough)
        {
            StartCoroutine(DisablePlatformCollision());
        }

        // ================== THROW BOMB ==================
        if (Input.GetKeyDown(KeyCode.U))
        {
            ThrowBomb();
        }

        // ================== CLIMB INPUT ==================
        if (canClimb)
        {
            if (Mathf.Abs(verticalInput) > 0.01f || Mathf.Abs(moveInput) > 0.01f || isClimbing || rigidbody.velocity.y < 0)
            {
                isClimbing = true;
            }

            if (isAligningX && !isClimbingChain && Mathf.Abs(transform.position.x - alignPosition.x) > 0.01f)
            {
                Vector3 newPos = transform.position;
                newPos.x = alignPosition.x;
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    newPos,
                    movingSpeed * currentSlowFactor * Time.deltaTime
                );
            }
        }

        // ================== CLIMB LOGIC ==================
        if (isClimbing)
        {
            rigidbody.gravityScale = 0f;
            rigidbody.velocity = Vector2.zero;

            if (isClimbingChain && currentChainPath != null)
            {
                float movementDelta =
                    (moveInput + verticalInput * 0.5f) *
                    slideSpeed *
                
                    currentSlowFactor *
                    Time.deltaTime;
                currentPathPosition += movementDelta;

                bool exitChain = false;

                if (currentPathPosition > pathLength)
                {
                    currentPathPosition = pathLength;
                    exitChain = true;
                }
                else if (currentPathPosition < 0f)
                {
                    currentPathPosition = 0f;
                    exitChain = true;
                }

                if (exitChain)
                {
                    isClimbing = false;
                    canClimb = false;
                    isClimbingChain = false;
                    rigidbody.gravityScale = originalGravityScale;

                    currentChainPath = null;
                    pathPointsWorld = null;
                    pathDistances = null;
                    rigidbody.velocity = new Vector2(moveInput * movingSpeed * currentSlowFactor, 0f);
                    return;
                }

                Vector2 targetPoint = GetPathPoint(currentPathPosition);
                Vector2 targetPos = targetPoint - Vector2.up * playerColliderHeightOffset;
                rigidbody.MovePosition(targetPos);
            }
            else
            {
                if (Mathf.Abs(verticalInput) > 0.01f)
                    rigidbody.velocity = new Vector2(0f, verticalInput * climbSpeed * currentSlowFactor);
                else
                    rigidbody.velocity = Vector2.zero;
            }

            if (isGrounded && Mathf.Abs(moveInput) > 0.01f)
            {
                isClimbing = false;
                canClimb = false;
                isClimbingChain = false;
                isAligningX = false;
                rigidbody.gravityScale = originalGravityScale;
            }

            animator.SetInteger("playerState", 3);
            return;
        }
        else
        {
            rigidbody.gravityScale = originalGravityScale;
        }

        // ================== MOVE + JUMP ==================
        if (Mathf.Abs(moveInput) < 0.01f && isGrounded)
        {
            rigidbody.velocity = new Vector2(0f, rigidbody.velocity.y);
            animator.SetInteger("playerState", 0);
        }
        else if (Mathf.Abs(moveInput) > 0.01f)
        {
            float speedFactor = isGrounded ?
            1f : 0.7f;
            rigidbody.velocity = new Vector2(
                moveInput * movingSpeed * speedFactor * currentSlowFactor,
                rigidbody.velocity.y
            );
            animator.SetInteger("playerState", 1);

            if (!facingRight && moveInput > 0) Flip();
            else if (facingRight && moveInput < 0) Flip();
        }
        else if (!isGrounded)
        {
            animator.SetInteger("playerState", 2);
        }

        // ================== JUMP ==================
        if ((Input.GetKeyDown(KeyCode.Space) || mobileJump) && isGrounded)
        {
            mobileJump = false;
            rigidbody.velocity = new Vector2(rigidbody.velocity.x, 0f);
            rigidbody.AddForce(transform.up * jumpForce, ForceMode2D.Impulse);

            animator.SetInteger("playerState", 2);
            PlaySFX(jumpSFX);
        }

        if (!isGrounded)
        {
            animator.SetInteger("playerState", 2);
        }
    }

        // =============================================================
        // HÀM HỖ TRỢ & XỬ LÝ VA CHẠM
        // =============================================================

        private void Flip() 
        {
            facingRight = !facingRight;
            Vector3 Scaler = transform.localScale;
            Scaler.x *= -1;
            transform.localScale = Scaler;
        }

        private void CheckGround()
        {
            bool wasGrounded = isGrounded;
            Vector2 center = groundCheck.position;
            float offsetX = 0.2f; 
            Vector2 left = new Vector2(center.x - offsetX, center.y);
            Vector2 right = new Vector2(center.x + offsetX, center.y);

            isGrounded =
                Physics2D.OverlapCircle(center, checkRadius, whatIsGround) ||
                Physics2D.OverlapCircle(left, checkRadius, whatIsGround) ||
                Physics2D.OverlapCircle(right, checkRadius, whatIsGround);

            if (wasGrounded && !isGrounded && currentPlatformCollider != null && !isDroppingThrough)
            {
                currentPlatformCollider = null;
            }
        }
        
        private void CheckHead()
        {
            if (headCheck != null)
            {
                isHeadBlocked = Physics2D.OverlapCircle(headCheck.transform.position, checkRadius, whatIsHeadBlock);
            }
            else
            {
                isHeadBlocked = false;
            }
        }

        private void OnCollisionEnter2D(Collision2D other) 
        {
            if (other.gameObject.tag == "Enemy") 
            {
                TakeDamage(1);
            } 
        }

        private void OnCollisionStay2D(Collision2D other) 
        {
            if (other.gameObject.CompareTag("OneWayPlatform") && !isDroppingThrough) 
            {
                if (rigidbody.velocity.y <= 0.01f)
   
                {
                    currentPlatformCollider = other.collider;
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.gameObject.tag == "Enemy") 
            {
                TakeDamage(1);
            } 

            if (other.gameObject.tag == "Ladder")
            {
                contactCount++;
                canClimb = true; 
                if (contactCount == 1)
                    PlaySFX(climbSFX);
                {
                    alignPosition.x = other.bounds.center.x;
                    isAligningX = true;
                    isClimbingChain = false; 
                }
            }

            if (other.gameObject.tag == "Chain")
            {
                contactCount++;
                canClimb = true; 
                if (contactCount == 1)
                {
                    isAligningX = false;
                    isClimbingChain = true; 
                    
                    EdgeCollider2D edge = other.GetComponent<EdgeCollider2D>() ?? other.GetComponentInParent<EdgeCollider2D>();
                    if (edge != null)
                    {
                        InitializeChainPath(edge);
                    }
                }
            }

            // NHẶT BOMB - TÍCH HỢP VỚI GAMEMANAGER
            if (other.gameObject.CompareTag("BombItem"))
            {
                if (gameManager != null)
       
                {
                    gameManager.AddBomb(1);
                }
                Destroy(other.gameObject);
            }

            if (other.gameObject.tag == "Coin")
            {
                if (gameManager != null) gameManager.coinsCounter += 1;
                Destroy(other.gameObject);
            }
            
            if (other.gameObject.tag == "Heart_Pickup")
            {
                if (gameManager != null) gameManager.AddHeart();
                Destroy(other.gameObject);
            }

            if (other.gameObject.tag == "CheckPoint")
            {
                if (gameManager != null) gameManager.UpdateCheckpoint(other.transform.position);
            }
            
            if (other.gameObject.tag == "Key") 
            {
                hasKey = true;
                Destroy(other.gameObject);
                if (gameManager != null) gameManager.UpdateKeyStatus(true);
            }

            if (other.gameObject.tag == "Door") 
            {
                if (gameManager != null)
                {
                    if (hasKey)
          
                    {
                        hasKey = false;
                        gameManager.UpdateKeyStatus(false);
                    }
                }
            }
        }
        
        public void ResetInvulnerability()
        {
            StopAllCoroutines();
            isInvulnerable = false;

            if (spriteRenderer != null)
                spriteRenderer.enabled = true;
        }
        
        public void SetInvulnerable(bool value)
        {
            isInvulnerable = value;
        }

        private IEnumerator GoThroughCollider(Collider2D colliderToIgnore)
        {
            Collider2D playerCol = GetComponent<Collider2D>();
            if (playerCol != null && colliderToIgnore != null)
                Physics2D.IgnoreCollision(playerCol, colliderToIgnore, true);
            yield return new WaitForSeconds(0.5f);

            if (playerCol != null && colliderToIgnore != null)
                Physics2D.IgnoreCollision(playerCol, colliderToIgnore, false);
        }

        private void OnTriggerExit2D(Collider2D other) 
        {
            if (other.gameObject.tag == "Ladder" || other.gameObject.tag == "Chain")
            {
                contactCount--;
                if (contactCount <= 0)
                {
                    canClimb = false;
                    isClimbing = false;
                    isClimbingChain = false; 
                    isAligningX = false;
                    rigidbody.gravityScale = originalGravityScale; 
                    contactCount = 0; 
                    
                    currentChainPath = null;
                    pathPointsWorld = null;
                    pathDistances = null;
                    
                    if (verticalInput < -0.01f) 
                    {
                        rigidbody.AddForce(Vector2.down * jumpForce * 0.75f, ForceMode2D.Impulse);
                    }
                }
            }
        }
    }
}