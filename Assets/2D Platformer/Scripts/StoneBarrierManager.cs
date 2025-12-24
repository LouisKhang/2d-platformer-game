using UnityEngine;

// Gắn script này vào GameObject Stone_Barrier_Root
public class StoneBarrierManager : MonoBehaviour
{
    [Tooltip("Collider 2D của Sprite stone-4")]
    public Collider2D stoneCollider; 
    
    [Tooltip("Sprite Renderer của Sprite stone-4")]
    public SpriteRenderer stoneRenderer; 
    
    [Tooltip("GameObject của vật che chắn StairCover")]
    public GameObject stairCoverObject;

    private Animator anim;

    void Awake()
    {
        anim = GetComponent<Animator>();

        // Collider TẮT khi game bắt đầu (đá vô hình và không chặn lối đi)
        if (stoneCollider != null)
        {
            stoneCollider.enabled = false;
        }
        // Lưu ý: Sprite Renderer đã TẮT trong Editor
    }

    // HÀM GỌI TỪ EVILWIZARDBOSS.CS KHI BOSS KÍCH HOẠT
    public void StartRise()
    {
        // 1. TẮT VẬT CHE CHẮN (StairCover)
        if (stairCoverObject != null)
        {
             stairCoverObject.SetActive(false); 
        }

        // 2. BẬT SPRITE RENDERER (HIỆN HÌNH)
        if (stoneRenderer != null)
        {
            stoneRenderer.enabled = true;
        }
        
        // 3. KÍCH HOẠT ANIMATION TRỒI LÊN
        if (anim != null)
        {
            anim.SetTrigger("Rise"); // Tên trigger phải là 'Rise'
        }
    }

    // HÀM NÀY ĐƯỢC GỌI BỞI ANIMATION EVENT (Keyframe Cuối)
    public void FinalizeBarrier()
    {
        // BẬT Va chạm khi animation kết thúc
        if (stoneCollider != null)
        {
            stoneCollider.enabled = true;
        }
        
        Debug.Log("Stone-4 Barrier is now solid and exposed.");
    }
}