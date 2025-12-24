using UnityEngine;
using System.Collections.Generic;

namespace Platformer
{
    public class BoatController : MonoBehaviour
    {
        // ********* SỰ KIỆN *********
        [HideInInspector] public event System.Action OnEnterBoat;
        [HideInInspector] public event System.Action OnExitBoat;
        // ****************************************
        
        [Header("Boat Settings")]
        public float moveSpeed = 4f;

        [Header("Player Points")]
        public Transform playerMountPoint;
        public Transform[] exitPoints;

        [Header("Anchor Settings")]
        public Transform[] anchorPoints;
        public float anchorDistance = 2f;

        [HideInInspector] public bool playerOnBoat = false;
        [HideInInspector] public PlayerController player;

        private Collider2D playerCollider;
        private bool nearAnchor = false;
        private Transform currentAnchor;

        [HideInInspector] public int boatFacing = 1;

        [Header("Smoke Effect")]
        public GameObject smokePrefab;
        public Transform smokeSpawnPoint;
        public float smokeSpawnInterval = 0.2f;
        public int smokePoolSize = 10;

        private float smokeTimer = 0f;
        private List<GameObject> smokePool = new List<GameObject>();
        private int smokeIndex = 0;

        private Rigidbody2D rb;
        private GameManager gameManager;

        [Header("Audio")]
        public AudioSource boatAudioSource;   // Gán AudioSource chứa âm thanh lái thuyền
        public bool loopBoatAudio = true;

        void Start()
        {
            rb = GetComponent<Rigidbody2D>();
            
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            }

            // Tạo pool smoke
            if (smokePrefab != null)
            {
                for (int i = 0; i < smokePoolSize; i++)
                {
                    GameObject smoke = Instantiate(smokePrefab);
                    smoke.SetActive(false);
                    smokePool.Add(smoke);
                }
            }

            gameManager = FindObjectOfType<GameManager>();
            if (gameManager == null)
            {
                Debug.LogError("🚨 Không tìm thấy GameManager trong Scene! Thuyền cần GameManager để tăng coin.");
            }

            // Cấu hình AudioSource
            if (boatAudioSource != null)
            {
                boatAudioSource.loop = loopBoatAudio;
                boatAudioSource.playOnAwake = false;
            }
        }

        void Update()
        {
            if (!playerOnBoat) return;

            float input = Input.GetAxisRaw("Horizontal");

            if (rb != null)
            {
                rb.velocity = new Vector2(input * moveSpeed, rb.velocity.y);
            }

            if (input != 0)
            {
                // Quay thuyền theo hướng
                boatFacing = -(int)Mathf.Sign(input);
                Vector3 scale = transform.localScale;
                scale.x = Mathf.Abs(scale.x) * boatFacing;
                transform.localScale = scale;

                if (player != null)
                {
                    Vector3 mountPos = playerMountPoint.localPosition;
                    player.transform.localPosition = new Vector3(
                        Mathf.Abs(mountPos.x) * boatFacing,
                        mountPos.y,
                        mountPos.z
                    );
                }

                SpawnSmoke();
                PlayBoatSound();
            }
            else
            {
                smokeTimer = 0f;
                for (int i = 0; i < smokePool.Count; i++)
                {
                    if (smokePool[i].activeSelf)
                        smokePool[i].SetActive(false);
                }

                if (rb != null)
                {
                    rb.velocity = new Vector2(0, rb.velocity.y);
                }

                StopBoatSound();
            }

            CheckNearestAnchor();
        }

        private void SpawnSmoke()
        {
            if (smokePrefab == null || smokeSpawnPoint == null) return;

            smokeTimer -= Time.deltaTime;
            if (smokeTimer <= 0f)
            {
                GameObject smoke = smokePool[smokeIndex];
                smoke.transform.position = smokeSpawnPoint.position;
                smoke.SetActive(true);

                ParticleSystem ps = smoke.GetComponent<ParticleSystem>();
                if (ps != null)
                    ps.Play();

                smokeIndex = (smokeIndex + 1) % smokePool.Count;
                smokeTimer = smokeSpawnInterval;
            }
        }

        private void CheckNearestAnchor()
        {
            nearAnchor = false;
            currentAnchor = null;

            if (anchorPoints == null || anchorPoints.Length == 0) return;

            foreach (Transform anchor in anchorPoints)
            {
                if (anchor == null) continue;
                if (Vector2.Distance(transform.position, anchor.position) <= anchorDistance)
                {
                    nearAnchor = true;
                    currentAnchor = anchor;
                    break;
                }
            }
        }

        public void EnterBoat(PlayerController p)
        {
            if (playerOnBoat || p == null) return;

            playerOnBoat = true;
            player = p;
            player.DisableMovement();

            Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
            playerCollider = player.GetComponent<Collider2D>();

            playerRb.velocity = Vector2.zero;
            playerRb.gravityScale = 0f;

            if (playerCollider != null)
                playerCollider.enabled = false;

            player.transform.position = playerMountPoint.position;
            player.transform.SetParent(transform);
            player.nearBoat = this;
            
            OnEnterBoat?.Invoke();

            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.velocity = Vector2.zero;
            }
        }

        public void RequestExitBoat()
        {
            if (!playerOnBoat || player == null) return;

            if (nearAnchor && currentAnchor != null)
                PerformExitBoat();
            else
                Debug.Log("🚫 Không thể xuống thuyền vì chưa gần Neo.");
        }

        private void PerformExitBoat()
        {
            if (!playerOnBoat || player == null) return;

            playerOnBoat = false;
            player.transform.SetParent(null);

            if (playerCollider != null)
                playerCollider.enabled = true;

            Transform exitPoint = null;
            float minDist = float.MaxValue;
            if (exitPoints != null)
            {
                foreach (Transform e in exitPoints)
                {
                    if (e == null) continue;
                    float dist = Vector2.Distance(transform.position, e.position);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        exitPoint = e;
                    }
                }
            }

            player.transform.position = exitPoint != null ? exitPoint.position : transform.position + Vector3.up;

            Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
            playerRb.velocity = Vector2.zero;
            playerRb.gravityScale = player.originalGravityScale;

            player.EnableMovement();
            player.nearBoat = null;
            
            OnExitBoat?.Invoke();

            if (rb != null)
            {
                rb.velocity = Vector2.zero;
                rb.bodyType = RigidbodyType2D.Kinematic;
            }

            player = null;
            playerCollider = null;
            currentAnchor = null;
            nearAnchor = false;

            StopBoatSound();
        }
        
        public void ForceExitBoat()
        {
            PerformExitBoat();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.gameObject.CompareTag("Coin"))
            {
                if (gameManager != null)
                    gameManager.coinsCounter += 1;

                Destroy(other.gameObject);
            }
        }

        // =================== AUDIO ===================
        private void PlayBoatSound()
        {
            if (boatAudioSource != null && !boatAudioSource.isPlaying)
                boatAudioSource.Play();
        }

        private void StopBoatSound()
        {
            if (boatAudioSource != null && boatAudioSource.isPlaying)
                boatAudioSource.Stop();
        }
    }
}
