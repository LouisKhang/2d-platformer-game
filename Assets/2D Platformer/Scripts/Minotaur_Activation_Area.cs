using UnityEngine;

public class Minotaur_Activation_Area : MonoBehaviour 
{
    // Cần được gán trong Inspector
    public MinotaurController minotaurBoss; 

    // Đã sử dụng Collider2D
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && minotaurBoss != null)
        {
            // 1. Lấy ranh giới (Bounds) của Box Collider 2D
            BoxCollider2D areaCollider = GetComponent<BoxCollider2D>();
            if (areaCollider != null)
            {
                Bounds bounds = areaCollider.bounds;
                
                float minX = bounds.min.x;
                float maxX = bounds.max.x;
                
                // 2. Gửi ranh giới cho Boss Controller
                minotaurBoss.SetMovementBoundaries(minX, maxX);
            }
            
            // 3. Kích hoạt Boss
            minotaurBoss.ActivateBoss(); 
            
            // 4. Vô hiệu hóa vùng kích hoạt
            gameObject.SetActive(false); 
        }
    }
}