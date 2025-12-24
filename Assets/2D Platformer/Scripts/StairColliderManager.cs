using UnityEngine;

// Gắn script này vào GameObject thang_bo
public class StairColliderManager : MonoBehaviour
{
    // Kéo Component Collider 2D của slope_collider vào đây
    public Collider2D slopeCollider; 
    
    private SpriteRenderer[] stairRenderers;

    void Awake()
    {
        // Lấy tất cả Sprite Renderer của các bậc thang con
        stairRenderers = GetComponentsInChildren<SpriteRenderer>();
        
        // Trạng thái ban đầu: Vô hình và không có va chạm
        SetStairsVisible(false);
        if (slopeCollider != null)
        {
            slopeCollider.enabled = false;
        }
    }

    // Hàm gọi từ StairActivator.cs để BẬT/TẮT Sprite Renderer
    public void SetStairsVisible(bool visible)
    {
        foreach (SpriteRenderer sr in stairRenderers)
        {
            sr.enabled = visible;
        }
    }

    // HÀM NÀY PHẢI PUBLIC VÀ SẼ ĐƯỢC GỌI BỞI ANIMATION EVENT (Keyframe Cuối)
    public void FinalizeStairs()
    {
        // 1. BẬT Va chạm
        if (slopeCollider != null)
        {
            slopeCollider.enabled = true;
        }
        
        // 2. Tắt GraveActivator để ngăn kích hoạt lại
        GameObject graveActivator = GameObject.Find("GraveActivator");
        if (graveActivator != null)
        {
            graveActivator.SetActive(false);
        }
        
        Debug.Log("Stairs are now solid and visible.");
    }
}