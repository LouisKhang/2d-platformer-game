using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Platformer;

namespace Platformer
{
    public class EnemyBombProjectile : MonoBehaviour
    {
        [Header("Damage Settings")]
        public int damageAmount = 1;

        [Header("Explosion Settings")]
        public string explosionTrigger = "Explode";
        public float explosionRadius = 1.5f;
        public float lifeTime = 5f;

        [Header("Effects")]
        public GameObject explosionEffectPrefab;

        [Header("Audio")]
        public AudioClip throwSound;
        [Range(0.0f, 2.0f)]
        public float throwVolumeMultiplier = 1.0f;

        public AudioClip explosionSound;
        [Range(0.0f, 2.0f)]
        public float explosionVolumeMultiplier = 1.0f;

        private Animator anim;
        private Rigidbody2D rb;
        private Collider2D projectileCollider;
        private bool hasExploded = false;

        private AudioSource audioSource;

        // Lưu thuyền đã bị bomb trúng để tránh gây nhiều lần
        private HashSet<BoatDamageController> damagedBoats = new HashSet<BoatDamageController>();

        void Start()
        {
            rb = GetComponent<Rigidbody2D>();
            anim = GetComponent<Animator>();
            projectileCollider = GetComponent<Collider2D>();
            audioSource = GetComponent<AudioSource>();

            // Nếu chưa có AudioSource thì thêm mới
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();

            // Phát âm thanh ném
            if (audioSource != null && throwSound != null)
                audioSource.PlayOneShot(throwSound, throwVolumeMultiplier);

            Invoke("Explode", lifeTime);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (hasExploded) return;

            // Nếu va chạm với Player thì bỏ qua
            if (collision.gameObject.CompareTag("Player")) return;

            // Nếu va chạm với thuyền Player
            if (collision.gameObject.CompareTag("Boat"))
            {
                BoatDamageController boat = collision.collider.GetComponentInParent<BoatDamageController>();
                if (boat != null && !damagedBoats.Contains(boat))
                {
                    boat.TakeDamage(damageAmount);
                    damagedBoats.Add(boat);
                }
            }

            CancelInvoke("Explode");
            Explode();
        }

        private void Explode()
        {
            if (hasExploded) return;
            hasExploded = true;

            CancelInvoke("Explode");

            // Phát âm thanh nổ
            if (explosionSound != null)
                AudioSource.PlayClipAtPoint(explosionSound, transform.position, explosionVolumeMultiplier);

            if (rb != null)
            {
                rb.velocity = Vector2.zero;
                rb.isKinematic = true;
            }
            if (projectileCollider != null)
                projectileCollider.enabled = false;

            if (anim != null)
                anim.SetTrigger(explosionTrigger);

            if (explosionEffectPrefab != null)
                Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);

            // --------------------------
            // Gây damage các enemy khác xung quanh
            // --------------------------
            HashSet<GameObject> damagedObjects = new HashSet<GameObject>();
            Collider2D[] objectsInExplosion = Physics2D.OverlapCircleAll(transform.position, explosionRadius);

            foreach (Collider2D nearby in objectsInExplosion)
            {
                GameObject targetObject = nearby.gameObject;
                if (damagedObjects.Contains(targetObject)) continue;
                damagedObjects.Add(targetObject);

                // Không gây damage lên thuyền Player nữa
                if (targetObject.CompareTag("Boat")) continue;

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

                var breakableBlock = targetObject.GetComponent<BreakableBlock>();
                if (breakableBlock != null)
                {
                    for (int i = 0; i < damageAmount; i++)
                        breakableBlock.TakeHit();
                }
            }

            StartCoroutine(DestroyAfterExplosion(0.5f));
        }

        private IEnumerator DestroyAfterExplosion(float delay)
        {
            yield return new WaitForSeconds(delay);
            Destroy(gameObject);
        }
    }
}
