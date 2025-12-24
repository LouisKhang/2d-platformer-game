using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Platformer
{
    public class BoatTurretController : MonoBehaviour
    {
        [Header("References")]
        public Transform firePoint;
        public GameObject bombPrefab;
        public float fireCooldown = 5f;

        [Header("Boat Colliders To Ignore")]
        public List<Collider2D> ignoreColliders;

        private bool canFire = true;
        private BoatController boat;

        void Start()
        {
            boat = GetComponentInParent<BoatController>();
        }

        void Update()
        {
            // Chỉ bắn khi player trên thuyền + nhấn chuột trái
            if (boat != null && boat.playerOnBoat && Input.GetMouseButtonDown(0) && canFire)
                FireBomb();
        }

        private void FireBomb()
        {
            if (bombPrefab == null || firePoint == null) return;

            GameObject bomb = Instantiate(bombPrefab, firePoint.position, firePoint.rotation);

            Rigidbody2D rb = bomb.GetComponent<Rigidbody2D>();
            Collider2D bombCol = bomb.GetComponent<Collider2D>();

            if (bombCol != null)
            {
                foreach (Collider2D col in ignoreColliders)
                    if (col != null) Physics2D.IgnoreCollision(bombCol, col);
            }

            if (rb != null)
            {
                rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

                // Bay ngang ngược hướng thuyền
                rb.velocity = new Vector2(-10f * boat.boatFacing, 0f);
            }

            canFire = false;
            StartCoroutine(ResetFireCooldown());
        }

        private IEnumerator ResetFireCooldown()
        {
            yield return new WaitForSeconds(fireCooldown);
            canFire = true;
        }
    }
}
