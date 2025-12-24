using UnityEngine;
using UnityEngine.UI; 
using System.Collections;
using Platformer; 

public class PlayerOrbitalShield : MonoBehaviour
{
    private PlayerController playerController;

    // =============================================================
    // CÁC THUỘC TÍNH
    // =============================================================
    [Header("Orbital Shield Skill")]
    public GameObject OrbitalPrefab; 
    public float OrbitalDuration = 7.0f; 
    public float OrbitalCooldown = 40.0f; 
    
    private bool isOrbitalActive = false; 
    private float nextOrbitalTime = 0f; 
    private GameObject currentOrbitalInstance; 

    // ⭐ KHAI BÁO MỚI CHO ÂM THANH ⭐
    [Header("Audio")]
    public AudioClip activateSound; // ⭐ Âm thanh kích hoạt skill ⭐
    private AudioSource audioSource; // Component phát âm thanh

    // =========================================================
    // UI COOLDOWN LOGIC
    // =========================================================
    [Header("UI Cooldown References")]
    public Image cooldownOverlayImage; 
    public Text cooldownText; 
    
    private bool isCoolingDown = false; 

    void Start()
    {
        playerController = GetComponent<PlayerController>();
        if (playerController == null)
        {
            Debug.LogError("🚨 PlayerController không tìm thấy trên cùng GameObject!");
        }

        // ⭐ LẤY HOẶC THÊM AUDIO SOURCE CHO PLAYER ⭐
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Thiết lập trạng thái UI ban đầu
        if (cooldownText != null) cooldownText.gameObject.SetActive(false);
        if (cooldownOverlayImage != null) cooldownOverlayImage.fillAmount = 0f;
    }

    void Update()
    {
        // 🛡️ CẬP NHẬT VỊ TRÍ ORBITAL
        if (isOrbitalActive && currentOrbitalInstance != null)
        {
            // Giữ Orbital bám theo Player
            currentOrbitalInstance.transform.position = this.transform.position;
        }

        // 🛡️ XỬ LÝ INPUT
        if (Input.GetKeyDown(KeyCode.I))
        {
            TryActivateOrbital();
        }
    }

    public void TryActivateOrbital()
    {
        if (isOrbitalActive)
        {
            Debug.Log("🛡️ Orbital Shield đang hoạt động!");
            return;
        }

        // Kiểm tra Cooldown
        if (Time.time >= nextOrbitalTime)
        {
            StartCoroutine(OrbitalRoutine());
            StartCoroutine(HandleCooldownUI()); // Bắt đầu UI Cooldown
        }
        else
        {
            float remainingCooldown = nextOrbitalTime - Time.time;
            Debug.Log($"⏳ Orbital đang hồi chiêu. Còn {remainingCooldown:F1}s.");
        }
    }

    // =============================================================
    // LOGIC KỸ NĂNG ORBITAL SHIELD (Coroutine)
    // =============================================================
    private IEnumerator OrbitalRoutine()
    {
        if (OrbitalPrefab == null || playerController == null)
        {
            Debug.LogError("🚨 Orbital Prefab hoặc PlayerController bị thiếu!");
            yield break;
        }
        
        isOrbitalActive = true;
        // ⭐ BẬT TRẠNG THÁI BẤT TỬ 7S ⭐
        playerController.SetInvulnerable(true); 

        // 1. Kích hoạt hiệu ứng Orbital và ÂM THANH
        currentOrbitalInstance = Instantiate(OrbitalPrefab, transform.position, Quaternion.identity, transform);
        
        // ⭐ PHÁT ÂM THANH KÍCH HOẠT ⭐
        if (audioSource != null && activateSound != null)
        {
            audioSource.PlayOneShot(activateSound);
        }
        
        Debug.Log("🛡️ Orbital Shield Kích hoạt! Bất tử trong 7s.");

        // 2. Thời gian duy trì (7s)
        yield return new WaitForSeconds(OrbitalDuration);

        // 3. Kết thúc hiệu ứng
        isOrbitalActive = false;
        // ⭐ TẮT TRẠNG THÁI BẤT TỬ ⭐
        playerController.SetInvulnerable(false); 

        // Hủy hiệu ứng hình ảnh
        if (currentOrbitalInstance != null)
        {
            Destroy(currentOrbitalInstance);
            currentOrbitalInstance = null;
        }

        // 4. Bắt đầu Cooldown
        nextOrbitalTime = Time.time + OrbitalCooldown;
        Debug.Log($"Orbital Shield kết thúc. Bắt đầu hồi chiêu {OrbitalCooldown}s.");
    }
    
    // =============================================================
    // LOGIC UI COOLDOWN (Coroutine)
    // =============================================================
    private IEnumerator HandleCooldownUI()
    {
        isCoolingDown = true;
        float startTime = Time.time;
        nextOrbitalTime = Time.time + OrbitalCooldown; 

        if (cooldownOverlayImage != null) cooldownOverlayImage.fillAmount = 1f;
        if (cooldownText != null) cooldownText.gameObject.SetActive(true);

        while (Time.time < nextOrbitalTime)
        {
            float timeRemaining = nextOrbitalTime - Time.time;

            if (cooldownText != null)
            {
                cooldownText.text = timeRemaining.ToString("F1"); 
            }
            if (cooldownOverlayImage != null)
            {
                cooldownOverlayImage.fillAmount = timeRemaining / OrbitalCooldown;
            }
            yield return null; 
        }

        isCoolingDown = false;
        if (cooldownText != null) cooldownText.gameObject.SetActive(false);
        if (cooldownOverlayImage != null) cooldownOverlayImage.fillAmount = 0f;
    }
    
    // ⭐ API CÔNG KHAI ĐỂ PLAYER CONTROLLER KIỂM TRA TRẠNG THÁI ⭐
    public bool IsOrbitalActive => isOrbitalActive;
}