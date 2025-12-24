using UnityEngine;

public class Portal : MonoBehaviour
{
    [Header("Portal Settings")]
    public Portal targetPortal;       // Cổng bên kia
    public KeyCode teleportKey = KeyCode.Y;

    private bool playerInRange = false;
    private Transform player;
    private bool canTeleport = true;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerInRange = true;
            player = collision.transform;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerInRange = false;
        }
    }

    private void Update()
    {
        if (!playerInRange || player == null) return;

        // Bấm Y để dịch chuyển
        if (Input.GetKeyDown(teleportKey) && canTeleport && targetPortal != null)
        {
            TeleportPlayer();
        }
    }

    private void TeleportPlayer()
    {
        // Move player to target portal
        player.position = targetPortal.transform.position;

        // Chặn teleport loop
        StartCoroutine(targetPortal.TeleportCooldown());

        // Chặn portal này 0.3s để tránh spam
        StartCoroutine(TeleportCooldown());
    }

    public System.Collections.IEnumerator TeleportCooldown()
    {
        canTeleport = false;
        yield return new WaitForSeconds(0.3f);
        canTeleport = true;
    }
}
