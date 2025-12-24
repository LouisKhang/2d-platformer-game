using UnityEngine;

public class MobileButtonsToggle : MonoBehaviour
{
    [Header("DEBUG (Editor only)")]
    public bool forceShowInEditor = false;

    void Awake()
    {
    #if UNITY_EDITOR
        bool show = forceShowInEditor;
    #else
        bool show = Application.platform == RuntimePlatform.Android;
    #endif

        gameObject.SetActive(show);
    }
}