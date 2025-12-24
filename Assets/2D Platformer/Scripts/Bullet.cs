using UnityEngine;
using System.Collections.Generic;

namespace Platformer
{
    public class Bullet : MonoBehaviour
    {
        public const int damage = 2; // Sát thương cố định
        public float lifeTime = 2f;

        // ⭐ BIẾN ÂM THANH (NHẬN TỪ PLAYER SHOOT) ⭐
        // Dùng [HideInInspector] để ẩn nó khỏi Inspector của Prefab Bullet
        [HideInInspector] public AudioClip hitSoundClip;

        // ⭐ HÀM SETTER ĐỂ PLAYER SHOOT GỌI VÀ TRUYỀN CLIP VÀO ⭐
        public void SetHitSound(AudioClip clip)
        {
            hitSoundClip = clip;
        }

        private void Start()
        {
            Destroy(gameObject, lifeTime);
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            // --- 🎯 LOGIC GÂY SÁT THƯƠNG ---
            Vector3 bulletPosition = transform.position;
            bool hitRegistered = false; 

            // >>> THÊM GOBLIN AI <<<
            GoblinAI goblin = collision.GetComponent<GoblinAI>();
            if (goblin != null)
            {
                goblin.TakeDamage(damage);
                hitRegistered = true;
            }
            
            // 1. GhostAI
            GhostAI ghost = collision.GetComponent<GhostAI>();
            if (ghost != null)
            {
                ghost.TakeDamage(damage);
                hitRegistered = true;
            }

            // 2. MinotaurController
            MinotaurController minotaur = collision.GetComponent<MinotaurController>();
            if (minotaur != null)
            {
                minotaur.TakeDamage(damage);
                hitRegistered = true;
            }

            // 3. EvilWizardBoss (Cần 3 tham số)
            EvilWizardBoss boss = collision.GetComponent<EvilWizardBoss>();
            if (boss != null)
            {
                boss.TakeDamage(damage, "Bullet", bulletPosition);
                hitRegistered = true;
            }

            // 4. SkeletonSummonController (Cần vị trí)
            SkeletonSummonController skeletonSummon = collision.GetComponent<SkeletonSummonController>();
            if (skeletonSummon != null)
            {
                skeletonSummon.TakeDamage(damage, bulletPosition);
                hitRegistered = true;
            }

            // 5. SkeletonCombatController (Cần vị trí)
            SkeletonCombatController skeleton = collision.GetComponent<SkeletonCombatController>();
            if (skeleton != null)
            {
                skeleton.TakeDamage(damage, bulletPosition);
                hitRegistered = true;
            }

            // 7. FlyingEyeAI
            FlyingEyeAI flyingEye = collision.GetComponent<FlyingEyeAI>();
            if (flyingEye != null)
            {
                flyingEye.TakeDamage(damage);
                hitRegistered = true;
            }

            // 8. MedusaAI
            MedusaAI medusa = collision.GetComponent<MedusaAI>();
            if (medusa != null)
            {
                medusa.TakeDamage(damage);
                hitRegistered = true;
            }

            // 9. Enemy đi đất (EnemyAI)
            EnemyAI groundEnemy = collision.GetComponent<EnemyAI>();
            if (groundEnemy != null)
            {
                groundEnemy.TakeDamage(damage);
                hitRegistered = true;
            }
            DemonController demon = collision.GetComponent<DemonController>();
            if (demon != null)
            {
                demon.TakeDamage(damage);
                hitRegistered = true;
            }
            

            // 10. SkullwolfAI (SÓI)
            SkullwolfAI skullwolf = collision.GetComponent<SkullwolfAI>();
            if (skullwolf != null)
            {
                skullwolf.TakeDamage(damage); 
                hitRegistered = true;
            }

            // 11. Block
            BreakableBlock breakableBlock = collision.GetComponent<BreakableBlock>();
            if (breakableBlock != null)
            {
                breakableBlock.TakeHit();
                hitRegistered = true;
            }
            
            // ⭐ XỬ LÝ ÂM THANH VÀ HỦY ĐẠN KHI TRÚNG MỤC TIÊU
            if (hitRegistered)
            {
                PlayHitSound(bulletPosition);
                Destroy(gameObject);
                return;
            }

            // --- 🧱 LOGIC VA CHẠM KHÁC (Địa hình/Tường) ---
            int layer = collision.gameObject.layer;
            if (layer == LayerMask.NameToLayer("Ground") ||
                layer == LayerMask.NameToLayer("Wall") ||
                layer == LayerMask.NameToLayer("BackGround"))
            {
                Debug.Log($"🧱 Bullet chạm {collision.gameObject.name} → hủy");
                
                // Phát âm thanh trúng và hủy đạn
                PlayHitSound(bulletPosition);
                Destroy(gameObject);
            }
        }
        
        // Hàm hỗ trợ phát âm thanh TRÚNG
        private void PlayHitSound(Vector3 position)
        {
            if (hitSoundClip != null)
            {
                AudioSource.PlayClipAtPoint(hitSoundClip, position);
            }
        }
    }
}