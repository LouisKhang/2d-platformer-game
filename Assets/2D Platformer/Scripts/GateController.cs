using UnityEngine;
using System.Collections; 

public class GateController : MonoBehaviour
{
    // Lưu trạng thái hiện tại của cửa
    private bool _isOpen = false; 
    private Animator _animator;
    
    // Cài đặt trong Inspector: Tên tham số BOOL trong Animator
    public string gateAnimationBool = "IsOpen"; 
    public float animationDuration = 1.0f; 

    private Collider2D[] _gateColliders; 

    void Start()
    {
        _animator = GetComponent<Animator>();
        // Lấy tất cả Collider trên các GameObject con
        _gateColliders = GetComponentsInChildren<Collider2D>();
        
        // Đảm bảo cửa bắt đầu đóng
        SetCollidersEnabled(true);
    }

    // HÀM MỚI ĐƯỢC LEVERCONTROLLER GỌI ĐẾN
    public void ToggleGate()
    {
        _isOpen = !_isOpen; // Đảo ngược trạng thái
        
        Debug.Log("Gate toggled. IsOpen: " + _isOpen);

        // Kích hoạt/Tắt Animation: Đặt BOOL trong Animator
        if (_animator != null)
        {
            _animator.SetBool(gateAnimationBool, _isOpen); 
        }
        
        // Quản lý Collider: Tắt nếu mở, Bật nếu đóng
        if (_isOpen)
        {
            // Nếu mở: Tắt Collider sau khi animation mở kết thúc
            StartCoroutine(ManageColliderAfterTime(false, animationDuration));
        }
        else
        {
            // Nếu đóng: Bật Collider ngay lập tức (trước khi animation đóng kết thúc)
            SetCollidersEnabled(true);
        }
    }

    // Coroutine để chờ animation kết thúc rồi mới tắt/bật Collider
    private IEnumerator ManageColliderAfterTime(bool enable, float delay)
    {
        yield return new WaitForSeconds(delay);
        SetCollidersEnabled(enable);
    }
    
    // Hàm hỗ trợ bật/tắt Collider
    private void SetCollidersEnabled(bool enabledState)
    {
        foreach (Collider2D col in _gateColliders)
        {
            if (col != null)
            {
                col.enabled = enabledState;
            }
        }
    }
}