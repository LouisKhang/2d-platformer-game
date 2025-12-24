using UnityEngine;

// Gắn script này vào GameObject GraveActivator
public class StairActivator : MonoBehaviour
{
    public Animator stairAnimator; 
    public GameObject stairRoot; // GameObject thang_bo
    
    private const string RevealTrigger = "RevealStairs"; 
    private bool playerInZone = false;
    private bool isActivated = false;
    private StairColliderManager stairManager;

    void Start()
    {
        // Khởi tạo và kiểm tra lỗi kết nối
        if (stairRoot != null)
        {
            stairManager = stairRoot.GetComponent<StairColliderManager>();
        }
        if (stairManager == null)
        {
            Debug.LogError("FATAL ERROR: StairColliderManager not found on thang_bo. Check Inspector setup.");
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) 
        {
            playerInZone = true;
            Debug.Log("Player entered Zone. Press G to reveal stairs.");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInZone = false;
        }
    }

    void Update()
    {
        if (playerInZone && Input.GetKeyDown(KeyCode.Y) && !isActivated)
        {
            if (stairAnimator != null && stairManager != null)
            {
                // BẬT Sprite Renderer trước khi gọi Animation
                stairManager.SetStairsVisible(true); 
                
                stairAnimator.SetTrigger(RevealTrigger);
                isActivated = true;
                enabled = false; // Ngăn kích hoạt lại
                Debug.Log("Stairs Activation Successful.");
            }
        }
    }
} 