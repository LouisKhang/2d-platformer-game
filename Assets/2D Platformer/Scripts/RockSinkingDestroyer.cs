using UnityEngine;

public class RockSinkingDestroyer : MonoBehaviour
{
    // Hàm này được gọi từ Animation Event ở cuối clip chìm
    public void DestroyRock()
    {
        // Hủy đối tượng khối đá sau khi animation chìm kết thúc
        Destroy(gameObject);
        Debug.Log("✅ Khối đá đã chìm và bị hủy.");
    }
}