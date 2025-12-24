using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace Platformer
{
    public class GameManager : MonoBehaviour
    {
        [Header("Coin System")]
        public int startingCoins = 10;
        public int coinsCounter;
        [Tooltip("Nhập tổng số lượng Coin có trong Scene này.")]
        public int totalCoinsInLevel = 100;

        [Header("Bomb System")]
        public int startingBombs = 0;
        public int bombCounter;

        [Header("Heart System")]
        public int maxHearts = 20;
        public int currentHearts;
        public int normalReviveHearts = 5;

        [Header("Checkpoint System")]
        private Vector3 lastCheckpointPosition;

        [Header("Death Position")]
        private Vector3 lastDeathPosition;

        [Header("Player References")]
        public GameObject playerGameObject;
        private PlayerController player;
        public GameObject deathPlayerPrefab;

        [Header("UI References")]
        public Text coinText;
        public Text heartText;
        public Text bombText;

        [Header("Key System UI")]
        public GameObject keyIconUI;

        [Header("GameOver Canvas")]
        public GameObject gameOverCanvas;
        public Button homeButton;
        public Button restartButton;
        public Button reviveButton;
        public int reviveCost = 50;

        [Header("Game Win Canvas")]
        public GameObject gameWinCanvas;
        public Button levelSelectButton;
        public Button homeMenuButton;
        public Button winRestartButton;
        public Button nextLevelButton;
        private string nextSceneToLoad;

        [Header("Game Win UI Display (Star & Score)")]
        public TextMeshProUGUI scoreTextTMP;
        public Image[] starImages = new Image[3];
        public Sprite starSpriteFilled;

        [Header("Star Thresholds (Coin % collected)")]
        public float threeStarThreshold = 100f;
        public float twoStarThreshold = 50f;

        [Header("Invulnerability Settings")]
        public float invulnerableDuration = 3f;
        private const float BLINK_INTERVAL = 0.1f;

        [Header("Audio")] 
        public AudioSource backgroundMusicSource;

        [Header("Sound Effects")]
        public AudioSource sfxSource;
        public AudioClip dieSFX;
        public AudioClip hurtSFX;
        public AudioClip reviveSFX;

        [Header("UI Music")]
        public AudioSource gameWinMusicSource;   // Nhạc Game Win
        public AudioSource gameOverMusicSource;  // Nhạc Game Over

        private bool isDying = false;
        private bool diedOnBoat = false;
        private BoatDamageController lastBoatDeath;
        private bool isGamePaused = false;

        public bool IsGamePaused => isGamePaused;

        // =====================================================================================
        // START
        // =====================================================================================
        void Start()
        {
            Time.timeScale = 1f;
            isGamePaused = false;

            coinsCounter = startingCoins;
            bombCounter = startingBombs;
            currentHearts = maxHearts;

            if (playerGameObject != null)
            {
                player = playerGameObject.GetComponent<PlayerController>();
                lastCheckpointPosition = playerGameObject.transform.position;
                playerGameObject.SetActive(true);

                if (player != null)
                    player.currentBombs = bombCounter;
            }

            UpdateHeartUI();
            UpdateCoinUI();
            UpdateBombUI();
            UpdateKeyStatus(false);

            if (gameOverCanvas != null) gameOverCanvas.SetActive(false);
            if (gameWinCanvas != null) gameWinCanvas.SetActive(false);

            if (homeButton != null)
                homeButton.onClick.AddListener(() => { UnpauseGame(); SceneManager.LoadScene("Menu"); });

            if (restartButton != null)
                restartButton.onClick.AddListener(() => { UnpauseGame(); SceneManager.LoadScene(SceneManager.GetActiveScene().name); });

            if (reviveButton != null)
                reviveButton.onClick.AddListener(RevivePlayer);

            if (levelSelectButton != null)
                levelSelectButton.onClick.AddListener(() => { UnpauseGame(); SceneManager.LoadScene("LevelSelection"); });

            if (homeMenuButton != null)
                homeMenuButton.onClick.AddListener(() => { UnpauseGame(); SceneManager.LoadScene("Menu"); });

            if (winRestartButton != null)
                winRestartButton.onClick.AddListener(() => { UnpauseGame(); SceneManager.LoadScene(SceneManager.GetActiveScene().name); });

            if (nextLevelButton != null)
                nextLevelButton.onClick.AddListener(LoadNextLevelAfterWin);
        }

        // =====================================================================================
        // PAUSE / UNPAUSE
        // =====================================================================================
        public void PauseGame()
        {
            if (Time.timeScale > 0)
            {
                Time.timeScale = 0f;
                isGamePaused = true;
                if (backgroundMusicSource != null) backgroundMusicSource.Pause();
            }
        }

        public void UnpauseGame()
        {
            if (Time.timeScale == 0)
            {
                Time.timeScale = 1f;
                isGamePaused = false;
                if (backgroundMusicSource != null && !backgroundMusicSource.isPlaying)
                    backgroundMusicSource.UnPause();
            }
        }

        // =====================================================================================
        // UPDATE
        // =====================================================================================
        void Update()
        {
            if (Time.timeScale == 0f) return;

            UpdateCoinUI();
            UpdateBombUI();

            if (player != null && player.deathState && !isDying)
            {
                isDying = true;
                player.deathState = false;

                lastDeathPosition = playerGameObject.transform.position;

                StartCoroutine(HandlePlayerDeathEffectAndRespawn(
                    lastDeathPosition, 
                    currentHearts <= 0
                ));
            }
        }

        // =====================================================================================
        // CLEAR DEATH BODIES
        // =====================================================================================
        private void ClearAllDeathBodies()
        {
            GameObject[] bodies = GameObject.FindGameObjectsWithTag("DeathPlayer");
            foreach (var b in bodies)
                Destroy(b);
        }

        // =====================================================================================
        // DEATH EFFECT
        // =====================================================================================
        private IEnumerator HandlePlayerDeathEffectAndRespawn(Vector3 deathPosition, bool isGameOver)
        {
            PlaySFX(dieSFX);

            GameObject deathPlayer = null;
            float waitTime = 1.0f;

            if (deathPlayerPrefab != null)
            {
                deathPlayer = Instantiate(deathPlayerPrefab, deathPosition, Quaternion.identity);
                Animator deathAnim = deathPlayer.GetComponent<Animator>();
                if (deathAnim != null)
                    deathAnim.SetTrigger("Die");

                playerGameObject.SetActive(false);
            }

            yield return new WaitForSeconds(waitTime);

            if (deathPlayer != null) Destroy(deathPlayer);

            if (isGameOver)
            {
                PauseGame();
                ShowGameOver();
            }
            else
            {
                StartCoroutine(RespawnPlayerRoutine(lastCheckpointPosition));
            }
        }

        // =====================================================================================
        // GAME OVER
        // =====================================================================================
        public void ShowGameOverFromBoat(BoatDamageController boat)
        {
            diedOnBoat = true;
            lastBoatDeath = boat; 
            PauseGame();
            ShowGameOver();
        }

        public void ShowGameOver()
        {
            if (gameOverCanvas != null)
            {
                gameOverCanvas.SetActive(true);
                if (reviveButton != null)
                    reviveButton.interactable = coinsCounter >= reviveCost;

                if (gameOverMusicSource != null)
                {
                    gameOverMusicSource.Play();
                    if (backgroundMusicSource != null && backgroundMusicSource.isPlaying)
                        backgroundMusicSource.Pause();
                }
            }
        }

        // =====================================================================================
        // REVIVE
        // =====================================================================================
        private void RevivePlayer()
        {
            if (coinsCounter < reviveCost) return;

            ClearAllDeathBodies();

            coinsCounter -= reviveCost;
            UpdateCoinUI();

            PlaySFX(reviveSFX);

            gameOverCanvas.SetActive(false);

            if (gameOverMusicSource != null && gameOverMusicSource.isPlaying)
                gameOverMusicSource.Stop();

            UnpauseGame();
            isDying = false;

            if (diedOnBoat && lastBoatDeath != null)
                RevivePlayerFromBoat();
            else
                NormalRevive();

            diedOnBoat = false;
            lastBoatDeath = null;

            if (backgroundMusicSource != null && !backgroundMusicSource.isPlaying)
                backgroundMusicSource.Play();
        }

        private void NormalRevive()
        {
            ClearAllDeathBodies();

            currentHearts = normalReviveHearts;
            UpdateHeartUI();
            ResetPlayerAfterRevive();

            Vector3 revivePos = lastDeathPosition;

            StartCoroutine(RespawnPlayerRoutine(revivePos));
        }

        private void RevivePlayerFromBoat()
        {
            ClearAllDeathBodies();

            currentHearts = 15;
            UpdateHeartUI();
            ResetPlayerAfterRevive();

            Vector3 revivePos = lastCheckpointPosition;

            if (lastBoatDeath != null)
            {
                lastBoatDeath.ResetBoatAfterRevive();

                if (lastBoatDeath.playerBoatRevivePoint != null)
                    revivePos = lastBoatDeath.playerBoatRevivePoint.position;
                else if (lastBoatDeath.boatCheckpoint != null)
                    revivePos = lastBoatDeath.boatCheckpoint.position;
            }

            StartCoroutine(RespawnPlayerRoutine(revivePos));
        }

        // =====================================================================================
        // RESET PLAYER
        // =====================================================================================
        private void ResetPlayerAfterRevive()
        {
            if (player != null)
            {
                player.deathState = false;
                player.isInputLocked = false;

                Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
                Animator anim = player.GetComponent<Animator>();
                Collider2D col = player.GetComponent<Collider2D>();
                SpriteRenderer sr = player.GetComponent<SpriteRenderer>();

                if (rb != null)
                {
                    rb.velocity = Vector2.zero;
                    rb.angularVelocity = 0f;
                    rb.gravityScale = 1.5f;
                    rb.simulated = true;
                    rb.constraints = RigidbodyConstraints2D.FreezeRotation;
                }

                if (anim != null) anim.Rebind();
                if (col != null) col.enabled = true;
                if (sr != null) sr.enabled = true;
            }
        }

        // =====================================================================================
        // RESPAWN ROUTINE
        // =====================================================================================
        private IEnumerator RespawnPlayerRoutine(Vector3 spawnPosition)
        {
            Rigidbody2D rb = playerGameObject.GetComponent<Rigidbody2D>();
            Animator anim = playerGameObject.GetComponent<Animator>();
            SpriteRenderer sr = playerGameObject.GetComponent<SpriteRenderer>();
            Collider2D col = playerGameObject.GetComponent<Collider2D>();

            if (player != null)
            {
                player.enabled = false;
                player.isInputLocked = true;
            }

            if (col != null) col.enabled = false;

            playerGameObject.transform.position = spawnPosition;

            if (rb != null)
            {
                rb.velocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.gravityScale = 1.5f;
                rb.simulated = true;
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            }

            playerGameObject.SetActive(true);
            if (sr != null) sr.enabled = true;

            if (anim != null)
            {
                anim.Rebind();
                anim.Update(0f);
                anim.SetInteger("playerState", 0);
            }

            if (player != null)
            {
                player.SetInvulnerable(true);
                StartCoroutine(PlayerInvulnerableRoutine(invulnerableDuration));
            }

            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            if (col != null) col.enabled = true;
            if (player != null) player.enabled = true;

            yield return null;

            if (rb != null) rb.velocity = Vector2.zero;
            if (player != null) player.isInputLocked = false;

            isDying = false;
        }

        private IEnumerator PlayerInvulnerableRoutine(float duration)
        {
            SpriteRenderer sr = playerGameObject.GetComponent<SpriteRenderer>();
            float startTime = Time.time;
            bool visible = true;

            while (Time.time < startTime + duration)
            {
                if (sr != null)
                {
                    sr.enabled = visible;
                    visible = !visible;
                }
                yield return new WaitForSeconds(BLINK_INTERVAL);
            }

            if (sr != null) sr.enabled = true;
            if (player != null) player.SetInvulnerable(false);
        }

        // =====================================================================================
        // LEVEL WIN
        // =====================================================================================
        public void HandleLevelWin(string nextScene)
        {
            if (playerGameObject != null && player != null)
            {
                player.enabled = false;
                player.isInputLocked = true;

                Rigidbody2D rb = playerGameObject.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.velocity = Vector2.zero;
                    rb.angularVelocity = 0f;
                }
            }

            PauseGame();

            int maxCoins = (totalCoinsInLevel > 0) ? totalCoinsInLevel : 1;
            float percentageCollected = ((float)coinsCounter / maxCoins) * 100f;

            int starsGained = CalculateStars(percentageCollected);
            int finalScore = CalculateScore(coinsCounter, starsGained);

            UpdateWinUI(starsGained, finalScore);

            nextSceneToLoad = nextScene;

            ShowGameWinCanvas();
        }

        private int CalculateStars(float percentage)
        {
            if (percentage >= threeStarThreshold) return 3;
            else if (percentage >= twoStarThreshold) return 2;
            else return 1;
        }

        private int CalculateScore(int coinsCollected, int stars)
        {
            return (coinsCollected * 10) + (stars * 50);
        }

        private void UpdateWinUI(int starsGained, int score)
        {
            if (scoreTextTMP != null)
                scoreTextTMP.text = score.ToString("N0");

            if (starImages.Length == 3 && starSpriteFilled != null)
            {
                for (int i = 0; i < 3; i++)
                {
                    if (i < starsGained)
                    {
                        starImages[i].sprite = starSpriteFilled;
                        starImages[i].color = Color.white;
                    }
                }
            }
        }

        public void ShowGameWinCanvas()
        {
            if (gameWinCanvas != null)
            {
                gameWinCanvas.SetActive(true);

                if (nextLevelButton != null)
                {
                    nextLevelButton.interactable =
                        !string.IsNullOrEmpty(nextSceneToLoad) &&
                        nextSceneToLoad != SceneManager.GetActiveScene().name;
                }

                if (gameWinMusicSource != null)
                {
                    gameWinMusicSource.Play();
                    if (backgroundMusicSource != null && backgroundMusicSource.isPlaying)
                        backgroundMusicSource.Pause();
                }
            }
        }

        private void LoadNextLevelAfterWin()
        {
            UnpauseGame();

            if (gameWinMusicSource != null && gameWinMusicSource.isPlaying)
                gameWinMusicSource.Stop();

            if (backgroundMusicSource != null && !backgroundMusicSource.isPlaying)
                backgroundMusicSource.Play();

            if (!string.IsNullOrEmpty(nextSceneToLoad) &&
                nextSceneToLoad != SceneManager.GetActiveScene().name)
                SceneManager.LoadScene(nextSceneToLoad);
            else
                SceneManager.LoadScene("LevelSelection");
        }

        // =====================================================================================
        // UI UPDATE
        // =====================================================================================
        public void UpdateHeartUI()
        {
            if (heartText != null)
                heartText.text = currentHearts.ToString();
        }

        public void UpdateCoinUI()
        {
            if (coinText != null)
                coinText.text = coinsCounter.ToString();
        }

        public void UpdateBombUI()
        {
            if (bombText != null)
                bombText.text = bombCounter.ToString();
        }

        // =====================================================================================
        // ADD / USE ITEMS
        // =====================================================================================
        public void AddHeart()
        {
            if (currentHearts < maxHearts)
            {
                currentHearts++;
                UpdateHeartUI();
            }
        }

        public void AddCoin(int amount)
        {
            coinsCounter += amount;
            UpdateCoinUI();
        }

        public void AddBomb(int amount)
        {
            bombCounter += amount;

            if (player != null)
                player.currentBombs = bombCounter;

            UpdateBombUI();
        }

        public void UseBomb()
        {
            if (bombCounter > 0)
            {
                bombCounter--;

                if (player != null)
                    player.currentBombs = bombCounter;

                UpdateBombUI();
            }
        }

        // =====================================================================================
        // CHECKPOINT
        // =====================================================================================
        public void UpdateCheckpoint(Vector3 newPosition)
        {
            lastCheckpointPosition = new Vector3(newPosition.x, newPosition.y, 0f);
        }

        // =====================================================================================
        // OTHER
        // =====================================================================================
        public void LoadNextLevel(string sceneName)
        {
            UnpauseGame();
            SceneManager.LoadScene(sceneName);
        }

        public void UpdateKeyStatus(bool status)
        {
            if (keyIconUI != null)
                keyIconUI.SetActive(status);
        }

        public void PlayerHit(int damage)
        {
            if (isDying) return;

            currentHearts -= damage;
            UpdateHeartUI();

            if (currentHearts <= 0)
                player.deathState = true;
        }

        public void PlaySFX(AudioClip clip)
        {
            if (sfxSource != null && clip != null)
                sfxSource.PlayOneShot(clip);
        }
    }
}
