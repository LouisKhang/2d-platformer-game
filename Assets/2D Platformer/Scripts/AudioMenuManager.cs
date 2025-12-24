using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// AudioMenuManager: Quản lý nhạc menu xuyên suốt scene.
/// Tách object riêng để tránh destroy nhầm.
/// Chức năng:
/// - Play/Stop BGM theo scene.
/// - Mute/Unmute + Volume control.
/// - Slider hover show/ẩn.
/// - Update icon.
/// - Singleton + DontDestroyOnLoad.
/// - Cleanup duplicate tự động.
/// </summary>
public class AudioMenuManager : MonoBehaviour
{
    public static AudioMenuManager Instance;

    [Header("BGM Setting")]
    public AudioSource menuBGM;

    [Header("UI Control")]
    public Image soundIconImage;
    public Sprite soundOnSprite;
    public Sprite soundOffSprite;
    public GameObject volumeSliderPanel;

    private Coroutine hideSliderCoroutine;
    private const float HideDelay = 0.5f;

    private float lastVolume = 0.5f;
    private const string GameSceneToStopMusic = "Demo 1";

    void Awake()
    {
        // ===============================
        // Cleanup duplicate ngay Awake
        // ===============================
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"⚠️ AudioMenuManager duplicate → Destroy {gameObject.name}, path: {GetFullPath(gameObject)}");
            Destroy(gameObject); // Chỉ destroy bản dư thừa
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // Giữ xuyên suốt các scene
        Debug.Log($"✅ AudioMenuManager Awake() → attached on: {gameObject.name}, path: {GetFullPath(gameObject)}");

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        if (menuBGM == null) Debug.LogError("❌ menuBGM chưa gán trong Inspector!");
        if (volumeSliderPanel != null) volumeSliderPanel.SetActive(false);
        else Debug.LogError("❌ volumeSliderPanel chưa gán!");

        UpdateSoundIcon(menuBGM != null && menuBGM.mute);

        if (menuBGM != null && menuBGM.volume > 0) lastVolume = menuBGM.volume;
    }

    #region Volume & Mute
    public void SetVolume(float volume)
    {
        if (menuBGM == null) return;

        float v = Mathf.Clamp01(volume);

        if (v <= 0.001f)
        {
            menuBGM.volume = 0f;
            menuBGM.mute = true;
            UpdateSoundIcon(true);
        }
        else
        {
            menuBGM.volume = v;
            lastVolume = v;
            menuBGM.mute = false;
            UpdateSoundIcon(false);
        }
    }

    public void ToggleMute()
    {
        if (menuBGM == null) return;

        if (menuBGM.mute)
        {
            menuBGM.mute = false;
            menuBGM.volume = lastVolume;
        }
        else
        {
            lastVolume = menuBGM.volume;
            menuBGM.volume = 0f;
            menuBGM.mute = true;
        }

        UpdateSoundIcon(menuBGM.mute);
    }

    private void UpdateSoundIcon(bool isMuted)
    {
        if (soundIconImage == null) return;
        soundIconImage.sprite = isMuted ? soundOffSprite : soundOnSprite;
    }
    #endregion

    #region Slider Hover
    public void OnPointerEnter()
    {
        if (hideSliderCoroutine != null) StopCoroutine(hideSliderCoroutine);
        if (volumeSliderPanel != null) volumeSliderPanel.SetActive(true);
    }

    public void OnPointerExit()
    {
        if (hideSliderCoroutine != null) StopCoroutine(hideSliderCoroutine);
        hideSliderCoroutine = StartCoroutine(HideSliderAfterDelay());
    }

    private IEnumerator HideSliderAfterDelay()
    {
        yield return new WaitForSeconds(HideDelay);
        if (volumeSliderPanel != null) volumeSliderPanel.SetActive(false);
        hideSliderCoroutine = null;
    }
    #endregion

    #region Scene Management
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == GameSceneToStopMusic)
            StopMenuMusic();
        else
            StartMenuMusic();
    }

    public void StartMenuMusic()
    {
        if (menuBGM == null) return;
        if (!menuBGM.isPlaying) menuBGM.Play();
    }

    public void StopMenuMusic()
    {
        if (menuBGM == null) return;
        if (menuBGM.isPlaying) menuBGM.Stop();
    }
    #endregion

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    #region Debug Helpers
    private string GetFullPath(GameObject obj)
    {
        string path = obj.name;
        Transform t = obj.transform.parent;
        while (t != null)
        {
            path = t.name + "/" + path;
            t = t.parent;
        }
        return path;
    }
    #endregion
}
