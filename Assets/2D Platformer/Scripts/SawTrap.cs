using System.Collections;
using UnityEngine;
using Platformer; 

public class SawTrap : MonoBehaviour
{
    [Header("Cycle Timings")]
    public float idleDuration = 2f;
    public float freezeDelayTime = 2f;

    [Header("Animator Triggers")]
    public string activateTriggerName = "ActivateSaw";
    public string deactivateTriggerName = "StartDeactivate";

    [Header("Damage Settings")]
    public int damageAmount = 1;
    public float damageCooldown = 0.5f;
    public string playerTag = "Player";

    [HideInInspector]
    public bool IsDangerous = false; 

    [Header("Damage Collider")]
    public Collider2D damageCollider; 

    // ⭐ CÀI ĐẶT ÂM THANH
    [Header("Audio")]
    [Tooltip("Clip âm thanh quay liên tục của cưa.")]
    public AudioClip sawSpinSound; 
    
    [Range(0f, 1f)] 
    [Tooltip("Âm lượng tối đa của tiếng cưa.")]
    public float sawVolume = 1.0f; 
    
    // ⭐ THAM SỐ KÍCH HOẠT ÂM THANH THEO KHOẢNG CÁCH
    [Header("Audio Activation")]
    [Tooltip("Bán kính (khoảng cách) mà người chơi phải ở trong để nghe thấy tiếng cưa.")]
    public float audioActivationRadius = 5f;
    
    private AudioSource audioSource; 
    private Animator anim;
    private bool canDealDamage = true;
    private Transform player; // Tham chiếu đến vị trí người chơi

    void Start()
    {
        anim = GetComponent<Animator>();
        if (anim == null) Debug.LogError("Animator missing!");
        if (damageCollider == null) Debug.LogError("Damage Collider missing on SawTrap!");

        // KHỞI TẠO AUDIO SOURCE
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
        
        // TÌM NGƯỜI CHƠI
        GameObject playerObject = GameObject.FindWithTag(playerTag);
        if (playerObject != null)
        {
            player = playerObject.transform;
        }

        StopAllCoroutines();
        StartCoroutine(SawCycleLoop());
    }
    
    void Update()
    {
        // ⭐ LOGIC KÍCH HOẠT ÂM THANH THEO BÁN KÍNH
        if (player == null || audioSource == null || !audioSource.isPlaying) 
        {
            // Chỉ kiểm tra khi cưa đang quay VÀ người chơi tồn tại
            return; 
        }

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= audioActivationRadius)
        {
            // Nếu player trong vùng, đặt âm lượng tối đa (do người dùng định nghĩa)
            audioSource.volume = sawVolume;
        }
        else
        {
            // Nếu player ngoài vùng, tắt âm lượng hoàn toàn
            audioSource.volume = 0f;
        }
    }

    IEnumerator SawCycleLoop()
    {
        if (anim != null) anim.SetTrigger(activateTriggerName);

        while (true)
        {
            // 1. Idle/Dangerous → Bật Collider
            if (damageCollider != null) damageCollider.enabled = true;
            IsDangerous = true; 
            
            // BẮT ĐẦU PHÁT ÂM THANH QUAY (Update sẽ điều chỉnh âm lượng)
            if (audioSource != null && sawSpinSound != null)
            {
                audioSource.clip = sawSpinSound;
                audioSource.loop = true; 
                // KHÔNG đặt audioSource.volume ở đây, vì Update() sẽ liên tục kiểm tra và đặt nó.
                audioSource.Play();
            }
            
            yield return new WaitForSeconds(idleDuration);

            // 2. Deactivate → Tắt Collider
            if (anim != null) anim.SetTrigger(deactivateTriggerName);
            if (damageCollider != null) damageCollider.enabled = false;
            IsDangerous = false; 
            
            // DỪNG ÂM THANH QUAY (Cưa không quay nữa thì âm thanh phải dừng hẳn)
            if (audioSource != null)
            {
                audioSource.Stop(); 
            }

            yield return null; 
            
            float deactivateClipDuration = 1f; 
            if (anim != null)
            {
                AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
                if (stateInfo.IsName("saw_deactivate"))
                {
                    deactivateClipDuration = stateInfo.length;
                }
            }
            
            yield return new WaitForSeconds(deactivateClipDuration + freezeDelayTime);
            
            // 3. Activate
            if (anim != null) anim.SetTrigger(activateTriggerName);
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (!collision.CompareTag(playerTag)) return;
        
        if (damageCollider == null || !damageCollider.enabled) return; 

        if (collision.TryGetComponent<PlayerController>(out PlayerController playerController) && canDealDamage)
        {
            if (playerController != null && playerController.isActiveAndEnabled)
            {
                playerController.TakeDamage(damageAmount);
                canDealDamage = false;
                Invoke(nameof(ResetDamageCooldown), damageCooldown);
            }
        }
    }

    private void ResetDamageCooldown() => canDealDamage = true;
    
    // ⭐ VẼ GIZMO ĐỂ XEM VÙNG KÍCH HOẠT
    void OnDrawGizmosSelected()
    {
        // VẼ VÙNG KÍCH HOẠT ÂM THANH
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, audioActivationRadius);
    }
}