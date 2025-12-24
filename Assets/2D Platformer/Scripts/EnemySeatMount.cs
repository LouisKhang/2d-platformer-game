using UnityEngine;

[System.Serializable]
public class EnemySeat
{
    public Transform seatPoint;   // Vị trí ghế
    public GameObject enemy;      // Quái mount
    [HideInInspector] public Rigidbody2D rb;
    [HideInInspector] public Collider2D col;
    [HideInInspector] public Vector3 originalScale; // scale gốc
}

public class EnemySeatMount : MonoBehaviour
{
    public EnemySeat[] seats;

    void Start()
    {
        foreach (var seat in seats)
        {
            if (seat.enemy == null || seat.seatPoint == null) continue;

            seat.rb = seat.enemy.GetComponent<Rigidbody2D>();
            seat.col = seat.enemy.GetComponent<Collider2D>();

            // Lưu scale gốc
            seat.originalScale = seat.enemy.transform.localScale;

            // Khóa quái
            if (seat.rb != null)
            {
                seat.rb.gravityScale = 0f;
                seat.rb.velocity = Vector2.zero;
                seat.rb.isKinematic = true;
            }
            if (seat.col != null) seat.col.enabled = false;

            // Gắn quái vào ghế
            seat.enemy.transform.SetParent(seat.seatPoint);
            seat.enemy.transform.localPosition = Vector3.zero;
        }
    }

    void LateUpdate()
    {
        // Flip quái theo thuyền, giữ nguyên scale gốc
        foreach (var seat in seats)
        {
            if (seat.enemy == null) continue;
            Vector3 scale = seat.originalScale;
            scale.x = Mathf.Abs(scale.x) * Mathf.Sign(transform.parent.localScale.x); // chỉ flip X
            seat.enemy.transform.localScale = scale;
        }
    }
}
