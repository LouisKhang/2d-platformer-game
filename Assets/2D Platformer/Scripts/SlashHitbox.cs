using UnityEngine;

namespace Platformer
{
    public class SlashHitbox : MonoBehaviour
    {
        public int damage = 1;       // Sát thương của cú chém
        public float lifeTime = 0.3f;    // Thời gian tồn tại của Hitbox

        void Start()
        {
            // Tự hủy Hitbox sau một thời gian ngắn
            Destroy(gameObject, lifeTime);
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            // --- 🎯 LOGIC GÂY SÁT THƯƠNG ---
            Vector3 slashPosition = transform.position;
            
            // >>> THÊM GOBLIN AI <<<
            GoblinAI goblin = collision.GetComponent<GoblinAI>();
            if (goblin != null)
            {
                goblin.TakeDamage(damage);
                return;
            }

            // 1. GhostAI
            GhostAI ghost = collision.GetComponent<GhostAI>();
            if (ghost != null)
            {
                ghost.TakeDamage(damage);
                return;
            }

            // 2. MinotaurController
            MinotaurController minotaur = collision.GetComponent<MinotaurController>();
            if (minotaur != null)
            {
                minotaur.TakeDamage(damage);
                return;
            }

            // 3. EvilWizardBoss
            EvilWizardBoss boss = collision.GetComponent<EvilWizardBoss>();
            if (boss != null)
            {
                // Sửa: Truyền damage
                boss.TakeDamage(damage, "Slash", slashPosition);
                return;
            }
            
            // 4. SkeletonSummonController (Quái Triệu Hồi)
            SkeletonSummonController skeletonSummon = collision.GetComponent<SkeletonSummonController>();
            if (skeletonSummon != null)
            {
                skeletonSummon.TakeDamage(damage, slashPosition);
                return;
            }

            // 5. SkeletonCombatController (Quái Xương Thường)
            SkeletonCombatController skeleton = collision.GetComponent<SkeletonCombatController>();
            if (skeleton != null)
            {
                skeleton.TakeDamage(damage, slashPosition);
                return;
            }

            // 7. FlyingEyeAI
            FlyingEyeAI flyingEye = collision.GetComponent<FlyingEyeAI>();
            if (flyingEye != null)
            {
                flyingEye.TakeDamage(damage);
                return;
            }

            // 8. MedusaAI
            MedusaAI medusa = collision.GetComponent<MedusaAI>();
            if (medusa != null)
            {
                medusa.TakeDamage(damage);
                return;
            }

            // 9. Enemy đi đất (EnemyAI)
            EnemyAI groundEnemy = collision.GetComponent<EnemyAI>();
            if (groundEnemy != null)
            {
                groundEnemy.TakeDamage(damage);
                return;
            }

            // 10. SkullwolfAI (SÓI)
            SkullwolfAI skullwolf = collision.GetComponent<SkullwolfAI>();
            if (skullwolf != null)
            {
                skullwolf.TakeDamage(damage); 
                return;
            }
            DemonController demon = collision.GetComponent<DemonController>();
            if (demon != null)
            {
                demon.TakeDamage(damage); 
                return;
            }

            // 11. Block
            BreakableBlock breakableBlock = collision.GetComponent<BreakableBlock>();
            if (breakableBlock != null)
            {
                breakableBlock.TakeHit();
                return;
            }
        }
    }
}