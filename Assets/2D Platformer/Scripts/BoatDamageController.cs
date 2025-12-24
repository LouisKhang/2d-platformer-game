using System.Collections;
using UnityEngine;
using Platformer;

public class BoatDamageController : MonoBehaviour
{
    [Header("Boat Settings")]
    public int maxHealth = 3;
    public int reviveHealth = 15;               // máu khi hồi sinh trên thuyền
    public GameObject explosionPrefab;
    public GameObject hurtPrefab;
    public GameObject playerDieEffectPrefab;
    public float invulnerabilityDuration = 1f;
    public float resetDelay = 2f;

    [Header("Audio Settings")]
    public AudioClip hurtSound;                 // âm thanh khi thuyền bị hurt
    public AudioClip explosionSound;            // âm thanh khi thuyền nổ
    private AudioSource audioSource;

    [Header("Damage Collider")]
    public Collider2D damageCollider;

    [Header("Hurt Spawn Points")]
    public Transform[] hurtSpawnPoints;

    [Header("Boat Revive Settings")]
    public Transform boatCheckpoint;            // Thuyền respawn
    public Transform playerBoatRevivePoint;     // Player respawn khi chết trên thuyền

    [HideInInspector] public bool diedOnBoat = false;   // Flag GM biết player chết trên thuyền
    private int currentHealth;
    private bool isInvulnerable = false;

    private SpriteRenderer[] boatSprites;
    private BoatController boat;
    public PlayerController player;

    private Rigidbody2D rb;
    private Vector3 boatStartPosition;

    void Start()
    {
        currentHealth = maxHealth;
        boat = GetComponent<BoatController>();
        boatSprites = GetComponentsInChildren<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        boatStartPosition = transform.position;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic; // mặc định chưa lái là kinematic
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        if (damageCollider == null)
            Debug.LogWarning("⚠️ Damage Collider chưa được gán! Kéo Collider vào Inspector.");
    }

    void Update()
    {
        if (boat != null)
            player = boat.playerOnBoat ? boat.player : null;
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (damageCollider == null || other.collider != damageCollider)
            HandleDamageCollision(other.gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (damageCollider == null || other != damageCollider)
            HandleDamageCollision(other.gameObject);
    }

    private void HandleDamageCollision(GameObject target)
    {
        if (player == null || isInvulnerable) return;

        if (target.CompareTag("Enemy") || target.CompareTag("Trap"))
        {
            SawTrap saw = target.GetComponent<SawTrap>();
            if (saw != null && !saw.IsDangerous) return;

            TakeDamage(1);
        }
    }

    public void TakeDamage(int amount)
    {
        if (isInvulnerable) return;

        currentHealth -= amount;
        StartCoroutine(HurtEffect());

        if (currentHealth <= 0)
            StartCoroutine(DieRoutine());
    }

    private IEnumerator HurtEffect()
    {
        isInvulnerable = true;

        // Phát âm thanh hurt
        if (hurtSound != null && audioSource != null)
            audioSource.PlayOneShot(hurtSound);

        if (hurtPrefab != null && hurtSpawnPoints != null && hurtSpawnPoints.Length > 0)
        {
            foreach (Transform point in hurtSpawnPoints)
                if (point != null) Instantiate(hurtPrefab, point.position, Quaternion.identity);
        }
        else if (hurtPrefab != null)
        {
            Instantiate(hurtPrefab, transform.position, Quaternion.identity);
        }

        yield return new WaitForSeconds(invulnerabilityDuration);
        isInvulnerable = false;
    }

    private IEnumerator DieRoutine()
    {
        // Phát âm thanh explosion
        if (explosionSound != null && audioSource != null)
            audioSource.PlayOneShot(explosionSound);

        if (explosionPrefab != null)
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);

        if (boat != null) boat.enabled = false;

        if (player != null)
        {
            if (playerDieEffectPrefab != null)
                Instantiate(playerDieEffectPrefab, player.transform.position, Quaternion.identity);

            player.isInputLocked = true;
            diedOnBoat = boat.playerOnBoat;
        }

        yield return null;

        foreach (var spr in boatSprites) spr.enabled = false;
        if (damageCollider != null) damageCollider.enabled = false;
        if (rb != null) rb.simulated = false;

        if (player != null)
        {
            Collider2D col = player.GetComponent<Collider2D>();
            SpriteRenderer sr = player.GetComponent<SpriteRenderer>();
            if (col != null) col.enabled = false;
            if (sr != null) sr.enabled = false;
        }

        yield return new WaitForSeconds(resetDelay);

        GameManager gm = FindObjectOfType<GameManager>();
        if (gm != null) gm.ShowGameOverFromBoat(this);
    }

    public void ResetBoatAfterRevive()
    {
        if (boatCheckpoint != null) transform.position = boatCheckpoint.position;
        else transform.position = boatStartPosition;

        currentHealth = reviveHealth;

        foreach (var spr in boatSprites) spr.enabled = true;
        if (damageCollider != null) damageCollider.enabled = true;
        if (rb != null)
        {
            rb.simulated = true;
            rb.velocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic; // mặc định chưa lái
        }

        if (boat != null)
        {
            boat.enabled = true;
            boat.playerOnBoat = false;
        }
    }

    public void OnPlayerEnterBoat()
    {
        if (rb != null) rb.bodyType = RigidbodyType2D.Dynamic;
    }

    public void OnPlayerExitBoat()
    {
        if (rb != null) rb.bodyType = RigidbodyType2D.Kinematic;
    }
}
