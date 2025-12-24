using UnityEngine;
using Platformer; 

public class BarrierController : MonoBehaviour
{
    // ⭐ CÁC THAM SỐ CÓ THỂ CHỈNH SỬA
    [Header("Cycle Timings (Seconds)")]
    [Tooltip("Thời gian Barrier ở trạng thái Idle (Laser BẬT).")]
    public float laserOnDuration = 2f; 
    [Tooltip("Thời gian Barrier ở trạng thái Deactivate (Laser TẮT).")]
    public float laserOffDuration = 1.5f; 

    [Header("Behavior & Damage")]
    public int damageAmount = 1; 
    [Tooltip("Tần suất gây sát thương (mỗi 0.5 giây) khi Player chạm vào Collider.")]
    public float damageInterval = 0.5f; // Tần suất gây sát thương khi Player chạm
    public LayerMask playerLayer; 
    
    [Header("Animator Triggers")]
    public string activateTriggerName = "Activate"; 
    public string deactivateTriggerName = "Deactivate"; 

    // ⭐ TRẠNG THÁI NỘI BỘ VÀ COMPONENTS
    private Animator anim;
    private Collider2D barrierCollider;
    private bool isLaserActive = true; 
    private float cycleTimer = 0f;
    private float lastDamageTime = 0f; // Dùng để giới hạn tần suất gây sát thương

    void Start()
    {
        anim = GetComponent<Animator>();
        barrierCollider = GetComponent<Collider2D>(); 

        if (anim == null || barrierCollider == null)
        {
            Debug.LogError("Cần Component Animator và Collider2D trên đối tượng Barrier (Collider phải là Is Trigger!).");
        }
        
        // ⭐ QUAN TRỌNG: Khởi tạo trạng thái và Collider
        isLaserActive = true;
        cycleTimer = 0f;
        
        // Collider BẬT khi Start (vì mặc định chu kỳ bắt đầu ở Idle)
        if (barrierCollider != null)
        {
            barrierCollider.enabled = true; 
        }
    }

    void Update()
    {
        if (anim == null) return;
        
        cycleTimer += Time.deltaTime;

        if (isLaserActive)
        {
            // ⭐ TRẠNG THÁI IDLE (LASER BẬT)
            if (cycleTimer >= laserOnDuration)
            {
                // Chuyển sang Deactivate
                isLaserActive = false;
                cycleTimer = 0f;
                anim.SetTrigger(deactivateTriggerName);
                
                // ⭐ TẮT COLLIDER
                if (barrierCollider != null) barrierCollider.enabled = false; 
            }
        }
        else
        {
            // ⭐ TRẠNG THÁI DEACTIVATE (LASER TẮT)
            if (cycleTimer >= laserOffDuration)
            {
                // Chuyển về Idle
                isLaserActive = true;
                cycleTimer = 0f;
                anim.SetTrigger(activateTriggerName);
                
                // ⭐ BẬT COLLIDER
                if (barrierCollider != null) barrierCollider.enabled = true; 
            }
        }
    }
    
    // ⭐ LOGIC GÂY SÁT THƯƠNG LIÊN TỤC (Yêu cầu Collider phải là Is Trigger = TRUE)
    private void OnTriggerStay2D(Collider2D other)
    {
        // 1. Kiểm tra Layer (Đảm bảo là Player)
        if (((1 << other.gameObject.layer) & playerLayer) == 0) return;
        
        // 2. Giới hạn Tần suất gây sát thương
        if (Time.time >= lastDamageTime + damageInterval)
        {
            // 3. Gây sát thương
            if (other.TryGetComponent<Platformer.PlayerController>(out Platformer.PlayerController playerScript))
            {
                playerScript.TakeDamage(damageAmount);
                lastDamageTime = Time.time;
                Debug.Log("💥 Laser Bẫy: Player bị sát thương liên tục do chạm vào Collider BẬT!");
            }
        }
    }
    
    // HÀM DEALDAMAGETOPLAYER (VÀ LOGIC OVERLAP) KHÔNG CẦN THIẾT KHI DÙNG ONTRIGGERSTAY2D.
}