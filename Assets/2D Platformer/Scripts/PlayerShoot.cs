using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Platformer; // ⭐ ĐÃ THÊM: Để truy cập class Bullet ⭐

public class PlayerShoot : MonoBehaviour
{
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float bulletSpeed = 10f;

    [Header("Cooldown Logic")]
    public float shootCooldown = 2f; 
    private float lastShootTime; 
    private bool isCoolingDown = false;

    [Header("UI Cooldown References")]
    public Image cooldownOverlayImage;
    public Text cooldownText;

    // ⭐ TRƯỜNG KÉO THẢ ÂM THANH TRÊN PLAYER ⭐
    [Header("Audio")]
    public AudioClip shootSound;        // Âm thanh khi Player BẮN
    public AudioClip bulletHitSound;    // Âm thanh khi đạn TRÚNG
    private AudioSource audioSource;    

    void Start()
    {
        audioSource = GetComponent<AudioSource>(); 
        if (audioSource == null)
        {
            // Tùy chọn: Tự động thêm AudioSource nếu chưa có
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        if (cooldownText != null) cooldownText.gameObject.SetActive(false);
        if (cooldownOverlayImage != null) cooldownOverlayImage.fillAmount = 0f;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.J))
        {
            TryShoot();
        }
    }

    public void TryShoot()
    {
        if (isCoolingDown) 
        {
            Debug.Log("⏳ Chưa hồi xong bắn!");
            return;
        }

        Shoot();
        lastShootTime = Time.time;
        StartCoroutine(HandleCooldownUI());
    }

    void Shoot()
    {
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
        
        // 1. PHÁT ÂM THANH BẮN
        if (audioSource != null && shootSound != null)
        {
            audioSource.PlayOneShot(shootSound); 
        }
        
        // 2. TRUYỀN CLIP VA CHẠM (bulletHitSound) CHO VIÊN ĐẠN
        Bullet bulletScript = bullet.GetComponent<Bullet>();
        if (bulletScript != null)
        {
            // Gọi hàm Setter mới trong Bullet.cs
            bulletScript.SetHitSound(bulletHitSound); 
        }

        float playerFlipDirection = transform.localScale.x;
        float fireDirection = -Mathf.Sign(playerFlipDirection);

        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = new Vector2(bulletSpeed * fireDirection, 0f);
        }

        // Đảm bảo đạn được flip theo hướng bắn
        bullet.transform.localScale = new Vector3(
            Mathf.Abs(bullet.transform.localScale.x) * fireDirection,
            bullet.transform.localScale.y,
            bullet.transform.localScale.z
        );

        Debug.Log("🏹 Player bắn cung!");
    }

    private IEnumerator HandleCooldownUI()
    {
        isCoolingDown = true;
        float timer = shootCooldown;
        
        if (cooldownOverlayImage != null) cooldownOverlayImage.fillAmount = 1f;
        if (cooldownText != null) cooldownText.gameObject.SetActive(true);

        while (timer > 0)
        {
            timer -= Time.deltaTime;
            
            if (cooldownText != null)
            {
                cooldownText.text = timer.ToString("F1");
            }
            if (cooldownOverlayImage != null)
            {
                cooldownOverlayImage.fillAmount = timer / shootCooldown;
            }

            yield return null;
        }

        isCoolingDown = false;
        
        if (cooldownText != null) cooldownText.gameObject.SetActive(false);
        if (cooldownOverlayImage != null) cooldownOverlayImage.fillAmount = 0f;
    }
}