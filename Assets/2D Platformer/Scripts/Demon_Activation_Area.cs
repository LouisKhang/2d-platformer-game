using UnityEngine;
using System.Collections;
using Cinemachine;

public class Demon_Activation_Area : MonoBehaviour 
{
    [Header("Boss")]
    public DemonController demonBoss;

    [Header("Activate Effect Prefab")]
    public GameObject activatePrefab;
    public Transform activateSpawnPoint; // thường là chân boss

    [Header("Camera")]
    public CinemachineVirtualCamera playerCam;
    public CinemachineVirtualCamera bossCam;

    [Header("Timing")]
    public float focusTime = 1.2f;
    public float shakeTime = 0.4f;
    public float activateEffectTime = 1f;

    [Header("Shake")]
    public float shakeStrength = 2f;

    bool triggered = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;
        triggered = true;
        StartCoroutine(ActivateSequence());
    }

    IEnumerator ActivateSequence()
    {
        // 1️⃣ Set vùng di chuyển boss
        BoxCollider2D area = GetComponent<BoxCollider2D>();
        if (area != null)
        {
            Bounds b = area.bounds;
            demonBoss.SetMovementBoundaries(b.min.x, b.max.x);
        }

        // 2️⃣ Camera focus boss
        bossCam.Priority = 30;
        playerCam.Priority = 0;

        // 3️⃣ Shake
        var noise = bossCam.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
        if (noise != null)
        {
            noise.m_AmplitudeGain = shakeStrength;
            yield return new WaitForSeconds(shakeTime);
            noise.m_AmplitudeGain = 0;
        }

        // 4️⃣ Spawn Activate prefab
        if (activatePrefab != null)
        {
            Vector3 pos = activateSpawnPoint != null 
                ? activateSpawnPoint.position 
                : demonBoss.transform.position;

            Instantiate(activatePrefab, pos, Quaternion.identity);
        }

        // 5️⃣ ĐỢI prefab xong → hiện boss
        yield return new WaitForSeconds(activateEffectTime);

        demonBoss.ShowBossInstant();

        // 6️⃣ Giữ camera thêm chút
        yield return new WaitForSeconds(focusTime);

        // 7️⃣ Bật AI boss
        demonBoss.ActivateBoss();

        // 8️⃣ Trả camera
        bossCam.Priority = 0;
        playerCam.Priority = 30;

        gameObject.SetActive(false);
    }
}
