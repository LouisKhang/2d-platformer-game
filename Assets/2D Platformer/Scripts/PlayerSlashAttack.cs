using UnityEngine;
using UnityEngine.UI; 
using System.Collections; 

public class PlayerSlashAttack : MonoBehaviour
{
    [Header("Slash Settings")]
    public GameObject slashPrefab; // Prefab hiệu ứng chém (hitbox + hình ảnh)
    public Transform firePoint;     // Vị trí để spawn (thường là giữa nhân vật)
    public float slashOffsetX = 0.6f; // Giá trị đẩy hitbox ra phía trước nhân vật
    public float slashLifetime = 0.3f; // Thời gian tồn tại của hitbox/hiệu ứng

    [Header("Cooldown Logic")]
    public float slashCooldown = 1f; // Thời gian hồi chiêu
    private float lastSlashTime;
    private bool isCoolingDown = false; 

    [Header("UI Cooldown References")]
    public Image cooldownOverlayImage; // Hình ảnh Fill để hiển thị vòng tròn hồi chiêu
    public Text cooldownText;         // Text hiển thị thời gian còn lại

    // ⭐ KHAI BÁO MỚI CHO ÂM THANH ⭐
    [Header("Audio")]
    public AudioClip slashSound; 
    private AudioSource audioSource; 

    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
        
        // ⭐ KHỞI TẠO AUDIO SOURCE ⭐
        audioSource = GetComponent<AudioSource>(); 
        if (audioSource == null)
        {
            // Nếu chưa có, tự động thêm AudioSource vào gameObject này
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Thiết lập trạng thái UI ban đầu
        if (cooldownText != null) cooldownText.gameObject.SetActive(false);
        if (cooldownOverlayImage != null) cooldownOverlayImage.fillAmount = 0f;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.K))
        {
            TrySlash();
        }
    }

    public void TrySlash()
    {
        // Kiểm tra xem chiêu đã hồi xong chưa
        if (isCoolingDown) 
        {
            Debug.Log("⏳ Chưa hồi xong chém!");
            return;
        }

        // Nếu đã hồi xong, thực hiện chém
        Slash();
        lastSlashTime = Time.time;
        // Bắt đầu Coroutine quản lý hồi chiêu và UI
        StartCoroutine(HandleCooldownUI());
    }

    void Slash()
    {
        // Kích hoạt animation Attack
        if (animator != null)
            animator.SetTrigger("Attack");

        float playerFlipDirection = transform.localScale.x;
        
        // Logic để đảm bảo Hitbox và hình ảnh Slash Effect hiển thị đúng hướng
        
        // 1. HƯỚNG SPAWN (Đẩy animation/hitbox ra ngoài)
        // Dùng hướng NGƯỢC lại scale.x của Player để đẩy Hitbox ra phía trước (logic cũ)
        float spawnDirection = -Mathf.Sign(playerFlipDirection); 
        
        // 2. HƯỚNG LẬT HÌNH ẢNH (Giữ hình ảnh đúng chiều)
        // Dùng hướng CÙNG với scale.x của Player để hình ảnh hiệu ứng chém đúng chiều
        float flipDirection = Mathf.Sign(playerFlipDirection); 

        // Tính toán vị trí spawn
        Vector3 spawnPos = firePoint.position + new Vector3(slashOffsetX * spawnDirection, 0f, 0f);
        
        // Tạo hiệu ứng chém
        GameObject slash = Instantiate(slashPrefab, spawnPos, Quaternion.identity);
        
        // Lật hình ảnh Slash Effect (sử dụng flipDirection)
        slash.transform.localScale = new Vector3(
            Mathf.Abs(slash.transform.localScale.x) * flipDirection,
            slash.transform.localScale.y,
            slash.transform.localScale.z
        );

        // Hủy hiệu ứng sau thời gian tồn tại
        Destroy(slash, slashLifetime);
        Debug.Log("⚔️ Player chém!");

        // ⭐ PHÁT ÂM THANH CHÉM ⭐
        if (audioSource != null && slashSound != null)
        {
            // PlayOneShot cho phép phát âm thanh mà không bị gián đoạn nếu Player chém liên tục 
            audioSource.PlayOneShot(slashSound); 
        }
    }

    private System.Collections.IEnumerator HandleCooldownUI()
    {
        isCoolingDown = true;
        float timer = slashCooldown;
        
        // Thiết lập UI bắt đầu hồi chiêu
        if (cooldownOverlayImage != null) cooldownOverlayImage.fillAmount = 1f;
        if (cooldownText != null) cooldownText.gameObject.SetActive(true);

        while (timer > 0)
        {
            timer -= Time.deltaTime;
            
            // Cập nhật text và fill amount
            if (cooldownText != null)
            {
                // Format "F1" để hiển thị 1 chữ số thập phân (ví dụ: 0.9, 0.5)
                cooldownText.text = timer.ToString("F1"); 
            }
            if (cooldownOverlayImage != null)
            {
                cooldownOverlayImage.fillAmount = timer / slashCooldown;
            }
            yield return null; // Chờ 1 frame tiếp theo
        }

        // Kết thúc hồi chiêu
        isCoolingDown = false;
        if (cooldownText != null) cooldownText.gameObject.SetActive(false);
        if (cooldownOverlayImage != null) cooldownOverlayImage.fillAmount = 0f;
    }
}