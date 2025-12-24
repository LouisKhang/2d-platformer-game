using System.Collections;
using UnityEngine;
using Platformer;

public class FireTrap : MonoBehaviour
{
    [Header("Fire Trap Settings")]
    [Tooltip("Sát thương gây ra mỗi lần (mỗi lần = 1 tim)")]
    public int damageAmount = 1;
    
    [Tooltip("Thời gian giữa mỗi lần gây sát thương (giây)")]
    public float damageInterval = 1.0f;
    
    [Tooltip("Có hiển thị debug log không?")]
    public bool showDebugLogs = true;

    private PlayerController playerInTrap = null;
    private Coroutine damageCoroutine = null;

    // =============================================================
    // TRIGGER EVENTS
    // =============================================================

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Kiểm tra xem có phải Player không
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            
            if (player != null)
            {
                playerInTrap = player;
                
                // Bắt đầu gây sát thương liên tục
                if (damageCoroutine == null)
                {
                    damageCoroutine = StartCoroutine(DamageOverTime());
                }
                
                if (showDebugLogs)
                {
                    Debug.Log("🔥 Player bước vào bẫy lửa! Bắt đầu gây sát thương.");
                }
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // Kiểm tra xem Player có rời khỏi bẫy không
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            
            if (player != null && player == playerInTrap)
            {
                // Dừng gây sát thương
                if (damageCoroutine != null)
                {
                    StopCoroutine(damageCoroutine);
                    damageCoroutine = null;
                }
                
                playerInTrap = null;
                
                if (showDebugLogs)
                {
                    Debug.Log("🔥 Player rời khỏi bẫy lửa. Ngừng gây sát thương.");
                }
            }
        }
    }

    // =============================================================
    // COROUTINE GÂY SÁT THƯƠNG LIÊN TỤC
    // =============================================================

    private IEnumerator DamageOverTime()
    {
        while (playerInTrap != null)
        {
            // Gây sát thương cho Player
            playerInTrap.TakeDamage(damageAmount);
            
            if (showDebugLogs)
            {
                // Kiểm tra xem Shield có đang bật không
                if (playerInTrap.IsShieldActive())
                {
                    Debug.Log("🔥💥 Bẫy lửa tấn công nhưng Shield đã chặn!");
                }
                else
                {
                    Debug.Log("🔥💥 Bẫy lửa gây sát thương! Player mất tim.");
                }
            }
            
            // Chờ trước khi gây sát thương tiếp theo
            yield return new WaitForSeconds(damageInterval);
        }
    }

    // =============================================================
    // CLEANUP
    // =============================================================

    private void OnDisable()
    {
        // Dọn dẹp khi object bị disable
        if (damageCoroutine != null)
        {
            StopCoroutine(damageCoroutine);
            damageCoroutine = null;
        }
        playerInTrap = null;
    }
}