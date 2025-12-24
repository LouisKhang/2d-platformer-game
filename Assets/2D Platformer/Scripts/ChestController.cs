using UnityEngine;

// Đảm bảo namespace này khớp với PlayerController của bạn nếu cần
using Platformer; 

public class ChestController : MonoBehaviour
{
    [Header("Visual & Prefabs")]
    [Tooltip("Prefab rương Đóng ban đầu")]
    public GameObject closedChestPrefab;
    [Tooltip("Prefab rương Mở (thay thế sau khi mở)")]
    public GameObject openedChestPrefab;
    [Tooltip("Prefab Coin để thả ra")]
    public GameObject coinPrefab;
    
    [Header("Interaction & Rewards")]
    public int goldAmount = 20;
    public float goldSpreadRadius = 2f;
    public float interactionRange = 1.5f;
    public KeyCode interactionKey = KeyCode.Y;

    // Trạng thái Rương
    private DemonController demonController;
    private GameObject currentChestVisual;
    private Transform playerTransform;
    private bool isLocked = true; // Mặc định Rương bị khóa
    private bool isOpened = false;

    void Start()
    {
        // 1. Khởi tạo visual ban đầu là Rương Đóng
        if (closedChestPrefab != null)
        {
            // Instantiate prefab Rương Đóng là con của ChestController GameObject
            currentChestVisual = Instantiate(closedChestPrefab, transform.position, Quaternion.identity, transform);
        }
        
        // 2. Tìm Player
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;

        // 3. Tìm Boss và đăng ký event
        DemonController[] demons = FindObjectsOfType<DemonController>();
        if (demons.Length > 0)
        {
            demonController = demons[0];
            // Đăng ký hàm UnlockChest() vào sự kiện Boss chết
            demonController.OnBossDeath += UnlockChest;
            Debug.Log("ChestController: Found Boss and registered OnBossDeath event.");
        }
        else
        {
            // Trường hợp không có Boss (ví dụ: test), tự động mở khóa
            Debug.LogWarning("ChestController: DemonController not found. Chest is unlocked by default.");
            isLocked = false;
        }
    }

    void Update()
    {
        if (isOpened || playerTransform == null) return;

        // Kiểm tra phạm vi tương tác
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        
        if (distanceToPlayer <= interactionRange)
        {
            // Nếu rương đã được mở khóa VÀ Player nhấn phím tương tác
            if (!isLocked && Input.GetKeyDown(interactionKey))
            {
                OpenChest();
            }
            // Tùy chọn: Thêm logic hiển thị UI "Press Y to Open" tại đây.
        }
    }
    
    // Hàm này được gọi khi event OnBossDeath của Boss kích hoạt
    private void UnlockChest()
    {
        isLocked = false;
        Debug.Log("CHEST UNLOCKED: Boss is dead.");
        
        // Ngắt kết nối event sau khi đã mở khóa (để tránh lỗi nếu có nhiều scene)
        if (demonController != null)
        {
            demonController.OnBossDeath -= UnlockChest;
        }
    }

    private void OpenChest()
    {
        if (isOpened) return;
        
        isOpened = true;
        Debug.Log("CHEST OPENED! Dropping " + goldAmount + " coins.");

        // 1. Thay đổi visual rương từ Đóng sang Mở
        if (currentChestVisual != null) Destroy(currentChestVisual);
        if (openedChestPrefab != null)
        {
            currentChestVisual = Instantiate(openedChestPrefab, transform.position, Quaternion.identity, transform);
        }
        
        // 2. Thả Coin
        DropGold();
        
        // Tùy chọn: Vô hiệu hóa script này sau khi mở
        enabled = false; 
    }

    private void DropGold()
    {
        if (coinPrefab == null) return;

        for (int i = 0; i < goldAmount; i++)
        {
            // Tính vị trí spawn ngẫu nhiên xung quanh rương
            Vector3 spawnPos = transform.position + new Vector3(
                Random.Range(-goldSpreadRadius, goldSpreadRadius),
                Random.Range(0.5f, goldSpreadRadius), // Nhảy lên một chút để tạo hiệu ứng rơi
                0
            );
            
            GameObject coin = Instantiate(coinPrefab, spawnPos, Quaternion.identity);
            
            // Tùy chọn: Thêm lực đẩy/vận tốc ban đầu để Coin văng ra
            Rigidbody2D rb = coin.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                float forceX = Random.Range(-3f, 3f);
                float forceY = Random.Range(5f, 8f);
                rb.velocity = new Vector2(forceX, forceY);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        // Vẽ phạm vi tương tác trong Editor
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}