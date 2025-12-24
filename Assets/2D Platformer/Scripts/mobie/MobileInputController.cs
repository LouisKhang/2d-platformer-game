using UnityEngine;
using Platformer;

public class MobileInputController : MonoBehaviour
{
    public PlayerController player;

#if UNITY_EDITOR
    [Header("DEBUG")]
    // 
    public bool enableInEditor = true;
#endif

    private float horizontal;
    private float vertical;

    private bool jumpPressed;
    private bool dashPressed;

    void Awake()
    {
#if UNITY_EDITOR
        // Cho phép chạy trong Editor nếu enableInEditor = true
        if (!enableInEditor)
        {
            enabled = false;
            return;
        }
#else
        // Chỉ chạy trên Android (Build thực tế)
        if (Application.platform != RuntimePlatform.Android)
        {
            enabled = false;
            // Đảm bảo tắt cờ Mobile Input trong Player khi script này tắt
            if (player != null)
                player.SetMobileInputInactive(); 
            return;
        }
#endif
    }

    void Update()
    {
        if (player == null || player.deathState) return;

        player.SetMobileMove(horizontal, vertical);

        if (jumpPressed)
        {
            player.SetMobileJump();
            jumpPressed = false;
        }

        if (dashPressed)
        {
            player.SetMobileDash();
            dashPressed = false;
        }
    }

    // =========================================================
    // HÀM GỌI TỪ NÚT UI (CÁC HÀM BẠN SẼ KÉO THẢ)
    // =========================================================

    // ===== DI CHUYỂN NGANG (CẦN POINTER DOWN & POINTER UP) =====
    public void MoveLeftDown()  { horizontal = -1f; }
    public void MoveRightDown() { horizontal = 1f; }
    public void MoveStop()      { horizontal = 0f; }

    // ===== DI CHUYỂN DỌC (CẦN POINTER DOWN & POINTER UP) =====
    public void UpDown()        { vertical = 1f; }
    public void DownDown()      { vertical = -1f; }
    public void VerticalStop()  { vertical = 0f; }

    // ===== HÀNH ĐỘNG (CHỈ CẦN ON CLICK) =====
    public void Jump() { jumpPressed = true; }
    public void Dash() { dashPressed = true; }
}