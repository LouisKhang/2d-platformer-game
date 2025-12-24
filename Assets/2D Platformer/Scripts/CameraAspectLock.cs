using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraAspectLock : MonoBehaviour
{
    [Header("Design Settings")]
    public float referenceAspect = 16f / 9f; // Tỉ lệ game thiết kế
    public float referenceOrthoSize = 5f;     // Size bạn thấy ĐÚNG khi thiết kế

    private Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
        cam.orthographic = true;
        UpdateCameraSize();
    }

    void UpdateCameraSize()
    {
        float currentAspect = (float)Screen.width / Screen.height;

        if (currentAspect >= referenceAspect)
        {
            // Màn hình rộng hơn → giữ chiều cao
            cam.orthographicSize = referenceOrthoSize;
        }
        else
        {
            // Màn hình hẹp hơn → tăng size để không crop
            float scale = referenceAspect / currentAspect;
            cam.orthographicSize = referenceOrthoSize * scale;
        }
    }

#if UNITY_EDITOR
    void Update()
    {
        // Để test resize Game View trong Editor
        UpdateCameraSize();
    }
#endif
}
