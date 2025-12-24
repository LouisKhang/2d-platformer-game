using UnityEngine;

public class BossActivator : MonoBehaviour
{
    [Header("Boss Reference")]
    [Tooltip("Kéo GameObject Evil_Wizard vào đây từ Hierarchy.")]
    public EvilWizardBoss bossScript;
    
    [Header("Activation Settings")]
    [Tooltip("Có thể kích hoạt lại Boss nhiều lần không? (Nếu true, mỗi lần Player vào sẽ kích hoạt lại)")]
    public bool canReactivate = true;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Kích hoạt khi Player đi vào vùng Trigger
        if (other.CompareTag("Player") && bossScript != null)
        {
            // Gọi hàm ActivateBoss (hàm này đã có logic kiểm tra isDead và isActivated)
            bossScript.ActivateBoss();
            
            Debug.Log("🎯 Player vào vùng Boss Activator!");
        }
    }
}