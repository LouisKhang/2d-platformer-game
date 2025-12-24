using UnityEngine;
using System.Collections;
using System.Collections.Generic; 

namespace Platformer
{
    public class BombProjectile : MonoBehaviour
    {
        [Header("Damage Settings")]
        public int damageAmount = 5 ; 
        
        [Header("Explosion Settings")]
        public string explosionTrigger = "Explode"; 
        public float explosionRadius = 1.5f; 
        public float lifeTime = 5f; 

        // ⭐ TRƯỜNG MỚI ĐỂ ĐIỀU CHỈNH ÂM LƯỢNG ⭐
        [Header("Audio")]
        public AudioClip throwSound; 
        [Range(0.0f, 2.0f)] // Giới hạn kéo thả từ 0.0 đến 2.0 (có thể tăng thêm)
        public float throwVolumeMultiplier = 1.0f; // Mặc định là 1.0 (âm lượng gốc)

        public AudioClip explosionSound;
        [Range(0.0f, 2.0f)] // Giới hạn kéo thả từ 0.0 đến 2.0 (có thể tăng thêm)
        public float explosionVolumeMultiplier = 1.0f; // Mặc định là 1.0 (âm lượng gốc)


        [Header("Effects")]
        public GameObject explosionEffectPrefab; 
        public GameObject hurtEffectPrefab; 

        private Animator anim;
        private Rigidbody2D rb;
        private Collider2D projectileCollider;
        private bool hasExploded = false; 
        private AudioSource audioSource; 

        void Start()
        {
            rb = GetComponent<Rigidbody2D>();
            anim = GetComponent<Animator>();
            projectileCollider = GetComponent<Collider2D>();
            audioSource = GetComponent<AudioSource>();

            // Lấy hoặc thêm AudioSource
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            // ⭐ PHÁT ÂM THANH NÉM (Sử dụng Volume Multiplier) ⭐
            if (audioSource != null && throwSound != null)
            {
                // PlayOneShot cho phép bạn chỉ định âm lượng phát
                audioSource.PlayOneShot(throwSound, throwVolumeMultiplier);
            }
            
            Invoke("Explode", lifeTime); 
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (hasExploded) return;
            
            if (collision.gameObject.CompareTag("Player"))
                return;
            
            CancelInvoke("Explode"); 
            Explode();
        }

        private void Explode()
        {
            if (hasExploded) return;
            hasExploded = true; 
            CancelInvoke("Explode"); 
            
            // ⭐ PHÁT ÂM THANH NỔ (Sử dụng Volume Multiplier) ⭐
            if (explosionSound != null)
            {
                // PlayClipAtPoint cho phép bạn chỉ định âm lượng phát
                AudioSource.PlayClipAtPoint(explosionSound, transform.position, explosionVolumeMultiplier);
            }

            // Dừng chuyển động và tắt Collider
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
                rb.isKinematic = true;
            }
            if (projectileCollider != null) projectileCollider.enabled = false;
            
            // Kích hoạt animation nổ
            if (anim != null)
                anim.SetTrigger(explosionTrigger);

            // Tạo hiệu ứng nổ (VFX)
            if (explosionEffectPrefab != null)
                Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
            
            // -------------------------------------------------------------
            // Gây sát thương
            // -------------------------------------------------------------
            HashSet<GameObject> damagedObjects = new HashSet<GameObject>();
            Collider2D[] objectsInExplosion = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
            
            foreach (Collider2D nearby in objectsInExplosion)
            {
                GameObject targetObject = nearby.gameObject; 
                if (damagedObjects.Contains(targetObject)) continue;
                damagedObjects.Add(targetObject);

                // Gây damage cho EnemyBoat
                var enemyBoat = targetObject.GetComponent<BoatEnemy>();
                if (enemyBoat != null)
                {
                    enemyBoat.TakeDamage(damageAmount);
                    if (hurtEffectPrefab != null)
                        Instantiate(hurtEffectPrefab, targetObject.transform.position, Quaternion.identity);
                    StartCoroutine(FlashWhite(enemyBoat.gameObject));
                    continue;
                }
                
                // >>> THÊM GOBLIN AI <<<
                var goblin = targetObject.GetComponent<GoblinAI>();
                if (goblin != null) { goblin.TakeDamage(damageAmount); continue; }
                
                // Các enemy khác
                var ghost = targetObject.GetComponent<GhostAI>();
                if (ghost != null) { ghost.TakeDamage(damageAmount); continue; }
                
                var minotaur = targetObject.GetComponent<MinotaurController>();
                if (minotaur != null) { minotaur.TakeDamage(damageAmount); continue; }
                
                var medusa = targetObject.GetComponent<MedusaAI>();
                if (medusa != null) { medusa.TakeDamage(damageAmount); continue; }

                var skullwolf = targetObject.GetComponent<SkullwolfAI>();
                if (skullwolf != null) { skullwolf.TakeDamage(damageAmount); continue; }

                var boss = targetObject.GetComponent<EvilWizardBoss>();
                if (boss != null) 
                { 
                    boss.TakeDamage(damageAmount, "Explosion", transform.position); 
                    continue; 
                }

                var skeletonSummon = targetObject.GetComponent<SkeletonSummonController>();
                if (skeletonSummon != null) { skeletonSummon.TakeDamage(damageAmount, transform.position); continue; }

                var skeleton = targetObject.GetComponent<SkeletonCombatController>();
                if (skeleton != null) { skeleton.TakeDamage(damageAmount, transform.position); continue; }

                var flyingEye = targetObject.GetComponent<FlyingEyeAI>();
                if (flyingEye != null) { flyingEye.TakeDamage(damageAmount); continue; }

                var groundEnemy = targetObject.GetComponent<EnemyAI>();
                if (groundEnemy != null) { groundEnemy.TakeDamage(damageAmount); continue; }
                
                var demon = targetObject.GetComponent<DemonController>();
                if (demon != null) { demon.TakeDamage(damageAmount); continue; }

                // Phá Block
                BreakableBlock breakableBlock = targetObject.GetComponent<BreakableBlock>();
                if (breakableBlock != null)
                {
                    // Bom gây damage theo damageAmount
                    for (int i = 0; i < damageAmount; i++)
                        breakableBlock.TakeHit();
                    continue;
                }
            }
            
            // Hủy đối tượng bom sau khi hiệu ứng animation nổ kết thúc (0.5s)
            StartCoroutine(DestroyAfterExplosion(0.5f)); 
        }
        
        private IEnumerator DestroyAfterExplosion(float delay)
        {
            yield return new WaitForSeconds(delay);
            Destroy(gameObject);
        }

        private IEnumerator FlashWhite(GameObject target)
        {
            SpriteRenderer sr = target.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                Color original = sr.color;
                sr.color = Color.white;
                yield return new WaitForSeconds(0.1f);
                sr.color = original;
            }
        }
    }
}