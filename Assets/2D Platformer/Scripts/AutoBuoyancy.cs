using UnityEngine;
using System.Collections.Generic;

public class AutoBuoyancy : MonoBehaviour
{
    [Header("Buoyancy Points")]
    public Transform[] floatPoints; // 2–3 điểm dưới đáy thuyền

    [Header("Settings")]
    public float buoyancyStrength = 20f; 
    public float damping = 0.2f;

    private Rigidbody2D rb;
    private List<Collider2D> waterZones = new List<Collider2D>();

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("WaterZone") && !waterZones.Contains(other))
            waterZones.Add(other);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("WaterZone"))
            waterZones.Remove(other);
    }

    void FixedUpdate()
    {
        if (waterZones.Count == 0) 
            return; // không ở trong nước thì không làm gì

        foreach (var p in floatPoints)
        {
            float waterLevelY = GetWaterSurfaceAtPoint(p.position);

            float difference = p.position.y - waterLevelY;

            if (difference < 0f) // điểm này dưới nước
            {
                float force = Mathf.Abs(difference) * buoyancyStrength;

                rb.AddForceAtPosition(Vector2.up * force, p.position);

                // damping để giảm rung
                rb.AddForceAtPosition(-rb.GetPointVelocity(p.position) * damping, p.position);
            }
        }
    }

    // Lấy mặt nước từ collider của WaterZone mà điểm đang đứng trong
    float GetWaterSurfaceAtPoint(Vector2 point)
    {
        foreach (var zone in waterZones)
        {
            if (zone.bounds.Contains(point))
            {
                return zone.bounds.max.y; // mặt nước = cạnh trên của vùng nước
            }
        }

        // fallback: lấy vùng nước đầu tiên
        return waterZones[0].bounds.max.y;
    }
}
