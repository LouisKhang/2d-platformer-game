using UnityEngine;
using System.Collections;

public class MovingUpDownPlatform : MonoBehaviour
{
    [Header("Movement Settings")]
    public float speed = 1.5f;
    public float distance = 4f; // Khoảng cách tổng cộng di chuyển (Lên/Xuống)
    public float waitTime = 1f; // Thời gian chờ ở mỗi điểm

    private Vector3 startPosition;
    private Vector3 endPosition;
    private bool movingToEnd = true; // Cờ kiểm tra hướng di chuyển

    void Start()
    {
        // 1. Lưu vị trí khởi đầu
        startPosition = transform.position;
        
        // 2. Tính toán vị trí đích (di chuyển theo trục Y)
        endPosition = startPosition + new Vector3(0, distance, 0);

        // Bắt đầu di chuyển ngay lập tức
        StartCoroutine(MovePlatform());
    }

    IEnumerator MovePlatform()
    {
        while (true) // Vòng lặp vô hạn
        {
            // Xác định điểm hiện tại và điểm đích
            Vector3 target = movingToEnd ? endPosition : startPosition;
            
            // Di chuyển đến điểm đích
            while (Vector3.Distance(transform.position, target) > 0.01f)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position, 
                    target, 
                    speed * Time.deltaTime
                );
                yield return null; // Chờ 1 frame
            }
            
            // Đã đến điểm đích, chờ một chút
            yield return new WaitForSeconds(waitTime);
            
            // Đảo ngược hướng di chuyển
            movingToEnd = !movingToEnd;
        }
    }

    // Đảm bảo Player di chuyển cùng với Platform
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // Thiết lập Platform là vật thể cha của Player
            collision.gameObject.transform.SetParent(transform);
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // Bỏ liên kết (tránh lỗi khi Player nhảy khỏi Platform)
            collision.gameObject.transform.SetParent(null);
        }
    }
}