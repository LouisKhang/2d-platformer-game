using UnityEngine;
using Platformer; // Đảm bảo namespace này được khai báo để truy cập PlayerController

public class CeilingTrap : MonoBehaviour
{
    [Header("1. Damage Settings")]
    public int damageAmount = 1;
    private const string PlayerTag = "Player";

    // Cờ này được điều khiển bởi Animation Events (Animation)
    private bool isDamageActive = false; 
    
    // Cờ này đảm bảo bẫy chỉ gây sát thương 1 lần mỗi lần kẹp
    private bool hasDealtDamage = false; 

    // Lắng nghe va chạm khi Player nằm trong vùng Trigger của bẫy
    private void OnTriggerStay2D(Collider2D collision)
    {
        // 1. Kiểm tra Tag
        if (!collision.CompareTag(PlayerTag)) return;

        // 2. Chỉ gây sát thương khi:
        //    a) Bẫy đang ở giai đoạn nguy hiểm (do Animation Event kích hoạt)
        //    b) Chưa gây sát thương trong chu kỳ kẹp này
        if (isDamageActive && !hasDealtDamage)
        {
            // 3. Tìm PlayerController (script quản lý máu)
            Platformer.PlayerController playerController = collision.GetComponent<Platformer.PlayerController>();
            
            if (playerController != null)
            {
                // 4. Gây sát thương
                playerController.TakeDamage(damageAmount);
                
                // Đánh dấu đã gây sát thương
                hasDealtDamage = true; 

                Debug.Log("Ceiling Trap ĐÃ KẸP! Trừ " + damageAmount + " máu.");
            }
        }
    }

    // --- HÀM PUBLIC DÀNH CHO ANIMATION EVENTS ---
    
    // Hàm này phải được gọi khi bẫy bắt đầu kẹp xuống hoặc đạt tới điểm kẹp (Nguy hiểm)
    public void EnableDamage()
    {
        isDamageActive = true;
        hasDealtDamage = false; // Reset cờ cho chu kỳ kẹp mới
        Debug.Log("Bẫy: Sát thương KÍCH HOẠT.");
    }
    
    // Hàm này phải được gọi khi bẫy mở ra hoàn toàn và an toàn
    public void DisableDamage()
    {
        isDamageActive = false;
        // Không cần reset hasDealtDamage ở đây, việc đó sẽ xảy ra ở EnableDamage
        Debug.Log("Bẫy: Sát thương VÔ HIỆU HÓA.");
    }
}