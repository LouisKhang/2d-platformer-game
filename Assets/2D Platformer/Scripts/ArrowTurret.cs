using UnityEngine;
using System.Collections;
using Platformer; 

public class ArrowTurret : MonoBehaviour
{
    // --- CÀI ĐẶT CHÍNH TRONG INSPECTOR ---
    
    [Header("Weapon Setup")]
    public GameObject arrowPrefab; 		
    public Transform firePoint; 		
    public int maxShotsPerCycle = 3; 	
    // PHẢI LỚN HƠN ĐỘ DÀI CỦA ANIMATION BẮN
    public float timeBetweenShots = 1.0f; 	
    public float cycleResetTime = 2.0f; // Thời gian hồi chiêu sau loạt bắn (ví dụ: 2s)
    
    // *** CÀI ĐẶT TẦM BẮN (SIGHT BOX) ***
    [Header("Activation Sight Box (180 deg)")]
    public Vector2 sightBoxSize = new Vector2(10f, 1.5f); 
    public float sightBoxOffset = 0.5f; 		
    public LayerMask playerLayer;

    [Header("Sound Effects")]
    public AudioClip shootSound;            // Âm thanh bắn mũi tên
    [Range(0f, 1f)]
    public float soundVolume = 0.7f;

    // --- BIẾN NỘI BỘ ---
    private Animator anim;
    private Transform player;
    private int currentShots = 0; 
    private bool isFiring = false; // Cờ kiểm soát trạng thái bận rộn (đang bắn hoặc đang hồi chiêu)
    private AudioSource audioSource;

    void Start()
    {
        anim = GetComponent<Animator>();
        if (firePoint == null) firePoint = transform;
        
        // Khởi tạo AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        audioSource.volume = soundVolume;
        
        GameObject playerObject = GameObject.FindWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
        }
    }

    private bool IsPlayerInSight()
    {
        if (player == null) return false;

        float facingDirection = Mathf.Sign(transform.localScale.x);

        Vector2 boxCenter = new Vector2(
            transform.position.x + (sightBoxSize.x / 2f * facingDirection), 
            transform.position.y + sightBoxOffset
        );
        
        Collider2D hit = Physics2D.OverlapBox(
            boxCenter, 
            sightBoxSize, 
            0f, 
            playerLayer
        );

        return hit != null;
    }

    void Update()
    {
        if (player == null) return;

        // KÍCH HOẠT NGAY khi player ở trong tầm VÀ turret không bận
        if (!isFiring && IsPlayerInSight())
        {
            StartFiringCycle();
        }
    }
    
    public void StartFiringCycle()
    {
        if (!isFiring)
        {
            isFiring = true; // Bắt đầu chu kỳ, Turret BẬN RỘN
            currentShots = 0;
            StartCoroutine(FiringCycleRoutine());
        }
    }

    // *** LOGIC COROUTINE ĐIỀU KHIỂN THỜI GIAN VÀ RESET ***
    private IEnumerator FiringCycleRoutine()
    {
        for (int i = 0; i < maxShotsPerCycle; i++)
        {
            // --- KIỂM TRA DỪNG TỨC THÌ ---
            if (!IsPlayerInSight())
            {
                currentShots = 0;
                isFiring = false; // Reset cờ BẬN RỘN
                yield break; // Ngắt Coroutine ngay lập tức
            }

            // Bắn (gọi Animation)
            anim.SetTrigger("Fire");
            currentShots++;
            
            // Chờ thời gian giữa các phát bắn
            yield return new WaitForSeconds(timeBetweenShots); 
        }

        // --- HỒI CHIÊU (2S) ---
        yield return new WaitForSeconds(cycleResetTime);
        
        // RESET: Cho phép Update() kiểm tra điều kiện bắn lại
        isFiring = false; 
        currentShots = 0;
    }

    // HÀM BẮN: Được gọi qua Animation Event
    public void ShootArrow()
    {
        if (arrowPrefab == null || firePoint == null) return;

        // Phát âm thanh bắn
        PlaySound(shootSound);

        GameObject arrow = Instantiate(arrowPrefab, firePoint.position, firePoint.rotation);
        
        ArrowProjectile arrowScript = arrow.GetComponent<ArrowProjectile>();
        if (arrowScript != null)
        {
             float arrowSpeed = arrowPrefab.GetComponent<ArrowProjectile>().speed;
             Vector2 firingVector = firePoint.right * arrowSpeed;
             arrowScript.velocityVector = firingVector; 
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
    
    // --- GIZMOS ---
    
    private void OnDrawGizmosSelected()
    {
        if (transform == null) return; 

        float facingDirection = Mathf.Sign(transform.localScale.x);

        Vector2 boxCenter = new Vector2(
            transform.position.x + (sightBoxSize.x / 2f * facingDirection), 
            transform.position.y + sightBoxOffset
        );

        Gizmos.color = Color.red; 
        Gizmos.DrawWireCube(boxCenter, sightBoxSize);
        
        Gizmos.DrawLine(
            transform.position, 
            boxCenter
        );
    }
}