using UnityEngine;
using UnityEngine.SceneManagement;

namespace Platformer
{
    public class DoorController : MonoBehaviour
    {
        [Header("Next Level Settings")]
        [Tooltip("Nhập tên Scene tiếp theo (Ví dụ: Map2). Dùng để hiển thị nút 'Next' trong UI Win.")]
        public string nextSceneName = "Map2"; 
        
        private GameManager gameManager;

        void Start()
        {
            // Tìm và lấy GameManager
            gameManager = FindObjectOfType<GameManager>();
            
            if (gameManager == null)
            {
                Debug.LogError("DoorController: LỖI THIẾT LẬP! Không tìm thấy GameManager. Đảm bảo GameManager tồn tại trong Scene.");
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // Kiểm tra xem đối tượng va chạm có phải là Player không
            if (other.CompareTag("Player"))
            {
                Debug.Log("Player chạm cửa. Kích hoạt Game Win UI. Scene tiếp theo được lưu lại, KHÔNG TẢI SCENE NGAY.");

                if (gameManager != null)
                {
                    // GỌI HÀM KÍCH HOẠT WIN UI: Chuyển giao nhiệm vụ xử lý chiến thắng cho GameManager
                    gameManager.HandleLevelWin(nextSceneName);
                }
                else
                {
                    // Dự phòng: Nếu GameManager lỗi, tải Scene tiếp theo
                    SceneManager.LoadScene(nextSceneName);
                }
            }
        }
    }
}