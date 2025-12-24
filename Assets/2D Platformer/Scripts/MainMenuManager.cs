using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    public void StartGame()
    {
        Debug.Log(" StartGame() → Load LevelSelection");
        SceneManager.LoadScene("LevelSelection");
    }

    public void QuitGame()
    {
        Debug.Log(" Thoát Game");
        Application.Quit();
    }

    public void OnVolumeSliderChange(float newVolume)
    {
        Debug.Log(" Slider Volume = " + newVolume);
        AudioMenuManager.Instance?.SetVolume(newVolume);
    }

    public void OnMuteButtonClick()
    {
        Debug.Log(" Click Mute Button");
        AudioMenuManager.Instance?.ToggleMute();
    }
}
