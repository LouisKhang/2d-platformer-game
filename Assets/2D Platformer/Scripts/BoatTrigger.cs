using UnityEngine;
using Platformer; // Nếu PlayerController nằm trong namespace này
public class BoatTrigger : MonoBehaviour
{
    public BoatController boat;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            other.GetComponent<PlayerController>().nearBoat = boat;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController pc = other.GetComponent<PlayerController>();
            if (pc.nearBoat == boat)
                pc.nearBoat = null;
        }
    }
}
