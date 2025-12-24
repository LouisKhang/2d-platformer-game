using UnityEngine;

// Gắn script này vào Activation Collider (vùng Trigger)
public class StoneActivator : MonoBehaviour
{
    [Tooltip("Kéo Component StoneBarrierManager (từ Stone_Barrier_Root) vào đây.")]
    public StoneBarrierManager stoneBarrierManager; 
    
    [Header("Activation Settings")]
    [Tooltip("Kích hoạt bởi Player hay Boat?")]
    public bool activateByPlayer = true;
    [Tooltip("Kích hoạt bởi Boat (khi có player trên thuyền)?")]
    public bool activateByBoat = true;
    
    private bool activated = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Đã kích hoạt rồi thì bỏ qua
        if (activated || stoneBarrierManager == null) 
            return;

        bool shouldActivate = false;

        // TRƯỜNG HỢP 1: Player đi bộ qua
        if (activateByPlayer && other.CompareTag("Player"))
        {
            shouldActivate = true;
            Debug.Log("🚶 Player đi bộ kích hoạt cục đá");
        }

        // TRƯỜNG HỢP 2: Thuyền (có player) đi qua
        if (activateByBoat)
        {
            // Kiểm tra xem object có phải là Boat không
            Platformer.BoatController boat = other.GetComponent<Platformer.BoatController>();
            
            if (boat != null && boat.playerOnBoat)
            {
                shouldActivate = true;
                Debug.Log("⛵ Thuyền (có player) kích hoạt cục đá");
            }
        }

        // Kích hoạt cục đá
        if (shouldActivate)
        {
            stoneBarrierManager.StartRise();
            activated = true;
            
            // Vô hiệu hóa trigger này
            GetComponent<Collider2D>().enabled = false;
            
            Debug.Log("🪨 Stone Barrier Activated!");
        }
    }
}