using UnityEngine;

public class MovingWallTrap : MonoBehaviour
{
    // Cài đặt bạn sẽ chỉnh trong Inspector
    [Header("Movement Settings")]
    public float moveDistance = 8f;   // Tổng khoảng cách tường di chuyển (ví dụ: 8 đơn vị)
    public float cycleTime = 10f;     // Thời gian cho 1 chu kỳ (5s đi + 5s về = 10s)
    
    [Header("Wall Type")]
    // Nếu bật, tường sẽ di chuyển theo chiều âm (phải -> trái).
    // Nếu tắt, tường sẽ di chuyển theo chiều dương (trái -> phải).
    public bool isRightWall = false; 

    private Vector3 positionA; // Vị trí cố định (Vị trí đóng)
    private Vector3 positionB; // Vị trí xa nhất (Vị trí mở)

    void Start()
    {
        // Tính toán vị trí cố định (A) và vị trí xa nhất (B) dựa trên loại tường
        if (!isRightWall) // Tường Trái (Di chuyển Trái -> Phải)
        {
            // A: Vị trí xuất phát cố định (gần tường đối diện)
            positionA = transform.position; 
            // B: Vị trí xa nhất (bên phải)
            positionB = new Vector3(positionA.x + moveDistance, positionA.y, positionA.z);
        }
        else // Tường Phải (Di chuyển Phải -> Trái)
        {
            // A: Vị trí cố định (gần tường đối diện)
            positionA = transform.position; 
            // B: Vị trí xa nhất (bên trái)
            positionB = new Vector3(positionA.x - moveDistance, positionA.y, positionA.z);
        }
    }

    void Update()
    {
        // 1. Tính toán giá trị t (từ 0 -> cycleTime/2 -> 0)
        // Đây là bộ đếm thời gian lặp đi lặp lại.
        float t = Mathf.PingPong(Time.time, cycleTime / 2f);
        
        // 2. Chuyển t thành yếu tố nội suy (Lerp Factor) từ 0 đến 1, rồi về 0
        // (t / (cycleTime / 2f)) = 0 -> 1 -> 0
        float lerpFactor = t / (cycleTime / 2f);

        // 3. Áp dụng di chuyển
        // LerpFactor sẽ nội suy giữa vị trí A (cố định) và vị trí B (xa nhất).
        // Ví dụ: Tường Trái: A -> B -> A. Tường Phải: A -> B -> A.
        transform.position = Vector3.Lerp(positionA, positionB, lerpFactor);
    }
}