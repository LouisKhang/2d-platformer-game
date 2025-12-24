using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelSelectManager : MonoBehaviour
{
    public void LoadLevel(string sceneName)
    {
        Debug.Log("🕹️ Load Scene: " + sceneName);
        
        
        if (sceneName == "Demo 1") 
        {
            
            AudioMenuManager.Instance?.StopMenuMusic();
        }
        
        SceneManager.LoadScene(sceneName);
    }

    public void BackToMenu()
    {
        Debug.Log(" Back to Menu");
        
        
        AudioMenuManager.Instance?.StartMenuMusic();
        
        SceneManager.LoadScene("Menu");
    }
}