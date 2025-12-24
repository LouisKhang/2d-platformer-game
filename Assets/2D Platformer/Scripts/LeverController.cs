using UnityEngine;

public class LeverController : MonoBehaviour
{
    private Animator _animator;
    // Lưu trạng thái hiện tại của cần gạt: false = đóng (mặc định), true = mở
    private bool _isPulled = false; 

    // Biến mới: Theo dõi xem Player có đang ở gần cần gạt không
    private bool _playerIsNear = false; 

    // Cài đặt trong Inspector
    public string leverAnimationParam = "IsPulled"; 
    public GameObject targetObject; // Gate_Master
    // Tên hàm mới cần gọi trên GateController
    public string targetFunctionName = "ToggleGate"; 

    // Phím cần ấn để kích hoạt cần gạt
    public KeyCode activationKey = KeyCode.G; 

    void Start()
    {
        _animator = GetComponent<Animator>();
    }

    void Update()
    {
        // CHỈ kích hoạt logic khi Player đang ở gần (trong Collider Trigger) 
        // VÀ người chơi nhấn phím kích hoạt (mặc định là 'U')
        if (_playerIsNear && Input.GetKeyDown(activationKey))
        {
            ToggleLever();
        }
    }

    // Khi Player đi vào vùng kích hoạt (Collider Trigger 2D)
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            _playerIsNear = true;
            Debug.Log("Player đã vào vùng kích hoạt cần gạt. Nhấn " + activationKey.ToString() + " để gạt.");
            // **Tùy chọn:** Thêm logic hiển thị gợi ý (UI) tại đây
        }
    }

    // Khi Player đi ra khỏi vùng kích hoạt
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            _playerIsNear = false;
            Debug.Log("Player đã ra khỏi vùng kích hoạt cần gạt.");
            // **Tùy chọn:** Thêm logic ẩn gợi ý (UI) tại đây
        }
    }
    
    // Hàm chứa logic kích hoạt cần gạt (tách ra để dễ quản lý)
    private void ToggleLever()
    {
        // 1. Đảo ngược trạng thái
        _isPulled = !_isPulled; 
        
        Debug.Log("Cần gạt chuyển trạng thái bằng phím. IsPulled: " + _isPulled);

        // 2. Kích hoạt Animation cần gạt (chuyển đổi giữa Idle và Pulled)
        if (_animator != null)
        {
            _animator.SetBool(leverAnimationParam, _isPulled); 
        }
        
        // 3. Gọi hàm chuyển trạng thái trên Gate_Master
        if (targetObject != null)
        {
            targetObject.SendMessage(targetFunctionName, SendMessageOptions.DontRequireReceiver);
        }
    }
}