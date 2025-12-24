using UnityEngine;
using System.Collections;
using Platformer;

public class SkullwolfDamageTrigger : MonoBehaviour
{
    [Header("Damage Settings")]
    [Tooltip("Sát thương gây ra mỗi lần chạm (1 = mất 1 tim)")]
    public int damageAmount = 1;
    
    [Tooltip("Thời gian giữa các lần gây sát thương cho cùng 1 Player (giây)")]
    public float damageInterval = 1.0f;
    
    [Tooltip("Có hiển thị debug log không?")]
    public bool showDebugLogs = true;

    // Theo dõi thời gian gây sát thương cho Player
    private float lastDamageTime = 0f;

    // =============================================================
    // TRIGGER COLLISION
    // =============================================================

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Kiểm tra xem có phải Player không
        if (other.CompareTag("Player"))
        {
            TryDamagePlayer(other);
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        // Gây sát thương liên tục khi Player đứng trong vùng trigger
        if (other.CompareTag("Player"))
        {
            TryDamagePlayer(other);
        }
    }

    // =============================================================
    // LOGIC GÂY SÁT THƯƠNG
    // =============================================================

    private void TryDamagePlayer(Collider2D playerCollider)
    {
        // Kiểm tra cooldown - chỉ gây sát thương sau mỗi damageInterval giây
        if (Time.time < lastDamageTime + damageInterval)
        {
            return; // Chưa đủ thời gian, không gây sát thương
        }

        // Lấy PlayerController
        PlayerController player = playerCollider.GetComponent<PlayerController>();
        
        if (player != null)
        {
            // Gây sát thương
            player.TakeDamage(damageAmount);
            
            // Cập nhật thời gian gây sát thương gần nhất
            lastDamageTime = Time.time;
            
            if (showDebugLogs)
            {
                Debug.Log($"🐺 Skullwolf gây {damageAmount} sát thương cho Player!");
            }
        }
    }

    // =============================================================
    // HIỂN THỊ GIZMOS TRONG SCENE VIEW
    // =============================================================

    private void OnDrawGizmos()
    {
        // Hiển thị vùng Trigger trong Scene View (màu đỏ)
        Collider2D col = GetComponent<Collider2D>();
        
        if (col != null)
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f); // Màu đỏ trong suốt
            
            if (col is BoxCollider2D boxCol)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(boxCol.offset, boxCol.size);
            }
            else if (col is CircleCollider2D circleCol)
            {
                Gizmos.DrawSphere(transform.position + (Vector3)circleCol.offset, circleCol.radius);
            }
        }
    }
}