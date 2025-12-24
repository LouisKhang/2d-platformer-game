using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject pauseMenuUI;
    
    [Header("System References")]
    [Tooltip("Kéo thả GameObject có chứa script GameManager.")]
    public Platformer.GameManager gameManager; 
    
    private bool isPaused = false;

    void Start()
    {
        if (pauseMenuUI == null)
            Debug.LogError("❌ pauseMenuUI CHƯA GÁN TRONG INSPECTOR!");
        if (gameManager == null)
            Debug.LogError("❌ gameManager CHƯA GÁN TRONG INSPECTOR!");

        pauseMenuUI?.SetActive(false);
        Time.timeScale = 1f;

        Debug.Log("▶️ UIManager Ready");
    }
    
    void Update()
    {
        // ⭐ SỬA LỖI TRUY CẬP: Đã đổi isGamePaused thành IsGamePaused
        if (Input.GetKeyDown(KeyCode.Escape) && gameManager != null && gameManager.IsGamePaused == false)
        {
            OpenPauseMenu();
        } 
        else if (Input.GetKeyDown(KeyCode.Escape) && isPaused)
        {
            ClosePauseMenu();
        }
    }

    public void OpenPauseMenu()
    {
        if (pauseMenuUI == null || gameManager == null) return;

        Debug.Log("⏸️ Mở Pause Menu");
        isPaused = true;
        
        // Gọi PauseGame() để dừng thời gian (Time.timeScale=0) và nhạc nền
        gameManager.PauseGame(); 
        
        pauseMenuUI.SetActive(true);
    }

    public void ClosePauseMenu()
    {
        if (pauseMenuUI == null || gameManager == null) return;

        Debug.Log("▶️ Đóng Pause Menu");
        isPaused = false;
        
        // Gọi UnpauseGame() để tiếp tục thời gian (Time.timeScale=1) và nhạc nền
        gameManager.UnpauseGame(); 
        
        pauseMenuUI.SetActive(false);
    }

    public void RestartLevel()
    {
        Debug.Log("🔄 Restart Scene");
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void BackToMainMenu()
    {
        Debug.Log("↩️ Quay về Menu");

        Time.timeScale = 1f;

        // AudioMenuManager.Instance?.StartMenuMusic();
        
        SceneManager.LoadScene("Menu");
    }
}